#!/usr/bin/env python3
"""Charts for docs/balance/mistral/attacker-cost (2026-11-24).
Usage: make_charts.py <attacker_cost.json> <metrics.json> <out_dir>"""
import json, math, os, sys
from pngchart import Canvas

TYPES = ["recruit","regular","special_forces","motorbike","buggy","van",
         "mrap","ifv","old_tank","modern_tank","helicopter"]
FIELDS = ["Forest belts","Balka and railway","River crossing"]
COL = {"Forest belts": (0,110,0), "Balka and railway": (190,120,0), "River crossing": (0,90,180)}

def chart_curves(ac, out):
    curves = ac["curves"]
    W,H = 1140, 900
    cv = Canvas(W,H,(255,255,255))
    cv.text(20,10,"attacker cost against count, by field", scale=2)
    cols, rows = 3, 4
    pw, ph = W//cols, (H-40)//rows
    for idx,u in enumerate(TYPES):
        gx, gy = idx%cols, idx//cols
        x0, y0 = gx*pw+8, gy*ph+40
        x1, y1 = x0+pw-16, y0+ph-24
        maxn = 0; maxc = 0
        for f in FIELDS:
            for n,c in curves.get(f+"|"+u, []):
                if c: maxn = max(maxn,n); maxc = max(maxc,c)
        if maxc == 0: maxc = 1
        if maxn == 0: maxn = 1
        cv.rect(x0, y0, x1-x0, y1-y0, (225,225,225))
        cv.rect(x0, y0, x1-x0, 1, (0,0,0)); cv.rect(x0, y0, 1, y1-y0, (0,0,0))
        cv.rect(x0, y1, x1-x0, 1, (0,0,0))
        cv.text(x0+2, y0+4, u.replace("special_forces","special forces").replace("_"," "))
        # y label: max cost
        cv.text(x0+2, y1-12, str(int(maxc)))
        cv.text(x1-len("count")*4-6, y1-12, "count %d"%maxn)
        def px(n): return x0 + 6 + (x1-x0-16) * n/maxn
        def py(c): return y1 - 6 - (y1-y0-24) * c/maxc
        for f in FIELDS:
            pts = sorted((n,c) for n,c in curves.get(f+"|"+u,[]) if c)
            for (n,c) in pts:
                cx_, cy_ = int(px(n)), int(py(c))
                cv.rect(cx_-2, cy_-2, 5, 5, COL[f])
            for a,b in zip(pts, pts[1:]):
                cv.line(int(px(a[0])), int(py(a[1])), int(px(b[0])), int(py(b[1])), COL[f])
    lx = W - 260; ly = 12
    for i,f in enumerate(FIELDS):
        cv.rect(lx, ly+i*12, 10, 4, COL[f]); cv.text(lx+16, ly+i*12-2, f)
    cv.save(os.path.join(out, "cost-by-type.png"))

def chart_pred(metrics, out):
    preds = metrics["predictions"]
    W,H = 760, 700
    cv = Canvas(W,H,(255,255,255))
    cv.text(20,10,"predicted against measured cost, mixed waves", scale=2)
    x0,y0,x1,y1 = 70, 50, W-40, H-60
    lo, hi = 1.7, 4.6   # log10 of cost 50..40000
    def px(v): return x0 + (x1-x0)*(math.log10(v)-lo)/(hi-lo)
    def py(v): return y1 - (y1-y0)*(math.log10(v)-lo)/(hi-lo)
    cv.line(x0,y0,x0,y1,(0,0,0)); cv.line(x0,y1,x1,y1,(0,0,0))
    for e in (2,3,4):
        xx = int(x0+(x1-x0)*(e-lo)/(hi-lo))
        cv.line(xx,y0,xx,y1,(225,225,225)); cv.line(x0,int(py(10**e)),x1,int(py(10**e)),(225,225,225))
        lab = "100" if e==2 else "1000" if e==3 else "10000"
        cv.text(xx-len(lab)*2, y1+6, lab)
        cv.text(x0-len(lab)*4-6, int(py(10**e))-3, lab)
    cv.text(x0-58, y0-8, "predicted")
    cv.text(x1-8*len("measured"), y1+22, "measured")
    # y=x
    cv.line(int(px(50)), int(py(50)), int(px(40000)), int(py(40000)), (0,0,0))
    cv.line(int(px(50)), int(py(62.5)), int(px(32000)), int(py(40000)), (150,150,150))
    cv.line(int(px(62.5)), int(py(50)), int(px(40000)), int(py(32000)), (150,150,150))
    for p in preds:
        f,m,pr = p["field"], p["measured"], p["predicted"]
        X, Y = int(px(m)), int(py(pr))
        cv.rect(X-2, Y-2, 5, 5, COL[f])
    for i,f in enumerate(FIELDS):
        cv.rect(W-250, 30+i*10, 10, 4, COL[f]); cv.text(W-234, 26+i*10, f)
    cv.save(os.path.join(out, "pred-vs-measured.png"))

def main(ac_path, metrics_path, out):
    os.makedirs(out, exist_ok=True)
    ac = json.load(open(ac_path))
    metrics = json.load(open(metrics_path))
    chart_curves(ac, out)
    chart_pred(metrics, out)
    print("charts written to", out)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3])
