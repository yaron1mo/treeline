#!/usr/bin/env python3
"""Charts for the mistral/squads report (2026-11-27), pure python (pngchart.py).

Reads the squads.json written by analyze.py and writes, per field:
- held-vs-premium-<field>.png: held share against cost premium, one point per squad
  (red = dominant, black = the "all" squad, blue = other). Threshold lines at held 0.9
  and premium 1.2. Points with no premium are skipped; premiums above the x max are clamped.
- heat-<field>.png: defender type against attacker category, cell = share of the waves with
  that attacker category where the type is in the best 10 squads of the wave.

Usage: charts.py <squads.json> <out dir>
"""
import json
import sys

from pngchart import Canvas

ABBR = ["rs", "mg", "sn", "mo", "at", "ad", "fpv", "bo", "rd"]
NAME = {"rs": "rifle_squad", "mg": "machine_gun", "sn": "sniper", "mo": "mortar",
        "at": "anti_tank", "ad": "air_defence", "fpv": "fpv_operator",
        "bo": "bomber_operator", "rd": "recon_drone"}

XMIN, XMAX = 0.5, 2.0


def slug(field):
    return field.lower().replace(" ", "-")


def scatter(js, field, path):
    stats = js["squads"][field]
    dom = set(js["dominant"][field])
    W, H = 680, 500
    L, R, T, B = 70, 20, 30, 50
    c = Canvas(W, H)
    pw, ph = W - L - R, H - T - B

    def x(v):
        return L + int((min(max(v, XMIN), XMAX) - XMIN) / (XMAX - XMIN) * pw)

    def y(v):
        return T + int((1.0 - v) * ph)

    c.text(L, 8, "held share vs cost premium - %s" % field, (0, 0, 0), 1)
    for i in range(6):  # y grid 0..1
        gy = y(i / 5.0)
        c.line(L, gy, W - R, gy, (220, 220, 220))
        c.text(8, gy - 2, "%.1f" % (i / 5.0))
    for i in range(7):  # x grid 0.5..2.0
        gx = x(XMIN + i * 0.25)
        c.line(gx, T, gx, H - B, (220, 220, 220))
        c.text(gx - 8, H - B + 6, "%.2f" % (XMIN + i * 0.25))
    c.text(L, H - 18, "cost premium (squad cost / all cost) ->")
    c.line(L, T, L, H - B, (0, 0, 0))
    c.line(L, H - B, W - R, H - B, (0, 0, 0))
    c.line(x(1.2), T, x(1.2), H - B, (180, 60, 60))   # premium 1.2
    c.line(L, y(0.9), W - R, y(0.9), (180, 60, 60))     # held 0.9
    c.text(x(1.2) + 3, T + 2, "1.2", (180, 60, 60))
    c.text(L + 3, y(0.9) - 8, "0.9", (180, 60, 60))
    n = 0
    for s, st in stats.items():
        if st["premium"] is None:
            continue
        n += 1
        col = (0, 0, 0) if s == "all" else ((200, 30, 30) if s in dom else (40, 80, 200))
        c.rect(x(st["premium"]) - 2, y(st["held_share"]) - 2, 5, 5, col)
    c.text(L, H - 8, "%d squads, premiums clamped to %.1f-%.1f" % (n, XMIN, XMAX), (90, 90, 90))
    c.save(path)


def heat(js, field, path):
    cats = js["categories"]
    hm = js["heat"][field]
    cw, ch, pad = 92, 30, 4
    L, T = 120, 40
    W = L + len(cats) * (cw + pad) + 20
    H = T + (len(ABBR) + 1) * (ch + pad) + 30
    c = Canvas(W, H)
    c.text(8, 8, "type in best 10, by attacker category - %s" % field, (0, 0, 0), 1)
    c.text(8, 20, "cell = share of waves with that attacker, type in best 10", (90, 90, 90))
    for j, cat in enumerate(cats):
        c.text(L + j * (cw + pad) + 4, T - 14, cat, (0, 0, 0))
    for i, ab in enumerate(ABBR):
        ry = T + i * (ch + pad)
        c.text(8, ry + ch // 2 - 3, ab, (0, 0, 0))
        c.text(30, ry + ch // 2 - 3, NAME[ab], (120, 120, 120))
        for j, cat in enumerate(cats):
            v = hm[NAME[ab]][cat]
            v = 0.0 if v is None else v
            shade = int(255 - v * 215)
            c.rect(L + j * (cw + pad), ry, cw, ch, (shade, shade, 255), fill=True)
            c.rect(L + j * (cw + pad), ry, cw, ch, (200, 200, 200), fill=False)
            c.text(L + j * (cw + pad) + cw // 2 - 8, ry + ch // 2 - 3,
                   "n/a" if hm[NAME[ab]][cat] is None else "%.2f" % v, (0, 0, 0))
    c.save(path)


def main():
    js = json.load(open(sys.argv[1], encoding="utf-8"))
    outdir = sys.argv[2]
    for f in js["fields"]:
        scatter(js, f, "%s/held-vs-premium-%s.png" % (outdir, slug(f)))
        heat(js, f, "%s/heat-%s.png" % (outdir, slug(f)))
        print("charts for", f)


if __name__ == "__main__":
    main()
