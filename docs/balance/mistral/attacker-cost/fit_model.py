#!/usr/bin/env python3
"""Fit the cost model for mixed probe waves (mistral/attacker-cost, 2026-11-24).

Input: tools/balance/probe_waves.csv and the measured wave costs (wave-cost.csv
from the Balance run artifact).

Model (per field):
  pred = gbm(ridge formula(beta, features(comp)), features(comp))
The ridge formula uses counts, presence, pair terms and the cost curves of the
single-type waves (piecewise-linear interpolation). The gradient boosting
corrects the residuals in sqrt space.

Train tags: single, pair, triple, mixed-train, mixed-train2 (wave notes).
Test tags: mixed, mixed-test2. The test waves are never used for the fit.

Usage: fit_model.py <probe_waves.csv> <wave-cost.csv> <attacker_cost.json> <out_dir>
Writes: model.json, metrics.json
"""
import csv, json, math, os, random, sys

TYPES = ["recruit","regular","special_forces","motorbike","buggy","van",
         "mrap","ifv","old_tank","modern_tank","helicopter"]
CLASS_OF = dict(zip(TYPES, [0,0,0,0,0,0,1,1,1,1,2]))   # 0 MG, 1 AT, 2 air defence
TRAIN = {"single","pair","triple","mixed-train","mixed-train2"}
TEST = {"mixed","mixed-test2"}
FIELDS = ["Forest belts","Balka and railway","River crossing"]
NB = 16


def tag_of(note):
    return note if not note.startswith(("single","pair","triple")) else note.split(":")[0]


def load(probe_path, wc_path):
    waves, tags = {}, {}
    for r in csv.DictReader(open(probe_path, newline="", encoding="utf-8")):
        w = int(r["wave"])
        waves.setdefault(w, {}).setdefault(r["unit"], 0)
        waves[w][r["unit"]] += int(r["count"])
        tags[w] = tag_of(r["notes"])
    cost = {}
    for r in csv.DictReader(open(wc_path, newline="", encoding="utf-8")):
        try:
            v = float(r["cost"])
        except (TypeError, ValueError):
            v = None
        if v is not None and math.isnan(v):
            v = None
        cost[(r["field"], int(r["wave"]))] = v
    return waves, tags, cost


def interp(points, n):
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


class Node:
    __slots__ = ("f","t","l","r","v")
    def ser(self):
        if self.l is None:
            return [self.v]
        return [self.f, self.t, self.l.ser(), self.r.ser()]


def tree_predict(t, x):
    while len(t) == 4:
        t = t[2] if x[t[0]] <= t[1] else t[3]
    return t[0]


def build_tree(X, idx, y, depth, maxdepth, minleaf, rnd, fsubset=12):
    m = len(idx); sv = sum(y[i] for i in idx) / m
    node = Node(); node.v = sv; node.l = node.r = None
    if depth >= maxdepth or m < 2 * minleaf:
        return node
    nf = len(X[0])
    best = (-1e18, None, None); tot = sv * m
    for fi in rnd.sample(range(nf), min(nf, fsubset)):
        s = [0.0] * NB; c = [0] * NB
        for i in idx:
            b = X[i][fi]; s[b] += y[i]; c[b] += 1
        cs = 0.0; cc = 0
        for b in range(NB - 1):
            cs += s[b]; cc += c[b]
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
    node.f = fi; node.t = b + 0.5
    node.l = build_tree(X, li, y, depth + 1, maxdepth, minleaf, rnd, fsubset)
    node.r = build_tree(X, ri, y, depth + 1, maxdepth, minleaf, rnd, fsubset)
    return node


def main(probe_path, wc_path, ac_json, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    waves, tags, cost = load(probe_path, wc_path)
    ac = json.load(open(ac_json, encoding="utf-8"))
    curves = {(f, u): pts for k, pts in ac["curves"].items()
              for f, u in [k.split("|", 1)]}
    counter = ac["counter"]

    def features(f, comp):
        cnt = [float(comp.get(u, 0)) for u in TYPES]
        pres = [1.0 if c > 0 else 0.0 for c in cnt]
        contrib = []
        for u in TYPES:
            v = interp(curves.get((f, u), []), comp.get(u, 0))
            contrib.append(v if v else 0.0)
        x = cnt + pres + contrib
        for cl in range(3):
            x.append(math.sqrt(sum(v * v for v, u in zip(contrib, TYPES) if CLASS_OF[u] == cl)))
        x.append(math.log1p(sum(comp.values())))
        # pair presence
        for i in range(11):
            for j in range(i + 1, 11):
                x.append(pres[i] * pres[j])
        # pair min-count
        for i in range(11):
            for j in range(i + 1, 11):
                x.append(float(min(cnt[i], cnt[j])))
        return x

    model = {"fields": {}, "counter": counter, "types": TYPES,
             "train_tags": sorted(TRAIN), "test_tags": sorted(TEST), "n_features": None}
    all_pairs = []
    summary = {}
    for f in FIELDS:
        Xt, yt, Xe, ye, wt, we = [], [], [], [], [], []
        for w, comp in waves.items():
            v = cost.get((f, w))
            if not v:
                continue
            x = features(f, comp)
            if tags[w] in TRAIN:
                Xt.append(x); yt.append(v); wt.append(w)
            elif tags[w] in TEST:
                Xe.append(x); ye.append(v); we.append(w)
        NF = len(Xt[0]); model["n_features"] = NF
        bins = []
        for j in range(NF):
            vals = sorted(r[j] for r in Xt)
            bins.append([vals[int(len(vals) * k / NB)] for k in range(1, NB)]
                        if len(set(vals)) > 1 else None)

        def binned(x):
            o = []
            for j in range(NF):
                bj = bins[j]
                if bj is None:
                    o.append(0); continue
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
        rnd = random.Random(7)
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
        Xeb = [binned(x) for x in Xe]
        for i, (xb, v) in enumerate(zip(Xeb, ye)):
            p = F0
            for t in trees:
                p += 0.2 * tree_predict(t, xb)
            p = max(p * p, 1.0)
            all_pairs.append((f, we[i], v, p))
        # metrics
        pf = [pp for ff, _, v, pp in all_pairs if ff == f]
        vf = [v for ff, _, v, _ in all_pairs if ff == f]
        mape = sum(abs(p - v) / v for p, v in zip(pf, vf)) / len(vf)
        w20 = sum(1 for p, v in zip(pf, vf) if abs(p - v) / v <= 0.2) / len(vf)
        summary[f] = {"n": len(vf), "mape": mape, "within_20pct": w20}
    pooled_mape = sum(abs(p - v) / v for _, _, v, p in all_pairs) / len(all_pairs)
    pooled_w20 = sum(1 for _, _, v, p in all_pairs if abs(p - v) / v <= 0.2) / len(all_pairs)
    summary["pooled"] = {"n": len(all_pairs), "mape": pooled_mape, "within_20pct": pooled_w20}
    json.dump(model, open(os.path.join(out_dir, "model.json"), "w", encoding="utf-8"))
    json.dump({"summary": summary,
               "predictions": [{"field": f, "wave": w, "measured": v, "predicted": round(p, 1)}
                               for f, w, v, p in all_pairs]},
              open(os.path.join(out_dir, "metrics.json"), "w", encoding="utf-8"), indent=1)
    print(json.dumps(summary, indent=1))
    return summary


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4])
