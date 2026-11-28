#!/usr/bin/env python3
"""Evaluate a wave-generator model on measured waves (mistral/wave-generator, review 1 of PR #9).

For each Balance run of this task the report needs the model error on the waves that the
run measured. Usage:

  eval_model.py <model.json> <waves.csv> <wave-cost.csv> [field]

Prints per-wave predictions and the mean error / share within 20% on one field
(default Forest belts, the gate field). Waves with no measured cost (not held) are skipped.
"""

import csv, json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
model_path = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "model.json")
waves_path = sys.argv[2] if len(sys.argv) > 2 else os.path.join(HERE, "training_waves.csv")
wc_path = sys.argv[3] if len(sys.argv) > 3 else os.path.join(HERE, "training_wave_cost.csv")
FIELD = sys.argv[4] if len(sys.argv) > 4 else "Forest belts"

MODEL = json.load(open(model_path, encoding="utf-8"))
FLOOR = float(MODEL.get("small_wave_floor", 0) or 0)
model = MODEL["fields"][FIELD]
TYPES = MODEL["types"]
CLASS_OF = {u: (2 if u == "helicopter" else 1 if u in ("mrap", "ifv", "old_tank", "modern_tank") else 0)
            for u in TYPES}

# Forest belts single-type curves, run 45411511647 (same source as attacker_cost.json).
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
    if FLOOR and p < 2 * FLOOR:
        p = max(p, FLOOR)
    return p


waves = {}
for r in csv.DictReader(open(waves_path, newline="", encoding="utf-8")):
    w = int(r["wave"])
    waves.setdefault(w, {}).setdefault(r["unit"], 0)
    waves[w][r["unit"]] += int(r["count"])

errs, rows = [], []
for r in csv.DictReader(open(wc_path, newline="", encoding="utf-8")):
    if r["field"] != FIELD:
        continue
    try:
        v = float(r["cost"])
    except (TypeError, ValueError):
        continue
    if math.isnan(v):
        continue
    w = int(r["wave"])
    if w not in waves:
        continue
    p = predict(waves[w])
    e = p / v - 1
    errs.append(abs(e))
    rows.append((w, v, p, e))

print(f"field: {FIELD}, model: {os.path.basename(model_path)}, waves: {len(rows)}")
for w, v, p, e in sorted(rows):
    print(f"  wave {w:5}: measured {v:7.0f}, predicted {p:7.0f}, error {100*e:+6.1f}%")
if errs:
    print(f"mean error {100*sum(errs)/len(errs):.1f}%, within 20%: "
          f"{sum(1 for e in errs if e <= 0.2)} of {len(errs)} "
          f"({100*sum(1 for e in errs if e <= 0.2)/len(errs):.0f}%)")
