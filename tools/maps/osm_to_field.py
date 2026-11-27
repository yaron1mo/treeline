#!/usr/bin/env python3
"""Turn a real OpenStreetMap window into a Treeline field (real-*.json).

The game field is 200 x 400 m, but real windbreak belts stand 300-800 m apart, so a
real 200 x 400 m window has at most one belt. We therefore take a real window of
about 0.6 x 1.2 km, rotate it so the belts run across the line of advance (like in
Forest belts) and scale it down to 200 x 400 m. The scale factor is written into
every map ("scale" and "source"), so winter/night variants can be built from the
same data later.

    python3 tools/maps/osm_to_field.py --centre 48.9500 31.4500 --rotation 90 \
        --scale 3.0 --name "Sector 08" --sector "Sector 08" \
        --region ua-cherkasy-plains \
        --out unity/Assets/Resources/Fields/real-sector-08.json

Every Overpass answer is cached in tools/maps/cache/ (key = hash of the query), so
the same input always gives the same map.

Tag mapping (decision 2026-11-27, issue #25):
  landuse=farmland            -> field (tone from crop, else a seeded random tone of Forest belts)
  natural=tree_row            -> belt
  narrow natural=wood strips  -> belt
  landuse=forest, natural=wood-> grove
  highway=track               -> tracks
  other highways              -> road
  natural=water               -> pond
  waterway=river/canal        -> river

No extra packages: standard-library Python 3.8+.
Map data (c) OpenStreetMap contributors, ODbL (see docs/maps/ATTRIBUTION.md).
"""

import argparse
import hashlib
import json
import math
import os
import random
import time
import urllib.parse
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CACHE_DIR = os.path.join(ROOT, "tools", "maps", "cache")

FIELD_W, FIELD_H = 200.0, 400.0          # game field size in metres
DEFAULT_SCALE = 3.0                        # 1 game metre = 3 real metres (0.6 x 1.2 km window)
PAD = 30.0                                 # keep geometry this far outside the field (game metres)

ENDPOINTS = [
    "https://overpass.openstreetmap.fr/api/interpreter",
    "https://overpass.kumi.systems/api/interpreter",
    "https://overpass.private.coffee/api/interpreter",
    "https://z.overpass-api.de/api/interpreter",
    "https://overpass-api.de/api/interpreter",
]
USER_AGENT = "treeline-real-maps/1.0 (github.com/yaron1mo/treeline; map data (c) OpenStreetMap contributors, ODbL)"

# Tones Forest belts uses for fields; a field without a crop tag gets one of these.
TONES = ["stubble", "plowed", "sunflower"]
CROP_TONE = {
    "sunflower": "sunflower",
    "wheat": "stubble", "barley": "stubble", "rye": "stubble", "oats": "stubble",
    "triticale": "stubble", "millet": "stubble", "maize": "stubble", "corn": "stubble",
    "potato": "plowed", "beet": "plowed", "fodder": "plowed", "rape": "plowed",
}
ROAD_WIDTH = {  # real-world carriageway width in metres, by highway class
    "motorway": 14, "trunk": 12, "primary": 10, "secondary": 9, "tertiary": 8,
    "unclassified": 7, "residential": 6, "living_street": 5, "service": 5, "track": 4,
}
BELT_DEFAULT_REAL_W = 24.0   # a windbreak strip is about 4-6 tree rows wide
STRIP_MAX_REAL_W = 60.0      # a wood polygon narrower than this (real m) is a belt, not a grove


# ---------------------------------------------------------------- Overpass ---

def features_query(bbox):
    """One Overpass query for everything we map, plus building bounding boxes (town filter)."""
    s, w, n, e = bbox
    b = f"{s},{w},{n},{e}"
    parts = [
        f'way["landuse"~"^(farmland|forest|orchard|vineyard)$"]({b});',
        f'way["natural"~"^(wood|tree_row|water)$"]({b});',
        f'way["waterway"~"^(river|canal|riverbank)$"]({b});',
        f'way["highway"~"^(track|service|unclassified|residential|living_street|tertiary|secondary|primary|trunk|motorway)$"]({b});',
        f'relation["natural"~"^(wood|water)$"]({b});',
        f'relation["landuse"~"^(farmland|forest)$"]({b});',
    ]
    body = "".join(parts)
    return (f'[out:json][timeout:90];way["building"]({b});out bb;'
            f'({body});out geom;')


def cache_path(query):
    """Cache file for a query: tools/maps/cache/<hash>.json.gz (gzip: the answers are MBs)."""
    key = hashlib.sha256(query.encode("utf-8")).hexdigest()[:24]
    return os.path.join(CACHE_DIR, key + ".json.gz"), key


def overpass(query, max_tries=6):
    """Fetch a query with retries and endpoint rotation; cache every answer (gzipped)."""
    import gzip
    path, key = cache_path(query)
    if os.path.exists(path):
        try:
            with gzip.open(path, "rt", encoding="utf-8") as fh:
                return json.load(fh)["response"], key
        except (ValueError, KeyError, OSError):
            os.remove(path)  # truncated cache file: fetch again
    os.makedirs(CACHE_DIR, exist_ok=True)
    data = urllib.parse.urlencode({"data": query}).encode()
    last = ""
    payload = None
    for attempt in range(max_tries):
        url = ENDPOINTS[attempt % len(ENDPOINTS)]
        req = urllib.request.Request(url, data=data, headers={"User-Agent": USER_AGENT})
        try:
            with urllib.request.urlopen(req, timeout=90) as resp:
                payload = json.load(resp)
            break
        except urllib.error.HTTPError as exc:
            last = f"{url}: HTTP {exc.code}"
            if exc.code == 400:  # query error: retrying will not help
                raise RuntimeError(f"Overpass rejected the query: {last}")
            time.sleep(min(10 * (2 ** attempt), 60) if exc.code == 429 else min(5 * (2 ** attempt), 30))
        except Exception as exc:  # noqa: BLE001 - retry any transport error
            last = f"{url}: {exc}"
            time.sleep(min(5 * (2 ** attempt), 30))
    if payload is None:
        raise RuntimeError(f"Overpass failed after {max_tries} tries: {last}")
    import gzip
    tmp = path + ".tmp"
    with gzip.open(tmp, "wt", encoding="utf-8") as fh:
        json.dump({"query": query, "response": payload}, fh)
    os.replace(tmp, path)  # atomic: a killed run never leaves a half-written cache file
    return payload, key


# ------------------------------------------------------------- geometry ------

class Projector:
    """Lat/lon -> game metres. rotation = compass bearing the attackers come FROM
    (0 = north = top edge y=0). scale = real metres per game metre."""

    def __init__(self, lat0, lon0, rotation_deg, scale):
        self.lat0, self.lon0 = lat0, lon0
        self.kx = 111320.0 * math.cos(math.radians(lat0))
        self.ky = 110574.0
        th = math.radians(rotation_deg)
        self.sx, self.sy = math.sin(th), math.cos(th)      # towards the spawn edge (east, north)
        self.rx, self.ry = math.cos(th), -math.sin(th)     # across the field
        self.scale = scale

    def metres(self, lat, lon):
        e = (lon - self.lon0) * self.kx
        n = (lat - self.lat0) * self.ky
        u = e * self.rx + n * self.ry      # across the field
        v = e * self.sx + n * self.sy      # towards the spawn edge
        return u, v

    def __call__(self, lat, lon):
        u, v = self.metres(lat, lon)
        return [round(FIELD_W / 2 + u / self.scale, 1),
                round(FIELD_H / 2 - v / self.scale, 1)]


def window_bbox(lat0, lon0, rotation_deg, scale, margin_m=80.0):
    """BBox (S,W,N,E) covering the rotated real window around (lat0, lon0)."""
    th = math.radians(rotation_deg)
    sx, sy = math.sin(th), math.cos(th)
    rx, ry = math.cos(th), -math.sin(th)
    hw, hh = FIELD_W * scale / 2 + margin_m, FIELD_H * scale / 2 + margin_m
    kx = 111320.0 * math.cos(math.radians(lat0))
    ky = 110574.0
    lats, lons = [], []
    for du, dv in ((-hw, -hh), (hw, -hh), (hw, hh), (-hw, hh)):
        e = du * rx + dv * sx
        n = du * ry + dv * sy
        lats.append(lat0 + n / ky)
        lons.append(lon0 + e / kx)
    return (min(lats), min(lons), max(lats), max(lons))


def clip_polyline(pts, x0, y0, x1, y1):
    """Clip a polyline to an axis-aligned rect (game metres); keeps order."""
    out = []
    for i in range(1, len(pts)):
        ax, ay = pts[i - 1]
        bx, by = pts[i]
        dx, dy = bx - ax, by - ay
        t0, t1 = 0.0, 1.0
        ok = True
        for p, q in ((-dx, ax - x0), (dx, x1 - ax), (-dy, ay - y0), (dy, y1 - ay)):
            if p == 0:
                if q < 0:
                    ok = False
                    break
            else:
                r = q / p
                if p < 0:
                    if r > t1:
                        ok = False
                        break
                    if r > t0:
                        t0 = r
                else:
                    if r < t0:
                        ok = False
                        break
                    if r < t1:
                        t1 = r
        if not ok:
            continue
        seg = [(ax + t0 * dx, ay + t0 * dy), (ax + t1 * dx, ay + t1 * dy)]
        if out and abs(out[-1][0] - seg[0][0]) < 1e-6 and abs(out[-1][1] - seg[0][1]) < 1e-6:
            out.append(seg[1])
        else:
            out.extend(seg)
    return out


def clip_polygon(pts, x0, y0, x1, y1):
    """Sutherland-Hodgman clip of a closed ring (without the repeated last point)."""
    def clip_edge(poly, inside, intersect):
        if not poly:
            return []
        out = []
        prev = poly[-1]
        for cur in poly:
            if inside(cur):
                if not inside(prev):
                    out.append(intersect(prev, cur))
                out.append(cur)
            elif inside(prev):
                out.append(intersect(prev, cur))
            prev = cur
        return out

    def ix(a, b, x):
        t = (x - a[0]) / (b[0] - a[0]) if b[0] != a[0] else 0.0
        return (x, a[1] + t * (b[1] - a[1]))

    def iy(a, b, y):
        t = (y - a[1]) / (b[1] - a[1]) if b[1] != a[1] else 0.0
        return (a[0] + t * (b[0] - a[0]), y)

    poly = list(pts)
    poly = clip_edge(poly, lambda p: p[0] >= x0, lambda a, b: ix(a, b, x0))
    poly = clip_edge(poly, lambda p: p[0] <= x1, lambda a, b: ix(a, b, x1))
    poly = clip_edge(poly, lambda p: p[1] >= y0, lambda a, b: iy(a, b, y0))
    poly = clip_edge(poly, lambda p: p[1] <= y1, lambda a, b: iy(a, b, y1))
    return poly


def area_perimeter(pts):
    a = p = 0.0
    for i in range(len(pts)):
        x1, y1 = pts[i - 1]
        x2, y2 = pts[i]
        a += x1 * y2 - x2 * y1
        p += math.hypot(x2 - x1, y2 - y1)
    return abs(a) / 2.0, p


def in_poly(x, y, pts):
    c = False
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
            c = not c
        j = i
    return c


def dist_line(x, y, pts):
    best = float("inf")
    for i in range(1, len(pts)):
        ax, ay = pts[i - 1]
        bx, by = pts[i]
        dx, dy = bx - ax, by - ay
        l2 = dx * dx + dy * dy
        t = 0.0 if l2 == 0 else max(0.0, min(1.0, ((x - ax) * dx + (y - ay) * dy) / l2))
        qx, qy = ax + t * dx - x, ay + t * dy - y
        best = min(best, math.hypot(qx, qy))
    return best


def longest_edge_angle(pts):
    best, ang = 0.0, 0.0
    for i in range(len(pts)):
        (x1, y1), (x2, y2) = pts[i - 1], pts[i]
        d = math.hypot(x2 - x1, y2 - y1)
        if d > best:
            best, ang = d, math.degrees(math.atan2(y2 - y1, x2 - x1))
    return round(ang % 180)


def tag_width_m(tags, default):
    try:
        return max(1.0, min(60.0, float(str(tags.get("width", "")).split()[0])))
    except (ValueError, IndexError):
        return default


# ------------------------------------------------------------ conversion -----

def shapes(element, proj):
    """Yield (points, closed, osm_ref) for a way, or the assembled outer ring of a relation."""
    if element.get("type") == "way" and element.get("geometry"):
        pts = [proj(g["lat"], g["lon"]) for g in element["geometry"]]
        yield pts, len(pts) > 3 and pts[0] == pts[-1], f"way/{element['id']}"
    elif element.get("type") == "relation":
        ring = []
        for m in element.get("members", []):
            if m.get("role") in ("outer", "") and m.get("geometry"):
                pts = [proj(g["lat"], g["lon"]) for g in m["geometry"]]
                if ring and pts and math.hypot(ring[-1][0] - pts[0][0], ring[-1][1] - pts[0][1]) < 0.2:
                    pts = pts[1:]
                ring.extend(pts)
        if len(ring) >= 4:
            yield ring, ring[0] == ring[-1], f"relation/{element['id']}"


def classify(tags, closed, pts, scale, rnd):
    """Map one OSM shape to a field feature dict, or None to skip it."""
    lu = tags.get("landuse")
    nat = tags.get("natural")
    ww = tags.get("waterway")
    hw = tags.get("highway")
    if closed:
        ring = pts[:-1]
        if len(ring) < 3:
            return None
        if lu in ("farmland", "orchard", "vineyard"):
            crop = (tags.get("crop") or "").lower()
            tone = CROP_TONE.get(crop)
            if tone is None:
                tone = rnd.choice(TONES) if not crop else "plowed"
            return {"type": "field", "poly": ring, "tone": tone,
                    "angle": longest_edge_angle(ring)}
        if lu == "forest" or nat == "wood":
            a, p = area_perimeter(ring)
            thickness_game = 2 * a / p if p else 0.0
            elongated = p * p / a > 25 if a else False
            # a wood polygon narrower than STRIP_MAX_REAL_W real metres is a belt strip
            if elongated and thickness_game * scale < STRIP_MAX_REAL_W:
                ang = math.radians(longest_edge_angle(ring))
                dx, dy = math.cos(ang), math.sin(ang)
                projs = [x * dx + y * dy for x, y in ring]
                i0, i1 = projs.index(min(projs)), projs.index(max(projs))
                w = max(7.0, min(16.0, thickness_game))
                return {"type": "belt", "line": [ring[i0], ring[i1]], "width": round(w, 1)}
            return {"type": "grove", "poly": ring}
        if nat == "water" or ww == "riverbank":
            return {"type": "pond", "poly": ring}
        return None
    # open lines
    if nat == "tree_row":
        w = tag_width_m(tags, BELT_DEFAULT_REAL_W) / scale
        return {"type": "belt", "line": pts, "width": round(max(7.0, min(16.0, w)), 1)}
    if hw == "track":
        return {"type": "tracks", "line": pts}
    if hw and hw in ROAD_WIDTH:
        w = tag_width_m(tags, ROAD_WIDTH[hw]) / scale
        return {"type": "road", "line": pts, "width": round(max(2.5, min(8.0, w)), 1)}
    if ww in ("river", "canal"):
        w = tag_width_m(tags, 12.0 if ww == "river" else 8.0) / scale
        if w < 1.5:
            return None
        return {"type": "river", "line": pts, "width": round(w, 1)}
    return None


def near_field(pts, pad=PAD):
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    return max(xs) > -pad and min(xs) < FIELD_W + pad and max(ys) > -pad and min(ys) < FIELD_H + pad


def merge_belts(feats):
    """Join collinear belt fragments (one windbreak split by crossings) into single
    belt lines: group by direction and perpendicular offset, then chain along the line."""
    belts = [(f["line"], f.get("width", 10.0), f.get("osm", "")) for f in feats
             if f["type"] == "belt" and f.get("line")]
    others = [f for f in feats if not (f["type"] == "belt" and f.get("line"))]
    used = [False] * len(belts)
    out = []
    for i, (ln, w, ref) in enumerate(belts):
        if used[i]:
            continue
        used[i] = True
        ang = math.degrees(math.atan2(ln[-1][1] - ln[0][1], ln[-1][0] - ln[0][0])) % 180.0
        ux, uy = math.cos(math.radians(ang)), math.sin(math.radians(ang))
        nx, ny = -uy, ux
        rho = ln[0][0] * nx + ln[0][1] * ny
        group = [(ln, w, ref)]
        for j in range(i + 1, len(belts)):
            if used[j]:
                continue
            lj, wj, refj = belts[j]
            angj = math.degrees(math.atan2(lj[-1][1] - lj[0][1], lj[-1][0] - lj[0][0])) % 180.0
            dth = abs(angj - ang)
            dth = min(dth, 180.0 - dth)
            if dth > 12.0:
                continue
            rhoj = lj[0][0] * nx + lj[0][1] * ny
            if abs(rhoj - rho) > 10.0:
                continue
            used[j] = True
            group.append((lj, wj, refj))
        if len(group) == 1:
            out.append((ln, w, ref))
            continue
        group.sort(key=lambda g: g[0][0][0] * ux + g[0][0][1] * uy)
        line = list(group[0][0])
        width = group[0][1]
        for g, wj, _ in group[1:]:
            if math.hypot(line[-1][0] - g[0][0], line[-1][1] - g[0][1]) <= \
               math.hypot(line[-1][0] - g[-1][0], line[-1][1] - g[-1][1]):
                line = line + list(g)
            else:
                line = line + list(reversed(g))
            width = max(width, wj)
        out.append((line, width, group[0][2]))
    result = []
    for ln, w, ref in out:
        length = sum(math.hypot(ln[i][0] - ln[i - 1][0], ln[i][1] - ln[i - 1][1])
                     for i in range(1, len(ln)))
        if length < 40.0:  # a tiny fragment, not a belt
            continue
        f = {"type": "belt", "line": [[round(x, 1), round(y, 1)] for x, y in ln],
             "width": round(w, 1)}
        if ref:
            f["osm"] = ref
        result.append(f)
    return others + result


def convert(osm, lat0, lon0, rotation_deg, scale, name, sector, region, cache_key, seed):
    proj = Projector(lat0, lon0, rotation_deg, scale)
    rnd = random.Random(f"{lat0:.5f},{lon0:.5f},{rotation_deg},{seed}")
    feats = []
    for el in osm.get("elements", []):
        tags = el.get("tags", {})
        if not tags or el.get("type") == "count":
            continue
        for pts, closed, ref in shapes(el, proj):
            if len(pts) < 2 or not near_field(pts):
                continue
            f = classify(tags, closed, pts, scale, rnd)
            if not f:
                continue
            if closed and f.get("poly") is not None:
                ring = clip_polygon(f["poly"], -PAD, -PAD, FIELD_W + PAD, FIELD_H + PAD)
                if len(ring) < 3:
                    continue
                f["poly"] = [[round(x, 1), round(y, 1)] for x, y in ring]
            else:
                line = clip_polyline(pts, -PAD, -PAD, FIELD_W + PAD, FIELD_H + PAD)
                if len(line) < 2:
                    continue
                key = "poly" if "poly" in f else "line"
                f[key] = [[round(x, 1), round(y, 1)] for x, y in line]
            f["osm"] = ref
            feats.append(f)
    feats = merge_belts(feats)
    order = {"field": 0, "pond": 1, "river": 2, "road": 3, "tracks": 4, "belt": 5, "grove": 6, "craters": 7}
    feats.sort(key=lambda f: order.get(f["type"], 8))

    spawns = choose_spawns(feats)
    craters = make_craters(feats, rnd)
    if craters:
        feats.append({"type": "craters", "points": craters})

    bbox = window_bbox(lat0, lon0, rotation_deg, scale, margin_m=0.0)
    return {
        "name": name,
        "sector": sector,
        "subtitle": subtitle(feats),
        "size": [int(FIELD_W), int(FIELD_H)],
        "spawns": spawns,
        "scale": scale,
        "source": {
            "provider": "OpenStreetMap",
            "licence": "ODbL - map data (c) OpenStreetMap contributors",
            "bbox": [round(bbox[0], 6), round(bbox[1], 6), round(bbox[2], 6), round(bbox[3], 6)],
            "centre": [round(lat0, 6), round(lon0, 6)],
            "rotation_deg": rotation_deg,
            "real_window_m": [int(FIELD_W * scale), int(FIELD_H * scale)],
            "overpass_cache": f"tools/maps/cache/{cache_key}.json.gz",
            "region": region,
        },
        "features": feats,
    }


def subtitle(feats):
    counts = {}
    for f in feats:
        counts[f["type"]] = counts.get(f["type"], 0) + 1
    bits = []
    if counts.get("field"):
        bits.append(f"{counts['field']} fields")
    if counts.get("belt"):
        bits.append(f"{counts['belt']} windbreak belts")
    if counts.get("grove"):
        bits.append(f"{counts['grove']} groves")
    if counts.get("tracks"):
        bits.append(f"{counts['tracks']} field tracks")
    if counts.get("road"):
        bits.append(f"{counts['road']} roads")
    if counts.get("pond"):
        bits.append(f"{counts['pond']} ponds")
    if counts.get("river"):
        bits.append(f"{counts['river']} rivers")
    return ("Real farmland from OpenStreetMap: " + ", ".join(bits) + ".") if bits \
        else "Open real terrain from OpenStreetMap."


def choose_spawns(feats):
    """2-3 entry points on the top edge where tracks or roads enter the field;
    filled with spread points along the top edge so the attackers get 2+ lanes."""
    xs = []
    for f in feats:
        if f["type"] in ("tracks", "road") and "line" in f:
            line = f["line"]
            for i in range(1, len(line)):
                x0, y0 = line[i - 1]
                x1, y1 = line[i]
                if (y0 <= 0.0 <= y1) or (y1 <= 0.0 <= y0):
                    if y1 == y0:
                        continue
                    t = -y0 / (y1 - y0)
                    xs.append((x0 + t * (x1 - x0), 0 if f["type"] == "tracks" else 1))
    xs.sort(key=lambda p: (p[1], p[0]))  # tracks first
    picked = []
    for x, _ in xs:
        if not (8.0 <= x <= FIELD_W - 8.0):
            continue
        if all(abs(x - p) >= 50.0 for p in picked):
            picked.append(round(x, 1))
        if len(picked) == 3:
            break
    for frac in (0.2, 0.5, 0.8):
        if len(picked) >= 3:
            break
        x = round(FIELD_W * frac, 1)
        if all(abs(x - p) >= 40.0 for p in picked):
            picked.append(x)
    if len(picked) == 1:
        picked.append(round(min(FIELD_W - 8.0, max(8.0, picked[0] + 70.0)), 1))
    return sorted(picked[:3])


def make_craters(feats, rnd, count=12):
    fields = [f["poly"] for f in feats if f["type"] == "field"]
    blocks = [f for f in feats if f["type"] in ("belt", "grove", "pond", "river", "road")]
    pts = []
    for _ in range(count * 30):
        if len(pts) >= count:
            break
        x = round(rnd.uniform(6.0, FIELD_W - 6.0), 1)
        y = round(rnd.uniform(6.0, FIELD_H - 6.0), 1)
        if fields and not any(in_poly(x, y, p) for p in fields):
            continue
        ok = True
        for b in blocks:
            geom = b.get("poly") or b.get("line")
            if geom is None:
                continue
            if b.get("line"):
                d = dist_line(x, y, geom)
            else:
                d = 0.0 if in_poly(x, y, geom) else 30.0
            if d < 12.0:
                ok = False
                break
        if ok:
            pts.append([x, y, round(rnd.uniform(0.7, 2.2), 1)])
    return pts


def write_field(field, out_path, meta_guid=None):
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    text = json.dumps(field, indent=1, ensure_ascii=False)
    with open(out_path, "w", encoding="utf-8", newline="") as fh:
        fh.write(text.replace("\n", "\r\n"))
    if out_path.endswith(".json") and ".meta" not in out_path:
        guid = meta_guid or hashlib.md5(out_path.encode()).hexdigest()
        with open(out_path + ".meta", "w", encoding="utf-8", newline="") as fh:
            fh.write("fileFormatVersion: 2\r\n"
                     f"guid: {guid}\r\n"
                     "TextScriptImporter:\r\n"
                     "  externalObjects: {}\r\n"
                     "  userData: \r\n"
                     "  assetBundleName: \r\n"
                     "  assetBundleVariant: \r\n")


def build(lat0, lon0, rotation_deg, scale, name, sector, region, seed=1):
    """Download (or read from cache) and convert one window. Returns (field, cache_key)."""
    bbox = window_bbox(lat0, lon0, rotation_deg, scale, margin_m=80.0)
    query = features_query(bbox)
    osm, key = overpass(query)
    field = convert(osm, lat0, lon0, rotation_deg, scale, name, sector, region, key, seed)
    return field, key


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--centre", nargs=2, type=float, required=True, metavar=("LAT", "LON"),
                    help="centre of the real window, e.g. 48.9500 31.4500")
    ap.add_argument("--rotation", type=float, default=0.0,
                    help="compass bearing the attackers come FROM, degrees (0 = north); belts should run across the advance")
    ap.add_argument("--scale", type=float, default=DEFAULT_SCALE,
                    help=f"real metres per game metre (default {DEFAULT_SCALE}: a 0.6 x 1.2 km window)")
    ap.add_argument("--name", required=True, help="field name shown in the game")
    ap.add_argument("--sector", default=None, help="fictional sector name (default: same as --name)")
    ap.add_argument("--region", default="", help="region label written into source.region")
    ap.add_argument("--seed", type=int, default=1, help="seed for tones/craters")
    ap.add_argument("--out", required=True, help="output JSON path")
    args = ap.parse_args(argv)
    field, key = build(args.centre[0], args.centre[1], args.rotation, args.scale,
                       args.name, args.sector or args.name, args.region, args.seed)
    write_field(field, args.out)
    counts = {}
    for f in field["features"]:
        counts[f["type"]] = counts.get(f["type"], 0) + 1
    print(f"{args.out}: scale {args.scale}, spawns {field['spawns']}, cache {key}, "
          + ", ".join(f"{n} {t}" for t, n in sorted(counts.items())))


if __name__ == "__main__":
    main()
