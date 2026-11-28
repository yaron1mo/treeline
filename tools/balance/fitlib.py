#!/usr/bin/env python3
"""Shared helpers of the wave cost model fit (tools/balance/fit_model.py).

Moved from docs/balance/mistral/attacker-cost/fitlib.py (task mistral/attacker-cost, PR #7)
to tools/balance for the automatic refit (task mistral/model-refit, issue #40). The old
copy stays in docs/balance/mistral/attacker-cost/.

The attacker types come from units.csv (side = attack), so a new attacker type in
units.csv needs no change in the scripts. EXCLUDED is the same list as at the top of
make_probe_waves.py and make_waves.py.
"""
import csv
import math
import os

EXCLUDED = ["fpv", "one_way_drone", "recon_uav"]   # never measured, never generated


def read_units(root):
    with open(os.path.join(root, "unity", "Assets", "Data", "units.csv"), newline="", encoding="utf-8") as fh:
        return {r["id"]: r for r in csv.DictReader(fh)}


def attackers(units):
    """Attacker types of units.csv in file order, without EXCLUDED."""
    return [u for u, r in units.items() if r["side"] == "attack" and u not in EXCLUDED]


def class_of(types):
    """Defence class of each attacker type. The same rule as CLASS_OF in make_waves.py."""
    return {u: (2 if u == "helicopter" else 1 if u in ("mrap", "ifv", "old_tank", "modern_tank") else 0)
            for u in types}


def counter(root, units):
    """Cheapest defender per attacker stopped (tools/balance/stop.csv), as in attacker_cost.py."""
    stop = {}
    with open(os.path.join(root, "tools", "balance", "stop.csv"), newline="", encoding="utf-8") as fh:
        for r in csv.DictReader(fh):
            stop.setdefault(r["attacker"], {})[r["defender"]] = float(r["stopped"])
    out = {}
    for a, m in stop.items():
        best = min(((float(units[d]["build_cost"]) / s, d) for d, s in m.items()
                    if s >= 0.5 and d in units), default=None)
        out[a] = best[1] if best else "none"
    return out


def tag_of(note):
    """Tag of a probe note: 'single:recruit:4' -> 'single', 'mixed-train2' -> 'mixed-train2'."""
    return note.split(":")[0]


def load(probe_path, wc_path):
    """Read the probe waves and the merged wave-cost.csv. Returns (waves, tags, cost)."""
    waves, tags = {}, {}
    with open(probe_path, newline="", encoding="utf-8") as fh:
        for r in csv.DictReader(fh):
            w = int(r["wave"])
            waves.setdefault(w, {}).setdefault(r["unit"], 0)
            waves[w][r["unit"]] += int(r["count"])
            tags[w] = tag_of(r["notes"])
    cost = {}
    with open(wc_path, newline="", encoding="utf-8") as fh:
        for r in csv.DictReader(fh):
            try:
                v = float(r["cost"])
            except (TypeError, ValueError):
                v = None
            if v is not None and math.isnan(v):
                v = None
            cost[(r["field"], int(r["wave"]))] = v
    return waves, tags, cost


def curves(waves, tags, cost, fields):
    """Single-type cost curves: {(field, unit): [(count, cost)]} from the 'single' probe waves."""
    cv = {}
    for w, comp in waves.items():
        if tags[w] == "single":
            u = list(comp)[0]
            n = sum(comp.values())
            for f in fields:
                cv.setdefault((f, u), []).append((n, cost.get((f, w))))
    return cv


def interp(points, n):
    """Piecewise-linear interpolation of a cost curve, as in attacker_cost.py."""
    pts = sorted((x, c) for x, c in points if c is not None)
    if not pts:
        return None
    if n <= pts[0][0]:
        return pts[0][1] * n / pts[0][0] if pts[0][0] else pts[0][1]
    for (x0, c0), (x1, c1) in zip(pts, pts[1:]):
        if x0 <= n <= x1:
            return c0 + (c1 - c0) * (n - x0) / (x1 - x0)
    if len(pts) == 1:
        return pts[0][1]
    (x0, c0), (x1, c1) = pts[-2], pts[-1]
    return c1 + (c1 - c0) / (x1 - x0) * (n - x1)
