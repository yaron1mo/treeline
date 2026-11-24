#!/usr/bin/env python3
"""Probe waves for the attacker cost model, version 5 (2026-11-24).

These waves are not for play. They measure what each attacker costs the defence:
1. Single-type waves: n attackers of one type, for n in COUNTS. They give the cost curve of each type.
2. Pair waves: two types. Level 1 is (rep, rep) for every pair. Level 2 is (half, rep) for pairs
   without a helicopter, and (rep, 1 helicopter) for pairs with one, so the helicopter level that
   mixed waves use (1-3) is measured too.
3. Triple waves: three types. They show if pair terms are enough.
4. Mixed waves: waves with the same structure as make_waves.py builds for play (a new type where it
   unlocks, two or three older types, infantry filler, heavy types for the rest), in two sets: 120
   train waves (seed 51) and 40 test waves (seed 61). The model is fitted on the train waves; the
   test waves judge it.

Usage: make_probe_waves.py <units.csv> <out csv> [train=120] [test=40]
The output has the waves.csv format, plus a 'notes' column (single:<unit>:<n>, pair:<u>+<v>@<level>, triple:<u>+<v>+<w>, mixed-train or mixed).
"""
import csv
import random
import sys

units_path, out = sys.argv[1], sys.argv[2]
TRAIN = int(sys.argv[3]) if len(sys.argv) > 3 else 120
TEST = int(sys.argv[4]) if len(sys.argv) > 4 else 40

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


COUNTS = [4, 6, 8, 10, 12, 16, 20, 30]


def counts(u):
    return [n for n in COUNTS if n <= cap(u)] or [cap(u)]


def rep(u):
    """The count of a type in a pair or triple wave: big enough to give a clear cost."""
    t = units[u]["type"]
    if t == "aviation":
        return 2
    if t == "troops":
        return 10
    return 4 if heavy(u) else 6


def half(u):
    return max(1, rep(u) // 2)


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
        add_wave([(u, rep(u)), (v, rep(v))], f"pair:{u}+{v}@1")
        if "helicopter" in (u, v):
            other = v if u == "helicopter" else u
            add_wave([(other, rep(other)), ("helicopter", 1)], f"pair:{u}+{v}@2")
        else:
            add_wave([(u, half(u)), (v, rep(v))], f"pair:{u}+{v}@2")
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
# Game waves: the same structure as make_waves.py produces for play, so the formula is judged on
# waves the game really sends: a new type where it unlocks, two or three older types, infantry filler,
# heavy types for what is left, all under the same threat budget curve.
UNLOCK = [(1, "recruit"), (2, "regular"), (3, "motorbike"), (4, "buggy"), (5, "van"), (6, "special_forces"),
          (7, "old_tank"), (8, "ifv"), (9, "mrap"), (12, "modern_tank"), (14, "helicopter")]
INFANTRY = ["recruit", "regular", "special_forces"]
tv = {k: float(r["threat_value"]) for k, r in units.items() if r["side"] == "attack" and r.get("threat_value")}


def game_cap(u):
    t = units[u]["type"]
    if t == "troops":
        return 30
    if t == "aviation":
        return 3
    return 8 if heavy(u) else 10


def game_wave(rng, w, first=50.0, growth=1.22):
    budget = first * growth ** (w - 1)
    unlocked = [u for (k, u) in UNLOCK if k <= w]
    new = [u for (k, u) in UNLOCK if k == w]
    groups = {}

    def add(u, n):
        n = min(n, game_cap(u) - groups.get(u, 0))
        if n > 0:
            groups[u] = groups.get(u, 0) + n
        return max(0, n)

    left = budget
    for u in new:
        n = add(u, max(1, round(0.35 * budget / tv[u])))
        left -= n * tv[u]
    others = [u for u in unlocked if u not in new and u != "recruit"]
    picks = []
    for _ in range(min(len(others), rng.choice([2, 3]))):
        weights = [1 + unlocked.index(u) for u in others if u not in picks]
        cand = [u for u in others if u not in picks]
        if cand:
            picks.append(rng.choices(cand, weights=weights)[0])
    for u in picks:
        share = left * rng.uniform(0.25, 0.45)
        n = add(u, int(share // tv[u]))
        left -= n * tv[u]
    filler = "recruit" if w < 6 else "regular"
    left -= add(filler, max(0, int(round(left / tv[filler])))) * tv[filler]
    heavy_types = sorted((u for u in unlocked if tv[u] >= 50), key=lambda u: tv[u])
    while heavy_types and left >= tv[heavy_types[0]]:
        pool = [h for h in heavy_types if tv[h] <= left]
        if not pool:
            break
        u = rng.choice(pool)
        if add(u, 1) == 0:
            heavy_types.remove(u)
            continue
        left -= tv[u]
    return list(groups.items())


for seed, k_total, tag in ((51, TRAIN, "mixed-train"), (61, TEST, "mixed")):
    rng = random.Random(seed)
    for _ in range(k_total):
        level = rng.randint(1, 20)
        add_wave(game_wave(rng, level), tag)

with open(out, "w", newline="", encoding="utf-8") as fh:
    wr = csv.writer(fh, lineterminator="\n")
    wr.writerow(["wave", "unit", "count", "delay_s", "interval_s", "notes"])
    wr.writerows(rows)
print(f"{w} probe waves")
