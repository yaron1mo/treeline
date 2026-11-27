#!/usr/bin/env python3
"""Find real windows for Treeline maps: scan farmland regions on a grid, score each
0.6 x 1.2 km window, write the best 40 to tools/maps/candidates.csv.

A window is good when (issue #25):
  - at least 2 windbreak belts (natural=tree_row, or narrow wood strips) run ACROSS
    the line of advance,
  - at least 1 field track (highway=track) enters it,
  - some groves (landuse=forest / natural=wood),
  - no town (few buildings),
  - not only one field.

Regions: farmland plains of Central and Southern Ukraine, Moldova, Romania and
Eastern Poland, chosen away from places of known battles. Every Overpass answer is
cached in tools/maps/cache/, so a rerun is offline and deterministic.

    python3 tools/maps/find_candidates.py                 # all regions, top 40
    python3 tools/maps/find_candidates.py --regions ua-cherkasy --top 10

No extra packages: standard-library Python 3.8+.
Map data (c) OpenStreetMap contributors, ODbL.
"""

import argparse
import csv
import math
import os
import sys
import time
from concurrent.futures import ThreadPoolExecutor

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import osm_to_field as osm  # noqa: E402

# Farmland-plain rectangles (S, W, N, E), ~45 x 45 km, away from known battle places.
REGIONS = [
    ("ua-cherkasy",      48.95, 30.90, 49.50, 32.00),
    ("ua-kirovohrad",    48.35, 31.65, 48.90, 32.90),
    ("ua-poltava-north", 49.78, 34.05, 50.28, 35.15),
    ("ua-dnipro-west",   48.68, 34.65, 49.18, 35.45),
    ("ua-zaporizhzhia-n", 47.98, 35.35, 48.38, 36.05),
    ("ua-mykolaiv",      46.88, 31.25, 47.43, 32.15),
    ("ua-kherson-ne",    46.98, 33.15, 47.43, 33.85),
    ("ua-vinnytsia",     48.93, 28.25, 49.43, 29.15),
    ("md-central",       46.93, 28.45, 47.38, 29.15),
    ("md-south",         46.23, 28.65, 46.73, 29.35),
    ("ro-baraagan",      44.68, 26.95, 45.23, 28.05),
    ("ro-moldova",       46.43, 27.35, 47.08, 28.25),
    ("pl-lublin-east",   50.88, 22.95, 51.48, 23.75),
    ("pl-siedlce",       52.08, 22.55, 52.63, 23.35),
    ("pl-biala-podlaska", 51.98, 23.15, 52.43, 23.85),
]

TILE_M = 12000.0        # scan tile (query bbox is the tile plus margin)
MARGIN_M = 1600.0       # data margin around the tile so edge windows are scored fully
WIN_ACROSS = osm.FIELD_W * osm.DEFAULT_SCALE    # 600 m real across the field
WIN_ALONG = osm.FIELD_H * osm.DEFAULT_SCALE     # 1200 m real along the advance
STEP_ACROSS, STEP_ALONG = 300.0, 600.0
MAX_BUILDINGS = 25      # a town window is rejected
STRIP_MAX_W = 60.0      # a wood polygon narrower than this (real m) is a windbreak strip = belt


def tile_boxes(region):
    rid, s, w, n, e = region
    lat0 = (s + n) / 2
    kx = 111320.0 * math.cos(math.radians(lat0))
    ky = 110574.0
    width_m = (e - w) * kx
    height_m = (n - s) * ky
    boxes = []
    x = 0.0
    while x < width_m:
        y = 0.0
        while y < height_m:
            tlat = s + (y + min(TILE_M, height_m - y) / 2) / ky
            tlon = w + (x + min(TILE_M, width_m - x) / 2) / kx
            half = min(TILE_M, width_m - x) / 2 + MARGIN_M
            halfy = min(TILE_M, height_m - y) / 2 + MARGIN_M
            boxes.append((rid, tlat, tlon, half, halfy))
            y += TILE_M
        x += TILE_M
    return boxes


def tile_bbox(tlat, tlon, half_x, half_y):
    ky = 110574.0
    kx = 111320.0 * math.cos(math.radians(tlat))
    return (tlat - half_y / ky, tlon - half_x / kx, tlat + half_y / ky, tlon + half_x / kx)


def to_en(lat, lon, lat0, lon0):
    kx = 111320.0 * math.cos(math.radians(lat0))
    return (lon - lon0) * kx, (lat - lat0) * 110574.0


def merge_chains(segments, tol=6.0):
    """Join tree_row ways into chains by endpoint proximity (metres)."""
    segs = [list(s) for s in segments if len(s) >= 2]
    used = [False] * len(segs)
    chains = []
    for i, s in enumerate(segs):
        if used[i]:
            continue
        used[i] = True
        chain = s[:]
        changed = True
        while changed:
            changed = False
            head, tail = chain[0], chain[-1]
            for j, t in enumerate(segs):
                if used[j]:
                    continue
                for end, other in ((t[0], t[1:]), (t[-1], list(reversed(t[:-1])))):
                    if math.hypot(head[0] - end[0], head[1] - end[1]) <= tol:
                        chain = other + chain
                        used[j] = True
                        changed = True
                        break
                    if math.hypot(tail[0] - end[0], tail[1] - end[1]) <= tol:
                        chain = chain + other
                        used[j] = True
                        changed = True
                        break
                if changed:
                    break
        chains.append(chain)
    return chains


def dominant_bearing(chains):
    """Circular mean of chain bearings mod 180 (degrees, 0 = north)."""
    sx = sy = 0.0
    for c in chains:
        for i in range(1, len(c)):
            dx = c[i][0] - c[i - 1][0]
            dy = c[i][1] - c[i - 1][1]
            if dx == 0 and dy == 0:
                continue
            a = math.atan2(dx, dy)  # bearing from north, east positive
            sx += math.sin(2 * a)
            sy += math.cos(2 * a)
    if sx == 0 and sy == 0:
        return 0.0
    return math.degrees(0.5 * math.atan2(sx, sy)) % 180.0


def parse_tile(osm_data, tlat, tlon):
    """Split a tile response into local metre lists (east, north of tile centre)."""
    rows = {"tree_rows": [], "woods": [], "farms": [], "tracks": [], "roads": [],
            "waters": [], "buildings": []}
    for el in osm_data.get("elements", []):
        tags = el.get("tags", {})
        if el.get("type") == "count":
            continue
        if el.get("type") == "way" and "bounds" in el and not el.get("geometry"):
            b = el["bounds"]
            rows["buildings"].append((b["minlat"], b["minlon"], b["maxlat"], b["maxlon"]))
            continue
        geom = el.get("geometry") or (el.get("members") and None)
        pts = None
        closed = False
        if el.get("type") == "way" and el.get("geometry"):
            pts = [to_en(g["lat"], g["lon"], tlat, tlon) for g in el["geometry"]]
            closed = len(pts) > 3 and pts[0] == pts[-1]
        elif el.get("type") == "relation":
            ring = []
            for m in el.get("members", []):
                if m.get("role") in ("outer", "") and m.get("geometry"):
                    ring += [to_en(g["lat"], g["lon"], tlat, tlon) for g in m["geometry"]]
            if len(ring) >= 4:
                pts, closed = ring, True
        if not pts:
            continue
        nat, lu, hw, ww = tags.get("natural"), tags.get("landuse"), tags.get("highway"), tags.get("waterway")
        if nat == "tree_row":
            rows["tree_rows"].append(pts)
        elif (lu == "forest" or nat == "wood") and closed:
            rows["woods"].append(pts)
        elif lu in ("farmland", "orchard", "vineyard") and closed:
            rows["farms"].append(pts)
        elif hw == "track":
            rows["tracks"].append(pts)
        elif hw:
            rows["roads"].append(pts)
        elif (nat == "water" or ww == "riverbank") and closed:
            rows["waters"].append(pts)
    return rows


def ring_bbox(pts):
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    return min(xs), min(ys), max(xs), max(ys)


def area_perimeter(pts):
    a = p = 0.0
    for i in range(len(pts)):
        x1, y1 = pts[i - 1]
        x2, y2 = pts[i]
        a += x1 * y2 - x2 * y1
        p += math.hypot(x2 - x1, y2 - y1)
    return abs(a) / 2.0, p


def is_strip(ring):
    """A closed wood ring that is a narrow long strip (a windbreak drawn as a polygon)."""
    if len(ring) < 4:
        return False
    a, p = area_perimeter(ring)
    if not a or not p:
        return False
    return p * p / a > 25 and 2 * a / p < STRIP_MAX_W


def belt_chains(rows):
    """Windbreak belts: tree_row ways joined into chains, plus narrow wood strips (as rings)."""
    chains = merge_chains(rows["tree_rows"])
    for w in rows["woods"]:
        ring = w[:-1] if len(w) > 3 and w[0] == w[-1] else w
        if is_strip(ring):
            chains.append(ring)
    return chains


def grove_rings(rows):
    out = []
    for w in rows["woods"]:
        ring = w[:-1] if len(w) > 3 and w[0] == w[-1] else w
        if not is_strip(ring):
            out.append(ring)
    return out


def score_tile(region_id, tlat, tlon, half_x, half_y, rows):
    """Slide rotated windows over one tile; return candidate dicts."""
    chains = belt_chains(rows)
    if len(chains) < 2:
        return []
    groves_all = grove_rings(rows)
    beta = dominant_bearing(chains)
    rotation = (beta + 90.0) % 180.0   # belts run across the line of advance
    th = math.radians(rotation)
    cos_t, sin_t = math.cos(th), math.sin(th)

    def to_window(e, n):
        # u across, v towards the spawn edge; b = -v is towards the defence
        return e * cos_t - n * sin_t, -(e * sin_t + n * cos_t)

    # transform all geometry to the window frame once (b grows towards the defence)
    def wf(pts):
        return [to_window(e, n) for e, n in pts]

    chains_w = [wf(c) for c in chains]
    groves_w = [wf(g) for g in groves_all]
    tracks_w = [wf(t) for t in rows["tracks"]]
    farms_w = [wf(f) for f in rows["farms"]]
    waters_w = [wf(w) for w in rows["waters"]]

    # window centres: cover the core tile (without the data margin)
    core_x, core_y = half_x - MARGIN_M, half_y - MARGIN_M
    out = []
    cu = -core_x + WIN_ACROSS / 2
    while cu <= core_x - WIN_ACROSS / 2 + 1e-6:
        cv = -core_y + WIN_ALONG / 2
        while cv <= core_y - WIN_ALONG / 2 + 1e-6:
            win = score_window(region_id, tlat, tlon, rotation, cu, cv,
                               tracks_w, groves_w, farms_w, waters_w, rows["buildings"],
                               chains_w)
            if win:
                out.append(win)
            cv += STEP_ALONG
        cu += STEP_ACROSS
    return out


def score_window(region_id, tlat, tlon, rotation, cu, cv, tracks_w, groves_w, farms_w,
                 waters_w, buildings, chains_w):
    hw, hh = WIN_ACROSS / 2, WIN_ALONG / 2

    def local(pts):
        return [(u - cu, v - cv) for u, v in pts]

    belts = 0
    for ch in chains_w:
        lp = local(ch)
        a_in = [p for p in lp if -hh - 100 <= p[1] <= hh + 100]
        if not a_in:
            continue
        amin = min(p[0] for p in a_in)
        amax = max(p[0] for p in a_in)
        if amin < -hw + 100 and amax > hw - 100:
            belts += 1
    tracks = sum(1 for t in tracks_w
                 if any(-hw <= p[0] <= hw and -hh <= p[1] <= hh for p in local(t)))
    groves = 0
    for w in groves_w:
        lp = local(w)
        bx0, by0, bx1, by1 = ring_bbox(lp)
        if bx1 > -hw and bx0 < hw and by1 > -hh and by0 < hh:
            area = max(0.0, min(bx1, hw) - max(bx0, -hw)) * max(0.0, min(by1, hh) - max(by0, -hh))
            if area > 3000:
                groves += 1
    fields = 0
    farm_area = 0.0
    for f in farms_w:
        lp = local(f)
        bx0, by0, bx1, by1 = ring_bbox(lp)
        if bx1 > -hw and bx0 < hw and by1 > -hh and by0 < hh:
            fields += 1
            farm_area += max(0.0, min(bx1, hw) - max(bx0, -hw)) * max(0.0, min(by1, hh) - max(by0, -hh))
    ponds = 0
    for w in waters_w:
        bx0, by0, bx1, by1 = ring_bbox(local(w))
        if bx1 > -hw and bx0 < hw and by1 > -hh and by0 < hh:
            ponds += 1
    # buildings: window rect in lat/lon vs building bboxes
    ky = 110574.0
    kx = 111320.0 * math.cos(math.radians(tlat))
    th = math.radians(rotation)
    corners = []
    for du, dv in ((-hw, -hh), (hw, -hh), (hw, hh), (-hw, hh)):
        u, b = cu + du, cv + dv
        e = u * math.cos(th) - b * math.sin(th)
        n = -u * math.sin(th) - b * math.cos(th)
        corners.append((tlat + n / ky, tlon + e / kx))
    lats = [c[0] for c in corners]
    lons = [c[1] for c in corners]
    wb = (min(lats), min(lons), max(lats), max(lons))
    n_buildings = sum(1 for b in buildings
                      if b[2] > wb[0] and b[0] < wb[2] and b[3] > wb[1] and b[1] < wb[3])

    if belts < 2 or tracks < 1 or groves < 1 or n_buildings > MAX_BUILDINGS:
        return None
    if fields < 2 and farm_area / (WIN_ACROSS * WIN_ALONG) < 0.5:
        return None

    score = (min(belts, 5) * 4.0 + min(tracks, 3) * 2.0 + min(groves, 3) * 2.0
             + (5.0 if fields >= 3 else 3.0) + (1.0 if ponds else 0.0)
             + 3.0 * min(1.0, farm_area / (WIN_ACROSS * WIN_ALONG))
             - min(5.0, n_buildings * 0.2))
    # centre lat/lon of the window (u across, b towards the defence)
    ce = cu * math.cos(th) - cv * math.sin(th)
    cn = -cu * math.sin(th) - cv * math.cos(th)
    clat = tlat + cn / ky
    clon = tlon + ce / kx
    bbox = osm.window_bbox(clat, clon, rotation, osm.DEFAULT_SCALE, margin_m=0.0)
    return {
        "region": region_id, "centre_lat": round(clat, 6), "centre_lon": round(clon, 6),
        "rotation_deg": round(rotation, 1), "scale": osm.DEFAULT_SCALE,
        "bbox": ";".join(f"{v:.6f}" for v in bbox),
        "score": round(score, 2), "belts": belts, "tracks": tracks, "groves": groves,
        "fields": fields, "ponds": ponds, "buildings": n_buildings,
    }


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--regions", default="", help="comma-separated region ids (default: all)")
    ap.add_argument("--top", type=int, default=40, help="how many windows to write (default 40)")
    ap.add_argument("--workers", type=int, default=4, help="parallel Overpass queries")
    ap.add_argument("--out", default=os.path.join(osm.ROOT, "tools", "maps", "candidates.csv"))
    args = ap.parse_args(argv)

    regions = [r for r in REGIONS if not args.regions or r[0] in args.regions.split(",")]
    tiles = []
    for r in regions:
        tiles += tile_boxes(r)
    print(f"{len(regions)} regions, {len(tiles)} tiles", file=sys.stderr)

    all_windows = []
    done = 0

    def work(tile):
        rid, tlat, tlon, hx, hy = tile
        bbox = tile_bbox(tlat, tlon, hx, hy)
        data, key = osm.overpass(osm.features_query(bbox))
        rows = parse_tile(data, tlat, tlon)
        return score_tile(rid, tlat, tlon, hx, hy, rows)

    with ThreadPoolExecutor(max_workers=args.workers) as pool:
        for wins in pool.map(work, tiles):
            all_windows.extend(wins)
            done += 1
            if done % 20 == 0:
                print(f"  {done}/{len(tiles)} tiles, {len(all_windows)} candidate windows", file=sys.stderr)

    all_windows.sort(key=lambda w: -w["score"])
    top = all_windows[: args.top]
    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    with open(args.out, "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, fieldnames=["rank", "region", "centre_lat", "centre_lon",
                                           "rotation_deg", "scale", "bbox", "score", "belts",
                                           "tracks", "groves", "fields", "ponds", "buildings"],
                           lineterminator="\n")
        w.writeheader()
        for i, win in enumerate(top, 1):
            win["rank"] = i
            w.writerow(win)
    print(f"wrote {len(top)} windows to {args.out}", file=sys.stderr)
    for win in top[:10]:
        print(f"  #{win['rank']} {win['region']} {win['centre_lat']},{win['centre_lon']} "
              f"rot {win['rotation_deg']} score {win['score']} belts {win['belts']} "
              f"tracks {win['tracks']} groves {win['groves']} fields {win['fields']} "
              f"bld {win['buildings']}", file=sys.stderr)


if __name__ == "__main__":
    main()
