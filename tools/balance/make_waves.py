#!/usr/bin/env python3
"""Generate a wave plan from a threat budget (Yaroslav, 2026-11-16).

Each attacker has a threat_value (units.csv: what it costs the cheapest fitting defence to stop one, rifle squad = 100).
Wave w gets a budget first * growth^(w-1). A unit that unlocks in a wave is featured (about 35% of the budget), the rest
goes to two or three other unlocked types, newer ones more often, and infantry fills what is left.

Usage: make_waves.py <units.csv> <out waves.csv> [waves=20] [first=50] [growth=1.22] [seed=7] [exclude=a,b]
"""
import csv
import random
import sys

units_path, out = sys.argv[1], sys.argv[2]
N = int(sys.argv[3]) if len(sys.argv) > 3 else 20
FIRST = float(sys.argv[4]) if len(sys.argv) > 4 else 50
GROWTH = float(sys.argv[5]) if len(sys.argv) > 5 else 1.22
rng = random.Random(int(sys.argv[6]) if len(sys.argv) > 6 else 7)
EXCLUDE = set(sys.argv[7].split(",")) if len(sys.argv) > 7 and sys.argv[7] else set()

units = {r["id"]: r for r in csv.DictReader(open(units_path, newline="", encoding="utf-8"))}
tv = {k: float(r["threat_value"]) for k, r in units.items() if r["side"] == "attack" and r.get("threat_value")}

UNLOCK = [(1, "recruit"), (2, "regular"), (3, "motorbike"), (4, "buggy"), (5, "van"), (6, "special_forces"),
          (7, "old_tank"), (8, "ifv"), (9, "mrap"), (10, "fpv"), (12, "modern_tank"), (14, "helicopter"),
          (16, "one_way_drone")]
UNLOCK = [(k, u) for (k, u) in UNLOCK if u not in EXCLUDE]
INFANTRY = ["recruit", "regular", "special_forces"]


def cap(u):
    t = units[u]["type"]
    if t == "troops":
        return 30
    if t == "aviation":
        return 3
    return 8 if units[u]["armour"] in ("medium", "high") else 10


def interval(u):
    t = units[u]["type"]
    if t == "troops":
        return 1.0
    if t == "aviation":
        return 8.0
    return 6.0 if units[u]["armour"] in ("medium", "high") else 2.5


rows = []
for w in range(1, N + 1):
    budget = FIRST * GROWTH ** (w - 1)
    unlocked = [u for (k, u) in UNLOCK if k <= w]
    new = [u for (k, u) in UNLOCK if k == w]
    groups = {}

    def add(u, n):
        n = min(n, cap(u) - groups.get(u, 0))
        if n > 0:
            groups[u] = groups.get(u, 0) + n
        return max(0, n)

    left = budget
    for u in new:
        n = add(u, max(1, round(0.35 * budget / tv[u])))
        left -= n * tv[u]
    # Two or three other types, newer ones more often.
    others = [u for u in unlocked if u not in new and u not in ("recruit",)]
    picks = []
    for _ in range(min(len(others), rng.choice([2, 3]))):
        weights = [1 + unlocked.index(u) for u in others if u not in picks]
        cand = [u for u in others if u not in picks]
        picks.append(rng.choices(cand, weights=weights)[0])
    for u in picks:
        share = left * rng.uniform(0.25, 0.45)
        n = add(u, int(share // tv[u]))
        left -= n * tv[u]
    # Infantry fills the rest: recruits early, regulars later.
    filler = "recruit" if w < 6 else "regular"
    left -= add(filler, max(0, int(round(left / tv[filler])))) * tv[filler]
    # Whatever the caps left over goes to the heavier unlocked units, cheapest first.
    heavy = sorted((u for u in unlocked if tv[u] >= 50 and u != "recon_uav"), key=lambda u: tv[u])
    while heavy and left >= tv[heavy[0]]:
        u = rng.choice([h for h in heavy if tv[h] <= left])
        if add(u, 1) == 0:
            heavy.remove(u)
            continue
        left -= tv[u]
    if "fpv" in groups:
        add("recon_uav", 1)
    # Order: infantry first, then vehicles, aviation last; delays staggered.
    order = sorted(groups, key=lambda u: (units[u]["type"] != "troops", units[u]["type"] == "aviation", tv[u]))
    delay = 0.0
    for u in order:
        n = groups[u]
        note = "new" if u in new else ""
        rows.append([w, u, n, round(delay, 1), interval(u), note])
        delay += min(12.0, 4.0 + n * interval(u) * 0.5)
    total = sum(n * tv[u] for u, n in groups.items())
    print(f"wave {w:2}: budget {budget:6.0f}, used {total:6.0f}: " + ", ".join(f"{n} {u}" for u, n in groups.items()))

with open(out, "w", newline="", encoding="utf-8") as fh:
    wr = csv.writer(fh, lineterminator="\n")
    wr.writerow(["wave", "unit", "count", "delay_s", "interval_s", "notes"])
    wr.writerows(rows)
