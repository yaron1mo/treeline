#!/usr/bin/env python3
"""Summary of tools/balance/ArmourHidden.cs: per defender, vehicle, position and recon, how often the vehicle was
stopped and how often the defender lived, as a CSV and as two Markdown tables (without / with recon)."""
import collections
import csv
import sys

src, out_csv, out_md = sys.argv[1:]
groups = collections.defaultdict(list)
with open(src, newline="", encoding="utf-8") as fh:
    for r in csv.DictReader(fh):
        groups[(r["defender"], r["vehicle"], r["position"], r["recon"])].append(r)

defenders, vehicles, positions = [], [], []
for d, v, p, _ in groups:
    for lst, x in ((defenders, d), (vehicles, v), (positions, p)):
        if x not in lst:
            lst.append(x)

rows = []
for (d, v, p, rc), g in groups.items():
    n = len(g)
    rows.append({
        "defender": d, "vehicle": v, "position": p, "recon": rc, "runs": n,
        "stopped_pct": round(100 * sum(int(x["stopped"]) for x in g) / n, 1),
        "defender_alive_pct": round(100 * sum(int(x["defender_alive"]) for x in g) / n, 1),
        "vehicle_damage_pct": round(sum(float(x["vehicle_damage_pct"]) for x in g) / n, 1),
        "line_damage_mean": round(sum(float(x["line_damage"]) for x in g) / n, 2),
    })
with open(out_csv, "w", newline="", encoding="utf-8") as fh:
    w = csv.DictWriter(fh, fieldnames=list(rows[0]))
    w.writeheader()
    w.writerows(rows)

idx = {(r["defender"], r["vehicle"], r["position"], r["recon"]): r for r in rows}
with open(out_md, "w", encoding="utf-8") as fh:
    for rc, title in (("0", "Without recon"), ("1", "With a recon drone over the approach")):
        fh.write(f"### {title}\n\nCell: vehicle stopped % / defender alive %\n\n")
        fh.write("| Defender | Vehicle | " + " | ".join(positions) + " |\n")
        fh.write("|---|---|" + "---|" * len(positions) + "\n")
        for d in defenders:
            for v in vehicles:
                cells = []
                for p in positions:
                    r = idx[(d, v, p, rc)]
                    cells.append(f"{r['stopped_pct']:.0f} / {r['defender_alive_pct']:.0f}")
                fh.write(f"| {d} | {v} | " + " | ".join(cells) + " |\n")
        fh.write("\n")
print("summary rows:", len(rows))
