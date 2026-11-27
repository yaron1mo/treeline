#!/usr/bin/env python3
"""Generate waves whose predicted defence cost follows a target cost per wave.

Task: mistral/wave-generator (docs/agents/mistral.md, section 10). The cost model
comes from docs/balance/mistral/wave-generator/model.json (field Forest belts), a
refit of the attacker-cost model of PR #7 with the waves measured in the Balance
runs of this task added to the training set (fit: fit_model.py next to the model).
The single-type curves of that model (run 45411511647, artifact balance-report)
are embedded below as constants, so this script needs no artifact to run.

Method: pure generator, no plan. For every wave count N and every seed the wave
list comes from a randomised local search over wave compositions that drives the
model prediction to the target cost of the wave. The search keeps the task rules
(caps, type order, mix rules, growth limit). The model prediction has a floor for
small waves: a wave cheaper than twice the price of one rifle squad (100, measured)
is counted as at least one rifle squad. The same seed always gives the same waves.

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
model_all = json.load(open(os.path.join(root, "docs", "balance", "mistral", "wave-generator", "model.json"),
                           encoding="utf-8"))
model = model_all["fields"]["Forest belts"]
FLOOR = float(model_all.get("small_wave_floor", 0) or 0)   # one rifle squad, measured

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
    p = max(p * p, 1.0)
    # Small-wave floor: a wave of a few infantry units costs at least the price of
    # one rifle squad (FLOOR), and the floor never applies above 2x FLOOR.
    if FLOOR and p < 2 * FLOOR:
        p = max(p, FLOOR)
    return p


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


def climb(start, target, w, prev_pred, introduced, unlock_order):
    """Randomised local search. The model prediction moves in jumps, not in
    smooth steps, so a beam of one-unit moves gets stuck. Instead: perturb the
    best state found so far (1-3 types, plus or minus 1-3 units) and keep every
    state that breaks no rule. New types appear in unlock order only."""
    unlocked = [u for k, u in UNLOCK if k <= w]
    nxt = next((u for u in unlock_order if u not in introduced), None)
    movable = [u for u in unlocked if u in introduced] + ([nxt] if nxt in unlocked else [])

    def cost(comp):
        pred = predict(comp)
        has_new = any(u not in introduced for u in comp)
        return abs(pred - target) + 1e7 * violations(comp, w, prev_pred, has_new)

    def random_comp():
        c = {}
        for u in movable:
            if rng.random() < 0.4:
                c[u] = rng.randint(1, min(cap(u), 8))
        return c

    best = dict(start)
    bc = cost(best)
    pool = [dict(start), {}]
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
    comp = climb(dict(comp), target, w, prev_pred, set(introduced), unlock_order)
    fresh = [u for u in comp if u not in introduced]
    has_new = bool(fresh)
    bad = violations(comp, w, prev_pred, has_new)
    if bad:
        print(f"wave {w:2}: RULE VIOLATIONS ({bad})")
    introduced.update(comp)
    pred = predict(comp)
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
    print(f"wave {w:2}: target {target:6.0f}, predicted {pred:6.0f} "
          f"({100 * (pred - target) / target:+5.1f}%): " +
          ", ".join(f"{n} {u}" for u, n in comp.items()))

with open(out, "w", newline="", encoding="utf-8") as fh:
    wr = csv.writer(fh, lineterminator="\n")
    wr.writerow(["wave", "unit", "count", "delay_s", "interval_s", "notes"])
    wr.writerows(rows)
