#!/usr/bin/env python3
"""Squads for the WaveCost.cs squad mode, task mistral/squads (2026-11-27).

Writes squads.csv with every squad of SIZE defender types from the 9 defence types in units.csv,
plus one squad "all" with all 9 types. A squad name is the short names of its types joined by "+"
(for example mg+sn+at+ad). WaveCost.cs reads the file as csv "squad,defenders" (defenders split by "|").

Usage: make_squads.py <units.csv> <out csv> [size=4]
"""
import csv
import itertools
import sys

units_path, out = sys.argv[1], sys.argv[2]
SIZE = int(sys.argv[3]) if len(sys.argv) > 3 else 4

ABBR = {"rifle_squad": "rs", "machine_gun": "mg", "sniper": "sn", "mortar": "mo",
        "anti_tank": "at", "air_defence": "ad", "fpv_operator": "fpv",
        "bomber_operator": "bo", "recon_drone": "rd"}

types = [r["id"] for r in csv.DictReader(open(units_path, newline="", encoding="utf-8"))
         if r["side"] == "defence"]
assert len(types) == 9, "expected the 9 defence types of units.csv; when a defender type is added, extend ABBR and this check: " + str(types)
for t in types:
    assert t in ABBR, "no short name for " + t

rows = []
for combo in itertools.combinations(types, SIZE):
    rows.append(("+".join(ABBR[t] for t in combo), "|".join(combo)))
rows.append(("all", "|".join(types)))
rows.sort()

with open(out, "w", newline="", encoding="utf-8") as fh:
    w = csv.writer(fh, lineterminator="\n")
    w.writerow(["squad", "defenders"])
    w.writerows(rows)
print("%s: %d squads of %d types + all" % (out, len(rows) - 1, SIZE))
