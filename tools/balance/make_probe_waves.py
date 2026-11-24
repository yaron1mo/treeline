#!/usr/bin/env python3
"""Probe waves for the attacker cost model (2026-11-24).

These waves are not for play. They measure what each attacker costs the defence:
1. Single-type waves: n attackers of one type, for n in COUNTS. They give the cost curve of each type.
2. Mixed waves: 2-4 types with random counts. They show if the costs of types add up or share defenders.

Usage: make_probe_waves.py <units.csv> <out csv> [mixed waves=60] [seed=11]
The output has the waves.csv format, plus a 'probe' column (single:<unit>:<n> or mixed).
"""
import csv
import random
import sys

units_path, out = sys.argv[1], sys.argv[2]
MIXED = int(sys.argv[3]) if len(sys.argv) > 3 else 60
rng = random.Random(int(sys.argv[4]) if len(sys.argv) > 4 else 11)

ATTACKERS = ["recruit", "regular", "special_forces", "motorbike", "buggy", "van", "mrap", "ifv",
             "old_tank", "modern_tank", "helicopter"]
COUNTS = [1, 2, 3, 5, 8, 12, 20]
units = {r["id"]: r for r in csv.DictReader(open(units_path, newline="", encoding="utf-8"))}


def heavy(u):
    return units[u]["armour"] in ("medium", "high")


def interval(u):
    t = units[u]["type"]
    if t == "troops":
        return 1.0
    if t == "aviation":
        return 8.0
    return 6.0 if heavy(u) else 2.5


def cap(u):
    t = units[u]["type"]
    if t == "troops":
        return 30
    if t == "aviation":
        return 3
    return 8 if heavy(u) else 12


def counts(u):
    c = [n for n in COUNTS if n <= cap(u)]
    if units[u]["type"] == "troops":
        c.append(30)
    return c


rows = []
w = 0


def add_wave(groups, probe):
    global w
    w += 1
    # Infantry first, then vehicles, aviation last, as in make_waves.py.
    order = sorted(groups, key=lambda g: (units[g[0]]["type"] != "troops", units[g[0]]["type"] == "aviation"))
    delay = 0.0
    for u, n in order:
        rows.append([w, u, n, round(delay, 1), interval(u), probe])
        delay += min(12.0, 4.0 + n * interval(u) * 0.5)


for u in ATTACKERS:
    for n in counts(u):
        add_wave([(u, n)], f"single:{u}:{n}")
for _ in range(MIXED):
    k = rng.choice([2, 2, 3, 3, 4])
    types = rng.sample(ATTACKERS, k)
    add_wave([(u, rng.randint(1, cap(u))) for u in types], "mixed")

with open(out, "w", newline="", encoding="utf-8") as fh:
    wr = csv.writer(fh, lineterminator="\n")
    wr.writerow(["wave", "unit", "count", "delay_s", "interval_s", "notes"])
    wr.writerows(rows)
print(f"{w} probe waves")
