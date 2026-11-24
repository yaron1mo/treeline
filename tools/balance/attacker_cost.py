#!/usr/bin/env python3
"""Attacker cost model from probe waves (2026-11-24).

Input: the probe waves (make_probe_waves.py) and their measured cost (WaveCost.cs, all fields).
Output:
- per attacker type and field: the cost curve cost(n) and a line fit cost(n) = entry + n * each;
- the counter of each type (cheapest defender per attacker stopped, from tools/balance/stop.csv);
- two models for mixed waves and their errors:
    A "sum of curves":   sum over types of cost_type(n_type)
    B "shared entry":    for each counter, the largest entry of its types once; plus sum of n * each
- the map coefficient: median of cost on a field / cost on Forest belts, over the same waves.

Usage: attacker_cost.py <repo root> <probe waves csv> <results dir> <out dir>
"""
import csv
import glob
import json
import math
import os
import statistics
import sys

GATE = "Forest belts"
root, probe_path, results, out = sys.argv[1:5]
os.makedirs(out, exist_ok=True)


def rows(p):
    with open(p, newline="", encoding="utf-8") as fh:
        return list(csv.DictReader(fh))


def num(v):
    try:
        f = float(v)
        return None if math.isnan(f) else f
    except (TypeError, ValueError):
        return None


units = {r["id"]: r for r in rows(os.path.join(root, "unity/Assets/Data/units.csv"))}
waves = {}
notes = {}
for r in rows(os.path.join(root, probe_path)):
    w = int(r["wave"])
    waves.setdefault(w, []).append((r["unit"], int(r["count"])))
    notes[w] = r.get("notes", "")

merged = []
for p in sorted(glob.glob(os.path.join(results, "**", "wave-cost*.csv"), recursive=True)):
    merged += rows(p)
if merged:
    with open(os.path.join(out, "wave-cost.csv"), "w", newline="", encoding="utf-8") as fh:
        wr = csv.DictWriter(fh, fieldnames=list(merged[0].keys()), lineterminator="\n")
        wr.writeheader()
        wr.writerows(merged)
cost = {(r["field"], int(r["wave"])): num(r["cost"]) for r in merged}
fields = sorted({f for f, _ in cost}, key=lambda f: (f != GATE, f))

# Counter of each attacker type: the defender with the lowest price per attacker stopped.
stop = {}
for r in rows(os.path.join(root, "tools/balance/stop.csv")):
    stop.setdefault(r["attacker"], {})[r["defender"]] = float(r["stopped"])
counter = {}
for a, m in stop.items():
    best = min(((float(units[d]["build_cost"]) / s, d) for d, s in m.items() if s >= 0.5 and d in units), default=None)
    counter[a] = best[1] if best else "none"

# Single-type curves and line fits.
curves = {}   # (field, unit) -> [(n, cost)]
for w, groups in waves.items():
    if notes[w].startswith("single:"):
        u, n = groups[0]
        for f in fields:
            c = cost.get((f, w))
            curves.setdefault((f, u), []).append((n, c))


def fit(points):
    pts = [(n, c) for n, c in points if c is not None]
    if len(pts) < 2:
        return None
    xs, ys = [p[0] for p in pts], [p[1] for p in pts]
    mx, my = sum(xs) / len(xs), sum(ys) / len(ys)
    sxx = sum((x - mx) ** 2 for x in xs)
    m = sum((x - mx) * (y - my) for x, y in pts) / sxx if sxx else 0.0
    b = my - m * mx
    ss = sum((y - my) ** 2 for y in ys)
    r2 = 1 - sum((y - (b + m * x)) ** 2 for x, y in pts) / ss if ss else 1.0
    return {"entry": b, "each": m, "r2": r2, "points": len(pts)}


fits = {k: fit(v) for k, v in curves.items()}
types = []
for (f, u) in curves:
    if u not in types:
        types.append(u)


def interp(points, n):
    pts = sorted((x, c) for x, c in points if c is not None)
    if not pts:
        return None
    if n <= pts[0][0]:
        return pts[0][1] * n / pts[0][0] if pts[0][0] else pts[0][1]
    for (x0, c0), (x1, c1) in zip(pts, pts[1:]):
        if x0 <= n <= x1:
            return c0 + (c1 - c0) * (n - x0) / (x1 - x0)
    if len(pts) >= 2:
        (x0, c0), (x1, c1) = pts[-2], pts[-1]
        return c1 + (c1 - c0) / (x1 - x0) * (n - x1)
    return pts[-1][1]


def model_a(f, groups):
    total = 0.0
    for u, n in groups:
        v = interp(curves.get((f, u), []), n)
        if v is None:
            return None
        total += v
    return total


def model_b(f, groups):
    entries, each = {}, 0.0
    for u, n in groups:
        ft = fits.get((f, u))
        if ft is None:
            return None
        c = counter.get(u, "none")
        entries[c] = max(entries.get(c, 0.0), max(0.0, ft["entry"]))
        each += max(0.0, ft["each"]) * n
    return sum(entries.values()) + each


def errors(f, model):
    pairs = []
    for w, groups in waves.items():
        if notes[w] != "mixed":
            continue
        m = cost.get((f, w))
        p = model(f, groups)
        if m and p:
            pairs.append((w, m, p))
    if not pairs:
        return None
    mape = statistics.mean(abs(p - m) / m for _, m, p in pairs)
    k = sum(m * p for _, m, p in pairs) / sum(p * p for _, _, p in pairs)
    mape_k = statistics.mean(abs(k * p - m) / m for _, m, p in pairs)
    within = sum(1 for _, m, p in pairs if abs(k * p - m) / m <= 0.2) / len(pairs)
    return {"waves": len(pairs), "mape": mape, "scale": k, "mape_scaled": mape_k, "within_20pct": within,
            "pairs": [(w, m, p) for w, m, p in pairs]}


models = {f: {"A_sum_of_curves": errors(f, model_a), "B_shared_entry": errors(f, model_b)} for f in fields}

# Map coefficient against the gate field.
mapk = {}
for f in fields:
    if f == GATE:
        continue
    ratios = [cost[(f, w)] / cost[(GATE, w)] for w in waves
              if cost.get((f, w)) and cost.get((GATE, w)) and cost[(GATE, w)] >= 200]
    mapk[f] = {"median": statistics.median(ratios) if ratios else None, "waves": len(ratios),
               "not_held": sum(1 for w in waves if (f, w) in cost and cost[(f, w)] is None)}

# Report.
md = ["# Attacker cost model", "",
      f"Probe waves: {len(waves)} ({sum(1 for n in notes.values() if n.startswith('single'))} single-type, "
      f"{sum(1 for n in notes.values() if n == 'mixed')} mixed). Fields: {', '.join(fields)}.",
      "Cost = the smallest budget with which the scripted player holds the wave (line damage 3 or less, 8 of 10 seeds).", ""]
for f in fields:
    md += [f"## {f}: cost of each attacker type", "",
           "| Attacker | Counter | Entry (fixed) | Each (per unit) | R² | Cost at n = 1, 2, 3, 5, 8, 12, 20, 30 |",
           "|---|---|---|---|---|---|"]
    for u in types:
        ft = fits.get((f, u))
        pts = sorted(curves.get((f, u), []))
        line = ", ".join("x" if c is None else f"{c:.0f}" for _, c in pts)
        if ft:
            md.append(f"| {u} | {counter.get(u, '-')} | {ft['entry']:.0f} | {ft['each']:.0f} | {ft['r2']:.2f} | {line} |")
        else:
            md.append(f"| {u} | {counter.get(u, '-')} | - | - | - | {line} |")
    md.append("")
md += ["## Mixed waves: how well the models predict the cost", "",
       "| Field | Model | Waves | Mean error | Scale k | Mean error after k | Within 20% after k |", "|---|---|---|---|---|---|---|"]
for f in fields:
    for name, e in models[f].items():
        if e:
            md.append(f"| {f} | {name} | {e['waves']} | {100 * e['mape']:.0f}% | {e['scale']:.2f} | {100 * e['mape_scaled']:.0f}% | {100 * e['within_20pct']:.0f}% |")
md += ["", "## Map coefficient (cost on the field / cost on Forest belts, median)", "", "| Field | Coefficient | Waves compared | Not held |", "|---|---|---|---|"]
for f, v in mapk.items():
    md.append(f"| {f} | {v['median']:.2f} | {v['waves']} | {v['not_held']} |" if v["median"] else f"| {f} | - | 0 | {v['not_held']} |")
md.append("")
open(os.path.join(out, "attacker_cost.md"), "w", encoding="utf-8").write("\n".join(md) + "\n")
json.dump({"fits": {f"{f}|{u}": v for (f, u), v in fits.items() if v},
           "curves": {f"{f}|{u}": v for (f, u), v in curves.items()},
           "counter": counter,
           "models": {f: {k: (dict(v, pairs=v["pairs"]) if v else None) for k, v in m.items()} for f, m in models.items()},
           "map_coefficient": mapk,
           "mixed": {w: waves[w] for w in waves if notes[w] == "mixed"}},
          open(os.path.join(out, "attacker_cost.json"), "w", encoding="utf-8"), indent=1)
print("\n".join(md))
