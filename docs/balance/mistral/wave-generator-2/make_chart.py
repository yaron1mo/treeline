#!/usr/bin/env python3
"""Draw the wave-generator-2 chart: target cost vs measured cost per wave.

Best run: Balance run 45926887097 (run 3, 25 of 30 waves within 20%, verdict pass).
Every 5th wave is a relief wave (target x 0.8). Input: report.json from the run
artifact. Output: target-vs-measured.png.
"""
import json
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "attacker-cost"))
from pngchart import Canvas

report = json.load(open(sys.argv[1]))
out = sys.argv[2]
rows = sorted(report["target_cost"]["rows"], key=lambda r: r["wave"])

W, H = 760, 400
L, R, T, B = 48, 16, 28, 36
cv = Canvas(W, H)

ymax = max(max(r["target"], r["cost"]) for r in rows) * 1.08
def X(w):
    return int(L + (W - L - R) * (w - 1) / (len(rows) - 1))
def Y(v):
    return int(H - B - (H - B - T) * v / ymax)

# grid, y labels at each 2000
step = 2000
v = 0
while v <= ymax:
    y = Y(v)
    cv.line(L, y, W - R, y, (210, 210, 210), 1)
    cv.text(4, y - 3, str(v), (110, 110, 110))
    v += step
# x axis and labels each 5 waves
cv.line(L, H - B, W - R, H - B, (0, 0, 0), 1)
for r in rows:
    if r["wave"] % 5 == 0 or r["wave"] == 1:
        cv.text(X(r["wave"]) - 4, H - B + 8, str(r["wave"]), (110, 110, 110))

GREEN = (0, 110, 0)
RED = (190, 30, 30)
GREY = (160, 160, 160)

def draw(key, colour, radius=3, above=False):
    prev = None
    for r in rows:
        x, y = X(r["wave"]), Y(r[key])
        if prev:
            cv.line(prev[0], prev[1], x, y, colour, 1)
        cv.rect(x - radius, y - radius, 2 * radius, 2 * radius, colour)
        prev = (x, y)

draw("target", GREY)
for r in rows:
    err = (r["cost"] - r["target"]) / r["target"]
    x, y = X(r["wave"]), Y(r["cost"])
    cv.rect(x - 4, y - 4, 8, 8, RED if abs(err) > 0.2 else GREEN)
    prev = None
for i, r in enumerate(rows):
    err = (r["cost"] - r["target"]) / r["target"]
    colour = RED if abs(err) > 0.2 else GREEN
    x, y = X(r["wave"]), Y(r["cost"])
    if i:
        cv.line(X(rows[i - 1]["wave"]), Y(rows[i - 1]["cost"]), x, y, colour, 1)

# legend
cv.line(L + 8, 12, L + 26, 12, GREY, 2)
cv.rect(L + 16, 8, 5, 5, GREY)
cv.text(L + 32, 9, "target cost", (90, 90, 90))
cv.line(L + 130, 12, L + 148, 12, GREEN, 2)
cv.rect(L + 138, 8, 5, 5, GREEN)
cv.text(L + 154, 9, "measured, within 20%", (0, 90, 0))
cv.line(L + 320, 12, L + 338, 12, RED, 2)
cv.rect(L + 328, 8, 5, 5, RED)
cv.text(L + 344, 9, "measured, outside 20%", (150, 20, 20))

cv.save(out)
print("saved", out)
