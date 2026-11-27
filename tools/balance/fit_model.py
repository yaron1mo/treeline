#!/usr/bin/env python3
"""Refit the wave cost model from the probe waves (mistral/model-refit, issue #40).

Moved from docs/balance/mistral/attacker-cost/fit_model.py (PR #7); the old copy stays
there. The Model refit workflow (.github/workflows/model-refit.yml) runs this script
after it measures the probe waves (make_probe_waves.py) on all fields.

Input: the probe waves and the merged wave-cost.csv of the run. The attacker types,
the single-type cost curves and the counter table are read from units.csv and
tools/balance/stop.csv, so no other artifact is needed and a new attacker type in
units.csv is fitted with no other change.

Output (out_dir, default tools/balance): model.json (read by make_waves.py) and
model_metrics.json (mean error and share within 20% on the held-out mixed waves, per
field, and the pass flag).

Model (per field), unchanged from PR #7:
  pred = gbm(features(comp)) in sqrt space, squared, with the small-wave floor.

Held-out rule (the same as in the attacker-cost task): waves with the tag "mixed" or
"mixed-test2" are never used for the fit; every other tag trains. The metrics are
computed on those held-out mixed waves only.

A fixed seed makes the fit deterministic: the same input gives the same model.

Usage: fit_model.py <repo root> <probe waves csv> <wave-cost csv> [out dir]
"""
import json
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from fitlib import attackers, class_of, counter, curves, interp, load, read_units

TEST = {"mixed", "mixed-test2"}   # held-out tags, unchanged since the attacker-cost task
NB = 16                            # bins per feature
FLOOR = 100.0                      # small-wave floor: one rifle squad, measured; never above 2x this
SEED = 7                           # fixed seed: the same input gives the same model
PASS_WITHIN = 0.80                 # pass rule: share within 20% on every field


class Node:
    __slots__ = ("f", "t", "l", "r", "v")

    def ser(self):
        if self.l is None:
            return [self.v]
        return [self.f, self.t, self.l.ser(), self.r.ser()]


def tree_predict(t, x):
    while len(t) == 4:
        t = t[2] if x[t[0]] <= t[1] else t[3]
    return t[0]


def build_tree(X, idx, y, depth, maxdepth, minleaf, rnd, fsubset=12):
    m = len(idx)
    sv = sum(y[i] for i in idx) / m
    node = Node()
    node.v = sv
    node.l = node.r = None
    if depth >= maxdepth or m < 2 * minleaf:
        return node
    nf = len(X[0])
    best = (-1e18, None, None)
    tot = sv * m
    for fi in rnd.sample(range(nf), min(nf, fsubset)):
        s = [0.0] * NB
        c = [0] * NB
        for i in idx:
            b = X[i][fi]
            s[b] += y[i]
            c[b] += 1
        cs = 0.0
        cc = 0
        for b in range(NB - 1):
            cs += s[b]
            cc += c[b]
            if cc < minleaf:
                continue
            cr = m - cc
            if cr < minleaf:
                break
            gain = cs * cs / cc + (tot - cs) * (tot - cs) / cr
            if gain > best[0]:
                best = (gain, fi, b)
    if best[1] is None:
        return node
    _, fi, b = best
    li = [i for i in idx if X[i][fi] <= b]
    ri = [i for i in idx if X[i][fi] > b]
    if not li or not ri:
        return node
    node.f = fi
    node.t = b + 0.5
    node.l = build_tree(X, li, y, depth + 1, maxdepth, minleaf, rnd, fsubset)
    node.r = build_tree(X, ri, y, depth + 1, maxdepth, minleaf, rnd, fsubset)
    return node


def apply_floor(p):
    """Small-wave floor: at least one rifle squad (FLOOR), never above 2x FLOOR."""
    p = max(p, 1.0)
    if p < 2 * FLOOR:
        p = max(p, FLOOR)
    return p


def main(root, probe_path, wc_path, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    units = read_units(root)
    types = attackers(units)
    cls = class_of(types)
    cnt = counter(root, units)
    waves, tags, cost = load(probe_path, wc_path)
    fields = sorted({f for f, _ in cost})
    cv = curves(waves, tags, cost, fields)
    n = len(types)

    def features(f, comp):
        c = [float(comp.get(u, 0)) for u in types]
        pres = [1.0 if v > 0 else 0.0 for v in c]
        contrib = []
        for u in types:
            v = interp(cv.get((f, u), []), comp.get(u, 0))
            contrib.append(v if v else 0.0)
        x = c + pres + contrib
        for cl in range(3):
            x.append(math.sqrt(sum(v * v for v, u in zip(contrib, types) if cls[u] == cl)))
        x.append(math.log1p(sum(comp.values())))
        for i in range(n):
            for j in range(i + 1, n):
                x.append(pres[i] * pres[j])
        for i in range(n):
            for j in range(i + 1, n):
                x.append(float(min(c[i], c[j])))
        return x

    model = {"fields": {}, "counter": cnt, "types": types,
             "train_tags": sorted({t for t in tags.values() if t not in TEST}),
             "test_tags": sorted(TEST), "n_features": None,
             "small_wave_floor": FLOOR, "seed": SEED,
             "curves": {f"{f}|{u}": v for (f, u), v in sorted(cv.items())},
             "note": "automatic refit (model-refit workflow): fit_model.py on the probe waves measured by the run"}
    per_field = {}
    predictions = []
    for f in fields:
        Xt, yt, Xe, ye, we = [], [], [], [], []
        for w, comp in waves.items():
            v = cost.get((f, w))
            if not v:
                continue
            x = features(f, comp)
            if tags[w] in TEST:
                Xe.append(x)
                ye.append(v)
                we.append(w)
            else:
                Xt.append(x)
                yt.append(v)
        if not Xt:
            per_field[f] = {"n": 0, "mean_error": None, "within_20pct": 0.0}
            continue
        nf = len(Xt[0])
        model["n_features"] = nf
        bins = []
        for j in range(nf):
            vals = sorted(r[j] for r in Xt)
            bins.append([vals[int(len(vals) * k / NB)] for k in range(1, NB)]
                        if len(set(vals)) > 1 else None)

        def binned(x):
            o = []
            for j in range(nf):
                bj = bins[j]
                if bj is None:
                    o.append(0)
                    continue
                b = 0
                for t in bj:
                    if x[j] > t:
                        b += 1
                    else:
                        break
                o.append(b)
            return o

        Xtb = [binned(x) for x in Xt]
        ysq = [math.sqrt(v) for v in yt]
        F0 = sum(ysq) / len(ysq)
        pred = [F0] * len(ysq)
        rnd = random.Random(SEED)
        trees = []
        for _ in range(60):
            res = [ysq[i] - pred[i] for i in range(len(ysq))]
            t = build_tree(Xtb, list(range(len(ysq))), res, 0, 5, 8, rnd, 12)
            for i in range(len(ysq)):
                node = t
                while node.l is not None:
                    node = node.l if Xtb[i][node.f] <= node.t else node.r
                pred[i] += 0.2 * node.v
            trees.append(t.ser())
        model["fields"][f] = {"bins": bins, "F0": F0, "trees": trees}
        pf, vf = [], []
        for x, v, w in zip(Xe, ye, we):
            xb = binned(x)
            p = F0
            for t in trees:
                p += 0.2 * tree_predict(t, xb)
            p = apply_floor(p * p)
            pf.append(p)
            vf.append(v)
            predictions.append({"field": f, "wave": w, "measured": v, "predicted": round(p, 1)})
        if vf:
            mape = sum(abs(p - v) / v for p, v in zip(pf, vf)) / len(vf)
            w20 = sum(1 for p, v in zip(pf, vf) if abs(p - v) / v <= 0.2) / len(vf)
        else:
            mape, w20 = None, 0.0
        per_field[f] = {"n": len(vf), "mean_error": mape, "within_20pct": w20}
    pooled_p = [p["predicted"] for p in predictions]
    pooled_v = [p["measured"] for p in predictions]
    pooled = {"n": len(pooled_v),
              "mean_error": (sum(abs(p - v) / v for p, v in zip(pooled_p, pooled_v)) / len(pooled_v)) if pooled_v else None,
              "within_20pct": (sum(1 for p, v in zip(pooled_p, pooled_v) if abs(p - v) / v <= 0.2) / len(pooled_v)) if pooled_v else 0.0}
    passed = bool(per_field) and all(v["within_20pct"] >= PASS_WITHIN and v["n"] > 0 for v in per_field.values())
    metrics = {"pass_rule": f"within_20pct >= {PASS_WITHIN} on every field, held-out mixed waves (tags {sorted(TEST)})",
               "pass": passed, "per_field": per_field, "pooled": pooled,
               "train_tags": model["train_tags"], "test_tags": sorted(TEST), "seed": SEED,
               "predictions": predictions}
    json.dump(model, open(os.path.join(out_dir, "model.json"), "w", encoding="utf-8"))
    json.dump(metrics, open(os.path.join(out_dir, "model_metrics.json"), "w", encoding="utf-8"), indent=1)
    print(json.dumps({"pass": passed, "per_field": per_field, "pooled": pooled}, indent=1))
    return metrics


if __name__ == "__main__":
    root = sys.argv[1]
    probe_path = sys.argv[2]
    wc_path = sys.argv[3]
    out_dir = sys.argv[4] if len(sys.argv) > 4 else os.path.join(root, "tools", "balance")
    main(root, probe_path, wc_path, out_dir)
