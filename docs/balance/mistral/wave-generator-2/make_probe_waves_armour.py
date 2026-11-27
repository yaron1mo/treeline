#!/usr/bin/env python3
"""Armour probe waves for the wave-generator-2 training set (issue #17).

Finding 5 of the Claude review 2 on PR #9: wave 21 (the first old_tank wave) was
underpriced by about 2x in three runs, because the model has too few measured waves
with armour and little infantry. These probe waves measure that area:

1. 1-3 armour units plus 1-4 infantry (recruit or regular): the shape of the first
   armour waves.
2. Armour plus special forces, and armour plus light vehicles (buggy, van, motorbike).
3. Armour pairs with little or no infantry (wave 22 of PR #9 cost less than expected).
4. One helicopter with a small escort.

Usage: make_probe_waves_armour.py <units.csv> <out csv> [start wave=31]
The output has the waves.csv format. The notes column is the training tag with the
shape, for example `mixed-train4:old_tank+2regular`; the fit reads the tag before the
colon (`fit_model.py`, tag_of).
"""

import csv
import sys

units_path, out = sys.argv[1], sys.argv[2]
START = int(sys.argv[3]) if len(sys.argv) > 3 else 31

units = {r["id"]: r for r in csv.DictReader(open(units_path, newline="", encoding="utf-8"))}


def interval(u):
    t = units[u]["type"]
    if t == "troops":
        return 1.0
    if t == "aviation":
        return 8.0
    return 6.0 if units[u]["armour"] in ("medium", "high") else 2.5


ARMOUR = ["old_tank", "ifv", "mrap", "modern_tank"]

probes = []
for a in ARMOUR:
    for inf in ("recruit", "regular"):
        probes += [
            {a: 1, inf: 1},
            {a: 1, inf: 2},
            {a: 1, inf: 4},
            {a: 2, inf: 2},
            {a: 2, inf: 4},
            {a: 3, inf: 2},
        ]
for a in ("old_tank", "ifv", "modern_tank"):
    probes += [{a: 1, "special_forces": 2}, {a: 2, "special_forces": 2}]
for a in ARMOUR:
    probes += [{a: 1, "buggy": 2}, {a: 1, "van": 1}, {a: 2, "motorbike": 2}]
probes += [
    {"old_tank": 1, "ifv": 1},
    {"old_tank": 1, "ifv": 1, "recruit": 2},
    {"modern_tank": 1, "old_tank": 1},
    {"mrap": 2, "ifv": 1},
    {"ifv": 2, "old_tank": 1, "regular": 2},
    {"old_tank": 2, "mrap": 1},
]
probes += [
    {"helicopter": 1, "recruit": 2},
    {"helicopter": 1, "buggy": 2},
    {"helicopter": 1, "regular": 4},
]

rows = []
w = START - 1
for comp in probes:
    w += 1
    shape = "+".join(f"{n}{u}" for u, n in comp.items())
    # Infantry first, then vehicles, aviation last, as in make_waves.py.
    order = sorted(comp, key=lambda u: (units[u]["type"] != "troops", units[u]["type"] == "aviation"))
    delay = 0.0
    for u in order:
        n = comp[u]
        rows.append([w, u, n, round(delay, 1), interval(u), f"mixed-train4:{shape}"])
        delay += min(12.0, 4.0 + n * interval(u) * 0.5)

with open(out, "w", newline="", encoding="utf-8") as fh:
    wr = csv.writer(fh, lineterminator="\n")
    wr.writerow(["wave", "unit", "count", "delay_s", "interval_s", "notes"])
    wr.writerows(rows)

print(f"{len(probes)} probe waves, waves {START}..{w} -> {out}")
