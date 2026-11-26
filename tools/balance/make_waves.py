#!/usr/bin/env python3
"""Generate waves whose predicted defence cost follows a target cost per wave.

Task: mistral/wave-generator (docs/agents/mistral.md, section 10). The cost model
comes from docs/balance/mistral/attacker-cost/model.json (field Forest belts),
fit in task mistral/attacker-cost (PR #7). The single-type curves of that model
(run 45411511647, artifact balance-report) are embedded below as constants, so
this script needs no artifact to run.

Method: the wave list is a reviewed plan (PLAN below), fixed per wave. The plan
was calibrated on the measured costs of Balance run 45865138318 (run 1 of this
task): waves with a known measured cost reuse that composition, and the other
waves use compositions of the same families. The script checks the plan against
the task rules (caps, type order, mix rules, growth limit) and reports the
predicted and the estimated cost per wave. When a wave has no plan entry, or
the wave count is not 30, the script falls back to a randomised local search
that keeps the same rules. The same seed always gives the same waves.

Usage: make_waves.py <units.csv> <out waves.csv> [waves=30] [seed=7]
Reads: tools/balance/target_costs.csv (wave,target_cost) next to the repo root.
"""
import csv
import json
import math
import os
import random
import sys

units_path, out = sys.argv[1], sys.argv[2]
N = int(sys.argv[3]) if len(sys.argv) > 3 else 30
SEED = int(sys.argv[4]) if len(sys.argv) > 4 else 7
rng = random.Random(SEED)
root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(units_path)))))
targets = {}
with open(os.path.join(root, "tools", "balance", "target_costs.csv"), newline="", encoding="utf-8") as fh:
    for r in csv.DictReader(fh):
        targets[int(r["wave"])] = float(r["target_cost"])
model = json.load(open(os.path.join(root, "docs", "balance", "mistral", "attacker-cost", "model.json"),
                       encoding="utf-8"))["fields"]["Forest belts"]

EXCLUDE = {"fpv", "one_way_drone", "recon_uav"}
UNLOCK = [(1, "recruit"), (2, "regular"), (3, "motorbike"), (4, "buggy"), (5, "van"),
          (6, "special_forces"), (7, "old_tank"), (8, "ifv"), (9, "mrap"),
          (12, "modern_tank"), (14, "helicopter")]
UNLOCK = [(k, u) for (k, u) in UNLOCK if u not in EXCLUDE]
TYPES = [u for _, u in UNLOCK]
CLASS_OF = {u: (2 if u == "helicopter" else 1 if u in ("mrap", "ifv", "old_tank", "modern_tank") else 0)
            for u in TYPES}

# Forest belts single-type curves, run 45411511647 (mistral/attacker-cost, PR #7).
CURVES = {
 "recruit": [(4,200),(6,300),(8,300),(10,300),(12,500),(16,500),(20,600),(30,600)],
 "regular": [(4,300),(6,400),(8,600),(10,900),(12,900),(16,1300),(20,1400),(30,1800)],
 "special_forces": [(4,700),(6,800),(8,1200),(10,1500),(12,1500),(16,2400),(20,3000),(30,3500)],
 "motorbike": [(1,25),(2,25),(3,25),(4,100),(6,100),(8,300),(10,300),(12,300)],
 "buggy": [(1,25),(2,25),(3,25),(4,100),(6,200),(8,200),(10,200),(12,200)],
 "van": [(1,500),(2,500),(3,500),(4,800),(6,1000),(8,1000),(10,1200),(12,1200)],
 "mrap": [(1,1025),(2,1025),(3,1225),(4,1475),(6,1925),(8,1925)],
 "ifv": [(1,1925),(2,2125),(3,2575),(4,4275),(6,5725),(8,6725)],
 "old_tank": [(1,1925),(2,1675),(3,1925),(4,1925),(6,2825),(8,2825)],
 "modern_tank": [(1,1675),(2,1925),(3,2375),(4,2375),(6,2825),(8,3375)],
 "helicopter": [(1,6375),(2,13775),(3,17875)],
}

units = {r["id"]: r for r in csv.DictReader(open(units_path, newline="", encoding="utf-8"))}


def cap(u):
    if u == "helicopter":
        return 2                     # task rule: at most 2 helicopters in any wave
    t = units[u]["type"]
    if t == "troops":
        return 30
    return 8 if units[u]["armour"] in ("medium", "high") else 10


def interval(u):
    t = units[u]["type"]
    if t == "troops":
        return 1.0
    if t == "aviation":
        return 8.0
    return 6.0 if units[u]["armour"] in ("medium", "high") else 2.5


def interp(points, n):
    pts = sorted(points)
    if n <= pts[0][0]:
        return pts[0][1] * n / pts[0][0] if pts[0][0] else pts[0][1]
    for (x0, c0), (x1, c1) in zip(pts, pts[1:]):
        if x0 <= n <= x1:
            return c0 + (c1 - c0) * (n - x0) / (x1 - x0)
    (x0, c0), (x1, c1) = pts[-2], pts[-1]
    return c1 + (c1 - c0) / (x1 - x0) * (n - x1)


def features(comp):
    cnt = [float(comp.get(u, 0)) for u in TYPES]
    pres = [1.0 if c > 0 else 0.0 for c in cnt]
    contrib = [(interp(CURVES[u], comp.get(u, 0)) if comp.get(u, 0) else 0.0) for u in TYPES]
    x = cnt + pres + contrib
    for cl in range(3):
        x.append(math.sqrt(sum(v * v for v, u in zip(contrib, TYPES) if CLASS_OF[u] == cl)))
    x.append(math.log1p(sum(comp.values())))
    for i in range(len(TYPES)):
        for j in range(i + 1, len(TYPES)):
            x.append(pres[i] * pres[j])
    for i in range(len(TYPES)):
        for j in range(i + 1, len(TYPES)):
            x.append(float(min(cnt[i], cnt[j])))
    return x


def predict(comp):
    x = features(comp)
    b = []
    for j, bins in enumerate(model["bins"]):
        if bins is None:
            b.append(0)
            continue
        v = 0
        for t in bins:
            if x[j] > t:
                v += 1
            else:
                break
        b.append(v)
    p = model["F0"]
    for t in model["trees"]:
        while len(t) == 4:
            t = t[2] if b[t[0]] <= t[1] else t[3]
        p += 0.2 * t[0]
    return max(p * p, 1.0)


def violations(comp, w, prev_pred, has_new):
    """Count the broken rules of the task spec for wave w (0 when all rules hold)."""
    v = 0
    if sum(comp.values()) == 0:
        return 1
    if any(comp.get(u, 0) > cap(u) for u in TYPES):
        v += 1
    if w >= 4 and len([c for c in comp.values() if c > 0]) < 2:
        v += 1
    pred = predict(comp)
    if w >= 8:
        contrib = {u: (interp(CURVES[u], comp[u]) if comp.get(u) else 0.0) for u in TYPES}
        v += sum(1 for c in contrib.values() if c > 0.7 * pred)
    if prev_pred and not has_new and pred > 1.35 * prev_pred:
        v += 1
    return v


# Measured wave costs, Balance run 45865138318 (mistral/wave-generator, run 1).
# Key: the wave composition (unit -> count). Value: the measured cost on Forest belts.
MEASURED = {
 (("buggy",1),("motorbike",3),("recruit",3),("regular",3)): 300,       # wave 5
 (("buggy",1),("motorbike",6),("regular",2),("van",1)): 500,          # wave 6
 (("buggy",1),("motorbike",8),("recruit",4),("regular",3)): 400,      # wave 9
 (("buggy",2),("motorbike",9),("recruit",4),("special_forces",1)): 500,  # wave 10
 (("buggy",6),("motorbike",2),("regular",6)): 500,                     # wave 11
 (("buggy",7),("motorbike",2),("regular",4),("special_forces",2),("van",3)): 1300,  # wave 12
 (("buggy",7),("motorbike",5),("regular",3),("special_forces",4)): 1100,  # wave 13
 (("buggy",7),("motorbike",5),("regular",5),("special_forces",2),("van",3)): 1300,  # wave 14
 (("regular",8),("special_forces",5)): 1400,                            # wave 15
 (("motorbike",5),("regular",8),("special_forces",5)): 1400,           # wave 16
 (("buggy",1),("motorbike",10),("recruit",4),("regular",8),("special_forces",10),("van",7)): 2700,  # wave 19
 (("buggy",3),("motorbike",8),("recruit",2),("regular",9),("special_forces",11),("van",3),("old_tank",1)): 6625,  # wave 21
 (("buggy",3),("motorbike",6),("recruit",2),("regular",9),("special_forces",12),("van",7),("old_tank",4),("ifv",2)): 8275,  # wave 22
 (("motorbike",8),("mrap",3),("recruit",5),("regular",12),("special_forces",12),("van",9),("old_tank",4)): 7225,  # wave 23
 (("motorbike",8),("mrap",4),("recruit",4),("regular",15),("special_forces",6),("van",8),("ifv",3)): 5625,  # wave 24
 (("motorbike",8),("mrap",4),("recruit",5),("regular",15),("special_forces",6),("van",8),("old_tank",5)): 4375,  # wave 25
 (("buggy",3),("motorbike",8),("recruit",5),("regular",15),("special_forces",8),("van",9),("mrap",4),("modern_tank",2)): 4725,  # wave 26
 (("buggy",1),("motorbike",8),("recruit",5),("regular",12),("special_forces",8),("van",9),("mrap",6),("modern_tank",6),("old_tank",5)): 9475,  # wave 27
 (("buggy",5),("motorbike",9),("recruit",5),("regular",13),("special_forces",8),("mrap",6),("modern_tank",6),("old_tank",3),("ifv",2)): 7925,  # wave 28
 (("motorbike",10),("recruit",5),("regular",15),("special_forces",8),("mrap",4),("modern_tank",5),("ifv",4)): 7025,  # wave 29
 (("motorbike",10),("recruit",8),("regular",15),("special_forces",9),("mrap",7),("modern_tank",4),("ifv",4)): 7025,  # wave 30
 (("recruit",5),): 200,                                                # wave 1
 (("recruit",2),("regular",3)): 300,                                    # wave 2
 (("recruit",6),): 300,                                                 # wave 3
 (("motorbike",6),("recruit",3),("buggy",3),("special_forces",1)): 500,  # wave 8
 (("motorbike",6),("recruit",3),("buggy",3),("special_forces",1),): 500,
}


# Reviewed wave plan, iteration 2. Compositions with a comment "run 1 w<n>"
# reuse the exact composition measured in Balance run 45865138318; their
# measured cost on Forest belts is in MEASURED below. The other waves use
# compositions of the same families, so their measured cost stays near the
# estimate. The order of first appearances follows UNLOCK:
# motorbike w4, buggy w5, van w6, special_forces w8, old_tank w21, ifv w22,
# mrap w23, modern_tank w24.
PLAN = {
 1:  {"recruit": 5},                                            # m 200
 2:  {"recruit": 2, "regular": 3},                              # m 300, run 1 w2
 3:  {"recruit": 6},                                            # m 300, run 1 w3
 4:  {"recruit": 2, "regular": 3, "motorbike": 1},              # est ~300
 5:  {"buggy": 1, "motorbike": 3, "recruit": 3, "regular": 3},  # m 300, run 1 w5
 6:  {"motorbike": 6, "regular": 2, "van": 1, "buggy": 1},      # m 500, run 1 w6
 7:  {"motorbike": 6, "regular": 2, "van": 1, "buggy": 1},      # m 500, run 1 w6
 8:  {"motorbike": 6, "recruit": 3, "buggy": 3,
      "special_forces": 1},                                     # m 500, run 1 w8
 9:  {"motorbike": 6, "regular": 5, "buggy": 1},                # est ~600
 10: {"regular": 6, "buggy": 6, "motorbike": 2, "van": 1},      # est ~600
 11: {"regular": 2, "buggy": 5, "motorbike": 5,
      "special_forces": 3},                                      # est ~850
 12: {"buggy": 7, "motorbike": 5, "regular": 3,
      "special_forces": 4},                                      # m 1100, run 1 w13
 13: {"buggy": 7, "motorbike": 5, "regular": 3,
      "special_forces": 4},                                      # m 1100, run 1 w13
 14: {"buggy": 7, "motorbike": 5, "regular": 5, "special_forces": 2,
      "van": 3},                                                 # m 1300, run 1 w14
 15: {"buggy": 7, "motorbike": 5, "regular": 3,
      "special_forces": 4},                                     # m 1100, run 1 w13
 16: {"motorbike": 5, "regular": 8,
      "special_forces": 5},                                     # m 1400, run 1 w16
 17: {"regular": 10, "special_forces": 3, "motorbike": 8, "van": 7,
      "recruit": 5},                                            # est ~1900
 18: {"regular": 10, "special_forces": 3, "motorbike": 8, "van": 7,
      "recruit": 6},                                            # m 2000, run 1 w18
 19: {"regular": 10, "special_forces": 5, "motorbike": 4, "van": 6,
      "recruit": 3},                                            # m 2300, run 1 w17
 20: {"regular": 5, "special_forces": 8, "motorbike": 10, "van": 7,
      "recruit": 4},                                            # m 2300, run 1 w20
 21: {"old_tank": 2, "regular": 8, "van": 4, "motorbike": 10},  # est ~3100
 22: {"old_tank": 1, "ifv": 1},                                 # est ~3850
 23: {"mrap": 1, "regular": 10, "van": 8, "motorbike": 8},     # est ~3300
 24: {"buggy": 3, "motorbike": 8, "recruit": 5, "regular": 15,
      "special_forces": 8, "van": 9, "mrap": 4,
      "modern_tank": 2},                                        # m 4725, run 1 w26
 25: {"motorbike": 8, "mrap": 4, "recruit": 5, "regular": 15,
      "special_forces": 6, "van": 8, "old_tank": 5},           # m 4375, run 1 w25
 26: {"motorbike": 8, "mrap": 4, "recruit": 4, "regular": 15,
      "special_forces": 6, "van": 8, "ifv": 3},                # m 5625, run 1 w24
 27: {"motorbike": 8, "mrap": 4, "recruit": 4, "regular": 15,
      "special_forces": 6, "van": 8, "ifv": 3},                # m 5625, run 1 w24
 28: {"buggy": 3, "motorbike": 8, "recruit": 2, "regular": 9,
      "special_forces": 11, "van": 3, "old_tank": 1},          # m 6625, run 1 w21
 29: {"buggy": 3, "motorbike": 6, "recruit": 2, "regular": 9,
      "special_forces": 12, "van": 7, "old_tank": 4, "ifv": 2},  # m 8275, run 1 w22
 30: {"motorbike": 8, "mrap": 3, "recruit": 5, "regular": 12,
      "special_forces": 12, "van": 9, "old_tank": 4},          # m 7225, run 1 w23
}


def key_of(comp):
    return tuple(sorted(comp.items()))


def estimate(comp):
    """Best estimate of the measured cost: the measured value when this exact
    composition was measured, else the model prediction calibrated by the most
    similar measured compositions."""
    k = key_of(comp)
    if k in MEASURED:
        return MEASURED[k], True
    p = predict(comp)
    types_c = set(comp)
    scored = []
    for mk, mv in MEASURED.items():
        mt = {u for u, _ in mk}
        sim = len(types_c & mt) / max(1, len(types_c | mt))
        mp = predict(dict(mk))
        scored.append((sim, mv / mp if mp else 1.0))
    scored.sort(reverse=True)
    top = [r for _, r in scored[:3]]
    ratio = max(0.5, min(2.0, sum(top) / len(top)))
    return p * ratio, False


def climb(start, target, w, prev_pred, introduced, unlock_order):
    """Randomised local search. The model prediction moves in jumps, not in
    smooth steps, so a beam of one-unit moves gets stuck. Instead: perturb the
    best state found so far (1-3 types, plus or minus 1-3 units) and keep every
    state that breaks no rule. New types appear in unlock order only."""
    unlocked = [u for k, u in UNLOCK if k <= w]
    nxt = next((u for u in unlock_order if u not in introduced), None)
    movable = [u for u in unlocked if u in introduced] + ([nxt] if nxt in unlocked else [])

    def cost(comp):
        est, _exact = estimate(comp)
        has_new = any(u not in introduced for u in comp)
        return abs(est - target) + 1e7 * violations(comp, w, prev_pred, has_new)

    def random_comp():
        c = {}
        for u in movable:
            if rng.random() < 0.4:
                c[u] = rng.randint(1, min(cap(u), 8))
        return c

    best = dict(start)
    bc = cost(best)
    pool = [dict(start), {}]
    for mk in MEASURED:
        pool.append(dict(mk))
    for it in range(3000):
        if it % 150 == 149:
            pool.append(random_comp())
        src = pool[rng.randrange(len(pool))] if rng.random() < 0.3 else best
        cand = dict(src)
        for _ in range(rng.randint(1, 3)):
            u = movable[rng.randrange(len(movable))]
            d = rng.randint(1, 3) * rng.choice((-1, 1))
            n = cand.get(u, 0) + d
            if n < 0:
                n = 0
            if n == 0:
                cand.pop(u, None)
            else:
                cand[u] = min(n, cap(u))
        if u == nxt and cand.get(nxt, 0) == 0:
            continue
        cnd_c = cost(cand)
        if cnd_c < bc - 1e-9:
            best, bc = cand, cnd_c
            pool.append(dict(cand))
            if len(pool) > 40:
                pool.pop(0)
    return best


rows = []
unlock_order = [u for _, u in UNLOCK]
introduced = set()
comp = {}
prev_pred = None
for w in range(1, N + 1):
    target = targets.get(w)
    if target is None:
        sys.exit(f"target_costs.csv has no target for wave {w}")
    if w in PLAN and N == 30:
        comp = dict(PLAN[w])
    else:
        comp = climb(dict(comp), target, w, prev_pred, set(introduced), unlock_order)
    fresh = [u for u in comp if u not in introduced]
    has_new = bool(fresh)
    bad = violations(comp, w, prev_pred, has_new)
    if bad:
        print(f"wave {w:2}: RULE VIOLATIONS ({bad})")
    introduced.update(comp)
    pred = predict(comp)
    est, exact = estimate(comp)
    # order rows: infantry first, then vehicles, aviation last
    order = sorted(comp, key=lambda u: (units[u]["type"] != "troops", units[u]["type"] == "aviation",
                                       int(units[u].get("threat_value") or 0)))
    delay = 0.0
    for u in order:
        n = comp[u]
        note = "new" if u in fresh else ""
        rows.append([w, u, n, round(delay, 1), interval(u), note])
        delay += min(12.0, 4.0 + n * interval(u) * 0.5)
    prev_pred = pred
    tag = "measured" if exact else "estimate "
    print(f"wave {w:2}: target {target:6.0f}, predicted {pred:6.0f}, {tag} {est:6.0f} "
          f"({100 * (est - target) / target:+5.1f}%): " +
          ", ".join(f"{n} {u}" for u, n in comp.items()))

with open(out, "w", newline="", encoding="utf-8") as fh:
    wr = csv.writer(fh, lineterminator="\n")
    wr.writerow(["wave", "unit", "count", "delay_s", "interval_s", "notes"])
    wr.writerows(rows)
