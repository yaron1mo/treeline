#!/usr/bin/env python3
"""Balance report for the balance-run workflow (2026-11-23).

Reads the wave cost results (WaveCost.cs) and the scripted-player results (PlaytestRun.cs), compares the money a player
has before each wave with the wave cost on the gate field, and checks the ratio against tools/balance/target_curve.csv.

Money before wave w = start_budget + sum over held waves i < w of (wave_income + kill_reward_share * rewards of wave i).
The rewards assume that the player kills every attacker. Prices that rise with each purchase are not in the money.

Usage: report.py <repo root> <results dir> <out dir> [baseline dir]
  results dir: wave-cost*.csv and playtest*.csv files (any sub-folders)
  out dir:     report.md, report.json, wave-cost.csv, playtest.csv
  baseline:    wave-cost.csv and playtest.csv from an earlier run, to show the change
Exit code 0 always; report.json has "verdict": "pass" or "fail".
"""
import csv
import glob
import json
import math
import os
import sys

GATE_FIELD = "Forest belts"

root, results, out = sys.argv[1], sys.argv[2], sys.argv[3]
baseline = sys.argv[4] if len(sys.argv) > 4 else None
data = os.path.join(root, "unity/Assets/Data")
os.makedirs(out, exist_ok=True)


def rows(path):
    with open(path, newline="", encoding="utf-8") as fh:
        return list(csv.DictReader(fh))


def num(v, default=None):
    try:
        f = float(v)
        return default if math.isnan(f) else f
    except (TypeError, ValueError):
        return default


units = {r["id"]: r for r in rows(os.path.join(data, "units.csv"))}
rules = {r["key"]: r["value"] for r in rows(os.path.join(data, "battle_rules.csv")) if r.get("key")}
waves = {}
for r in rows(os.path.join(data, "waves.csv")):
    waves.setdefault(int(r["wave"]), []).append(r)
start = num(rules.get("start_budget"), 250)
income = num(rules.get("wave_income"), 100)
share = num(rules.get("kill_reward_share"), 0.25)

# Merge the shards.
cost_rows = []
for p in sorted(glob.glob(os.path.join(results, "**", "wave-cost*.csv"), recursive=True)):
    cost_rows += rows(p)
play_rows = []
for p in sorted(glob.glob(os.path.join(results, "**", "playtest*.csv"), recursive=True)):
    play_rows += rows(p)
for name, rs in (("wave-cost.csv", cost_rows), ("playtest.csv", play_rows)):
    if rs:
        with open(os.path.join(out, name), "w", newline="", encoding="utf-8") as fh:
            w = csv.DictWriter(fh, fieldnames=list(rs[0].keys()), lineterminator="\n")
            w.writeheader()
            w.writerows(rs)

cost = {}
fields = []
for r in cost_rows:
    if r["field"] not in fields:
        fields.append(r["field"])
    cost[(r["field"], int(r["wave"]))] = num(r["cost"])
fields.sort(key=lambda f: (f != GATE_FIELD, f))

base_cost = {}
if baseline and os.path.exists(os.path.join(baseline, "wave-cost.csv")):
    for r in rows(os.path.join(baseline, "wave-cost.csv")):
        base_cost[(r["field"], int(r["wave"]))] = num(r["cost"])

# Target curve.
bands, new_unit_min = [], None
for r in rows(os.path.join(root, "tools/balance/target_curve.csv")):
    if r["wave_from"] == "new_unit":
        new_unit_min = num(r["min_ratio"])
    else:
        bands.append((int(r["wave_from"]), int(r["wave_to"]), num(r["min_ratio"]), num(r["max_ratio"])))


def reward(u):
    rv = num(units[u].get("reward_value"))
    return rv if rv is not None else (num(units[u].get("threat_value"), 0) or 0)


seen = set()
money = start
table, fails = [], []
for w in sorted(waves):
    groups = waves[w]
    new = [g["unit"] for g in groups if g["unit"] not in seen]
    seen.update(g["unit"] for g in groups)
    threat = sum(int(g["count"]) * (num(units[g["unit"]].get("threat_value"), 0) or 0) for g in groups)
    lo = hi = None
    for a, b, mn, mx in bands:
        if a <= w <= b:
            lo, hi = mn, mx
    if new and w > 1 and new_unit_min is not None:
        lo = max(lo or 0, new_unit_min)
        if hi is not None:
            hi = max(hi, lo + 0.2)
    c = cost.get((GATE_FIELD, w))
    ratio = money / c if c else None
    if c is None and (GATE_FIELD, w) in cost:
        status = "not held"
    elif ratio is None:
        status = "no data"
    elif lo is not None and ratio < lo - 1e-9:
        status = "too hard"
    elif hi is not None and ratio > hi + 1e-9:
        status = "too easy"
    else:
        status = "ok"
    if status != "ok":
        fails.append({"wave": w, "status": status, "ratio": ratio, "min": lo, "max": hi, "cost": c, "money": money})
    table.append(dict(wave=w, units=", ".join(f"{g['count']} {g['unit']}" for g in groups), new=new, threat=threat,
                      costs=[cost.get((f, w)) for f in fields], base=base_cost.get((GATE_FIELD, w)), money=money,
                      ratio=ratio, lo=lo, hi=hi, status=status))
    money += income + share * sum(int(g["count"]) * reward(g["unit"]) for g in groups)


def fmt(v, d=0):
    return "-" if v is None else f"{v:.{d}f}"


verdict = "pass" if not fails else "fail"
md = [f"# Balance report: {verdict.upper()}", "",
      f"Gate field: **{GATE_FIELD}**. Ratio = money before the wave / wave cost. Target: `tools/balance/target_curve.csv`.",
      f"Economy: start_budget {start:g}, wave_income {income:g}, kill_reward_share {share:g}.", "",
      "| Wave | Attackers | New | threat | " + " | ".join(f"Cost {f}" for f in fields) +
      " | Baseline cost | Money | Ratio | Target | Status |",
      "|" + "---|" * (9 + len(fields))]
for t in table:
    target = f"{fmt(t['lo'], 1)}–{fmt(t['hi'], 1)}" if t["hi"] is not None else f"≥ {fmt(t['lo'], 1)}"
    mark = "✅" if t["status"] == "ok" else "❌ " + t["status"]
    md.append(f"| {t['wave']} | {t['units']} | {', '.join(t['new'])} | {t['threat']:.0f} | " +
              " | ".join(fmt(c) for c in t["costs"]) +
              f" | {fmt(t['base'])} | {t['money']:.0f} | {fmt(t['ratio'], 2)} | {target} | {mark} |")
md += ["", f"Waves in target: {len(table) - len(fails)} of {len(table)}.", ""]

if play_rows:
    md += ["## Scripted players (20 waves in a row)", "", "| Field | Style | Runs | Mean waves held | Best |", "|---|---|---|---|---|"]
    groups = {}
    for r in play_rows:
        groups.setdefault((r["field"], r["style"]), []).append(int(r["waves_held"]))
    for (f, s), v in sorted(groups.items()):
        md.append(f"| {f} | {s} | {len(v)} | {sum(v) / len(v):.1f} | {max(v)} |")
    md.append("")

md += ["The cost is an upper bound: the scripted player plays worse than a person.",
       "The money assumes every attacker is killed."]
with open(os.path.join(out, "report.md"), "w", encoding="utf-8") as fh:
    fh.write("\n".join(md) + "\n")
with open(os.path.join(out, "report.json"), "w", encoding="utf-8") as fh:
    json.dump({"verdict": verdict, "gate_field": GATE_FIELD, "fails": fails,
               "waves": [{k: t[k] for k in ("wave", "threat", "money", "ratio", "lo", "hi", "status")} | {"cost": t["costs"][0] if fields else None}
                         for t in table]}, fh, indent=1)
print("\n".join(md))
