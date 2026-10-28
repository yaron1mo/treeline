#!/usr/bin/env python3
"""Turn a real spot on the map into a Treeline field.

Give it the centre of the field (latitude, longitude) and the direction the
enemy comes from. It downloads OpenStreetMap data for a box around
that point (200 x 400 m by default) and writes a field file the game can load ("Load field file").

    python3 tools/osm_to_field.py 48.2915 37.1720 --enemy-from east --name "Belts near X"

No extra packages needed: standard-library Python 3.8+.
Map data (c) OpenStreetMap contributors, ODbL. The game shows this credit.
"""
import argparse
import json
import math
import random
import sys
import urllib.parse
import urllib.request

OVERPASS = "https://overpass-api.de/api/interpreter"
FIELD_W, FIELD_H = 200.0, 400.0  # changed by --size

# Direction the attackers travel (east, north components) for each enemy side.
DOWN = {"north": (0.0, -1.0), "south": (0.0, 1.0), "east": (-1.0, 0.0), "west": (1.0, 0.0)}


def build_query(lat, lon):
    radius = math.hypot(FIELD_W, FIELD_H) / 2 + 40  # covers the box in any orientation, with margin
    dlat = radius / 110574.0
    dlon = radius / (111320.0 * math.cos(math.radians(lat)))
    bbox = f"{lat - dlat},{lon - dlon},{lat + dlat},{lon + dlon}"
    parts = [
        'way["landuse"~"^(forest|farmland|meadow|reservoir|basin|landfill)$"]',
        'way["natural"~"^(wood|scrub|water|tree_row|valley|gully)$"]',
        'way["waterway"~"^(river|stream|canal|ditch|drain)$"]',
        'way["highway"]',
        'way["railway"~"^(rail|light_rail|narrow_gauge|disused|abandoned)$"]',
        'way["man_made"~"^(pipeline|spoil_heap|embankment)$"]',
        'relation["natural"="water"]',
        'relation["landuse"="forest"]',
    ]
    body = "".join(f"{p}({bbox});" for p in parts)
    return f"[out:json][timeout:30];({body});out geom;"


def fetch(lat, lon):
    data = urllib.parse.urlencode({"data": build_query(lat, lon)}).encode()
    req = urllib.request.Request(OVERPASS, data=data, headers={"User-Agent": "treeline-field-import/1.0"})
    with urllib.request.urlopen(req, timeout=60) as resp:
        return json.load(resp)


class Projector:
    """Latitude/longitude to field metres: x across the field, y from the enemy edge to our line."""

    def __init__(self, lat0, lon0, enemy_from):
        self.lat0, self.lon0 = lat0, lon0
        self.kx = 111320.0 * math.cos(math.radians(lat0))
        self.ky = 110574.0
        de, dn = DOWN[enemy_from]
        self.down = (de, dn)
        self.right = (-dn, de)

    def __call__(self, lat, lon):
        e = (lon - self.lon0) * self.kx
        n = (lat - self.lat0) * self.ky
        x = e * self.right[0] + n * self.right[1] + FIELD_W / 2
        y = e * self.down[0] + n * self.down[1] + FIELD_H / 2
        return [round(x, 1), round(y, 1)]


def area_perimeter(pts):
    a = p = 0.0
    for i in range(len(pts)):
        x1, y1 = pts[i - 1]
        x2, y2 = pts[i]
        a += x1 * y2 - x2 * y1
        p += math.hypot(x2 - x1, y2 - y1)
    return abs(a) / 2, p


def near_field(pts, pad=20):
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    return max(xs) > -pad and min(xs) < FIELD_W + pad and max(ys) > -pad and min(ys) < FIELD_H + pad


def longest_edge_angle(pts):
    best, ang = 0, 0
    for i in range(len(pts)):
        (x1, y1), (x2, y2) = pts[i - 1], pts[i]
        d = math.hypot(x2 - x1, y2 - y1)
        if d > best:
            best, ang = d, math.degrees(math.atan2(y2 - y1, x2 - x1))
    return round(ang % 180)


def tag_width(tags, default):
    try:
        return max(2.0, min(40.0, float(str(tags.get("width", "")).split()[0])))
    except (ValueError, IndexError):
        return default


def shapes(element, proj):
    """Yield (points, closed) for a way, or for each outer ring of a relation."""
    if element["type"] == "way" and element.get("geometry"):
        pts = [proj(g["lat"], g["lon"]) for g in element["geometry"]]
        yield pts, len(pts) > 3 and pts[0] == pts[-1]
    elif element["type"] == "relation":
        for m in element.get("members", []):
            if m.get("role") == "outer" and m.get("geometry"):
                pts = [proj(g["lat"], g["lon"]) for g in m["geometry"]]
                yield pts, len(pts) > 3 and pts[0] == pts[-1]


def classify(tags, closed, pts):
    """Map OpenStreetMap tags to a Treeline feature, or None to skip."""
    lu, nat, ww, hw, rw, mm = (tags.get(k) for k in ("landuse", "natural", "waterway", "highway", "railway", "man_made"))
    if closed:
        if lu == "forest" or nat == "wood":
            a, p = area_perimeter(pts[:-1])
            thickness = 2 * a / p if p else 0       # about the strip width for long shapes
            elongated = a > 0 and p * p / a > 30      # a square scores 16, a forest belt 40+
            return {"type": "belt" if elongated and thickness < 25 else "grove", "poly": pts[:-1]}
        if nat == "scrub":
            return {"type": "belt", "poly": pts[:-1]}
        if nat == "water" or lu in ("reservoir", "basin"):
            kind = "river" if tags.get("water") in ("river", "canal", "stream") else "pond"
            return {"type": kind, "poly": pts[:-1]}
        if mm == "spoil_heap" or lu == "landfill":
            return {"type": "heap", "poly": pts[:-1]}
        if lu in ("farmland", "meadow"):
            return {"type": "field", "poly": pts[:-1], "angle": longest_edge_angle(pts[:-1])}
    if nat == "tree_row":
        return {"type": "belt", "line": pts, "width": tag_width(tags, 10)}
    if ww in ("river", "canal"):
        return {"type": "river", "line": pts, "width": tag_width(tags, 14 if ww == "river" else 8)}
    if ww in ("stream", "ditch", "drain"):
        return {"type": "river", "line": pts, "width": tag_width(tags, 4 if ww == "stream" else 3)}
    if nat in ("valley", "gully"):
        return {"type": "balka", "line": pts, "width": 14}
    if rw:
        return {"type": "embankment", "line": pts, "width": 7}
    if mm == "embankment":
        return {"type": "embankment", "line": pts, "width": 6}
    if mm == "pipeline" and tags.get("location") not in ("underground", "underwater"):
        return {"type": "pipe", "line": pts, "width": 3}
    if hw and hw not in ("path", "footway", "cycleway", "steps", "bridleway", "proposed", "construction"):
        return {"type": "road", "line": pts, "width": tag_width(tags, 4 if hw == "track" else 7)}
    return None


ORDER = ["field", "hill", "heap", "grove", "belt", "pond", "river", "embankment", "balka", "pipe", "road"]


def convert(osm, lat, lon, enemy_from, name):
    proj = Projector(lat, lon, enemy_from)
    feats = []
    for el in osm.get("elements", []):
        tags = el.get("tags", {})
        for pts, closed in shapes(el, proj):
            if len(pts) < 2 or not near_field(pts):
                continue
            f = classify(tags, closed, pts)
            if f:
                f["osm"] = f"{el['type']}/{el['id']}"
                feats.append(f)
    rnd = random.Random(f"{lat},{lon}")
    for f in feats:
        if f["type"] == "field":
            f["tone"] = rnd.choice(["stubble", "plowed", "sunflower"])
    feats.sort(key=lambda f: ORDER.index(f["type"]))
    counts = {}
    for f in feats:
        counts[f["type"]] = counts.get(f["type"], 0) + 1
    summary = ", ".join(f"{n} {t}" for t, n in counts.items()) or "no terrain features"
    return {
        "name": name,
        "sector": name,
        "subtitle": f"Real terrain at {lat:.4f}, {lon:.4f}, enemy from the {enemy_from}. Map data © OpenStreetMap contributors.",
        "source": "© OpenStreetMap contributors, ODbL",
        "center": [lat, lon],
        "enemy_from": enemy_from,
        "size": [FIELD_W, FIELD_H],
        "features": feats,
    }, summary


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("lat", type=float, help="latitude of the field centre, e.g. 48.2915")
    ap.add_argument("lon", type=float, help="longitude of the field centre, e.g. 37.1720")
    ap.add_argument("--enemy-from", choices=sorted(DOWN), default="north", help="side the attackers come from (default: north)")
    ap.add_argument("--name", default=None, help="field name shown in the game")
    ap.add_argument("--out", default=None, help="output file (default: fields/<name>.json)")
    ap.add_argument("--size", default="200x400", help="field size in metres, width x depth (default: 200x400)")
    ap.add_argument("--from-file", default=None, help="use a saved Overpass JSON response instead of downloading")
    args = ap.parse_args(argv)
    global FIELD_W, FIELD_H
    try:
        FIELD_W, FIELD_H = (float(v) for v in args.size.lower().split("x"))
    except ValueError:
        sys.exit("--size must look like 200x400")

    name = args.name or f"Field {args.lat:.3f} {args.lon:.3f}"
    if args.from_file:
        with open(args.from_file, encoding="utf-8") as fh:
            osm = json.load(fh)
    else:
        try:
            osm = fetch(args.lat, args.lon)
        except Exception as exc:  # network or server error
            sys.exit(f"Could not download OpenStreetMap data: {exc}. Check your connection and try again in a minute.")

    field, summary = convert(osm, args.lat, args.lon, args.enemy_from, name)
    out = args.out or "fields/" + "".join(ch if ch.isalnum() else "-" for ch in name.lower()).strip("-") + ".json"
    with open(out, "w", encoding="utf-8") as fh:
        json.dump(field, fh, ensure_ascii=False, indent=1)
    print(f"Wrote {out}: {summary}.")
    if not field["features"]:
        print("Nothing mapped here. Try a spot with visible forest belts or a river on openstreetmap.org.")


if __name__ == "__main__":
    main()
