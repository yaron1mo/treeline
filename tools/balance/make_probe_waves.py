#!/usr/bin/env python3
"""Probe waves for the attacker cost model, version 2 (2026-11-24).

These waves are not for play. They measure what each attacker costs the defence:
1. Single-type waves: n attackers of one type, for n in COUNTS. They give the cost curve of each type.
2. Pair waves: two types at a fixed count each. They show what a type costs when a second counter is already in the wave.
3. Triple waves: three types. They show if pair terms are enough.
4. Mixed waves: 2-4 types with random counts, generated with a seed that differs from the pair and triple waves. They are the test set: the model must predict them without seeing them.

Usage: make_probe_waves.py <units.csv> <out csv> [mixed waves=40] [seed=11]
The output has the waves.csv format, plus a 'probe' column (single:<unit>:<n>, pair:<u>+<v>, triple:<u>+<v>+<w> or mixed).
"""
import csv
import random
import sys

units_path, out = sys.argv[1], sys.argv[2]
MIXED = int(sys.argv[3]) if len(sys.argv) > 3 else 40
rng = random.Random(int(sys.argv[4]) if len(sys.argv) > 4 else 11)

ATTACKERS = ["recruit", "regular", "special_forces", "motorbike", "buggy", "van", "mrap", "ifv",
             "old_tank", "modern_tank", "helicopter"]
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


COUNTS = [1, 2, 4, 6, 8, 10, 12, 16, 20, 30]


def counts(u):
    return [n for n in COUNTS if n <= cap(u)]


def rep(u):
    """The count of a type in a pair or triple wave: big enough to give a clear cost."""
    t = units[u]["type"]
    if t == "troops":
        return 10
    if t == "aviation":
        return 2
    return 4 if heavy(u) else 6


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
for i, u in enumerate(ATTACKERS):
    for v in ATTACKERS[i + 1:]:
        add_wave([(u, rep(u)), (v, rep(v))], f"pair:{u}+{v}")
TRIPLES = [
    ("old_tank", "ifv", "mrap"),
    ("old_tank", "modern_tank", "ifv"),
    ("special_forces", "old_tank", "van"),
    ("regular", "modern_tank", "helicopter"),
    ("special_forces", "buggy", "motorbike"),
    ("recruit", "mrap", "helicopter"),
    ("special_forces", "ifv", "helicopter"),
    ("van", "mrap", "modern_tank"),
]
for t in TRIPLES:
    add_wave([(u, rep(u)) for u in t], "triple:" + "+".join(t))
for _ in range(MIXED):
    k = rng.choice([2, 2, 3, 3, 4])
    types = rng.sample(ATTACKERS, k)
    add_wave([(u, rng.randint(1, cap(u))) for u in types], "mixed")

with open(out, "w", newline="", encoding="utf-8") as fh:
    wr = csv.writer(fh, lineterminator="\n")
    wr.writerow(["wave", "unit", "count", "delay_s", "interval_s", "notes"])
    wr.writerows(rows)
print(f"{w} probe waves")
