#!/usr/bin/env python3
"""Build the real-*.json field files from tools/maps/candidates.csv.

Takes the best candidate windows and writes one game map each:
  unity/Assets/Resources/Fields/real-sector-XX.json (+ .meta)

Each map gets a fictional sector name (Sector 08, Sector 09, ...; numbers already
used by the existing maps are skipped) and no real place names. The scale factor
and the Overpass cache key are stored inside every map ("scale", "source").

    python3 tools/maps/build_maps.py                 # top 24 candidates
    python3 tools/maps/build_maps.py --count 20 --start 8

No extra packages: standard-library Python 3.8+.
Map data (c) OpenStreetMap contributors, ODbL.
"""

import argparse
import csv
import hashlib
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import osm_to_field as osm  # noqa: E402

FIELDS_DIR = os.path.join(osm.ROOT, "unity", "Assets", "Resources", "Fields")


def used_sectors():
    used = set()
    if os.path.isdir(FIELDS_DIR):
        for name in os.listdir(FIELDS_DIR):
            if not name.endswith(".json"):
                continue
            try:
                with open(os.path.join(FIELDS_DIR, name), encoding="utf-8") as fh:
                    doc = json.load(fh)
                sector = doc.get("sector") or ""
                if sector.lower().startswith("sector"):
                    used.add(int(sector.split()[-1]))
            except (ValueError, OSError, json.JSONDecodeError):
                continue
    return used


def next_seors(count, start, used):
    out = []
    n = start
    while len(out) < count:
        if n not in used:
            out.append(n)
        n += 1
    return out


def select(rows, count, max_per_region, min_km):
    """Greedy by rank, but diversify: at most max_per_region per region and at least
    min_km between chosen windows (so the maps are not near-duplicates)."""
    import math
    picked = []
    per_region = {}
    for r in sorted(rows, key=lambda r: int(r["rank"])):
        if len(picked) >= count:
            break
        region = r["region"]
        if per_region.get(region, 0) >= max_per_region:
            continue
        lat, lon = float(r["centre_lat"]), float(r["centre_lon"])
        too_close = False
        for p in picked:
            plat, plon = float(p["centre_lat"]), float(p["centre_lon"])
            d = math.hypot((lon - plon) * 111320.0 * math.cos(math.radians(lat)),
                           (lat - plat) * 110574.0) / 1000.0
            if d < min_km:
                too_close = True
                break
        if too_close:
            continue
        picked.append(r)
        per_region[region] = per_region.get(region, 0) + 1
    return picked


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--candidates", default=os.path.join(osm.ROOT, "tools", "maps", "candidates.csv"))
    ap.add_argument("--count", type=int, default=24, help="how many maps to build (default 24)")
    ap.add_argument("--start", type=int, default=8, help="first sector number (default 8; Forest belts is Sector 07)")
    ap.add_argument("--scale", type=float, default=osm.DEFAULT_SCALE, help="real metres per game metre")
    ap.add_argument("--max-per-region", type=int, default=8, help="at most this many maps per region")
    ap.add_argument("--min-km", type=float, default=2.5, help="minimum distance between chosen windows")
    ap.add_argument("--only", default="", help="comma-separated ranks to build (default: diverse top N)")
    args = ap.parse_args(argv)

    with open(args.candidates, newline="", encoding="utf-8") as fh:
        rows = list(csv.DictReader(fh))
    if args.only:
        want = {int(x) for x in args.only.split(",")}
        rows = [r for r in rows if int(r["rank"]) in want]
    else:
        rows = select(rows, args.count, args.max_per_region, args.min_km)
    rows = rows[: args.count]

    sectors = next_seors(len(rows), args.start, used_sectors())
    os.makedirs(FIELDS_DIR, exist_ok=True)
    for row, num in zip(rows, sectors):
        sector = f"Sector {num:02d}"
        fname = f"real-sector-{num:02d}.json"
        out = os.path.join(FIELDS_DIR, fname)
        field, key = osm.build(float(row["centre_lat"]), float(row["centre_lon"]),
                               float(row["rotation_deg"]), args.scale,
                               sector, sector, row["region"], seed=int(row["rank"]))
        guid = hashlib.md5(f"treeline-field-{fname}".encode()).hexdigest()
        osm.write_field(field, out, meta_guid=guid)
        counts = {}
        for f in field["features"]:
            counts[f["type"]] = counts.get(f["type"], 0) + 1
        print(f"{fname}  rank {row['rank']} {row['region']} score {row['score']} "
              f"rot {row['rotation_deg']} spawns {field['spawns']}  "
              + ", ".join(f"{n} {t}" for t, n in sorted(counts.items())))


if __name__ == "__main__":
    main()
