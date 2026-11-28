#!/usr/bin/env python3
"""Analysis for the mistral/squads task (2026-11-27).

Reads the merged wave-cost.csv of a Balance run (field,wave,squad,threat,cost,held_at_cost;
cost NaN = the squad cannot hold the wave with max_budget), tools/balance/squads.csv
(squad,defenders) and tools/balance/squad_waves.csv (wave,unit,count,...).

Per field and squad: held share, cost premium (median of squad cost / cost of the "all"
squad on the waves the squad holds), rank (waves where the squad is the cheapest that holds).
Per defender type: must-have (share of waves that no squad without the type holds) and
unused (share of waves where no squad in the best 10 has the type). A squad is dominant when
it holds 90% of the waves or more with a cost premium of 1.2 or less.

Writes squads.json and prints markdown tables for report.md.

Usage: analyze.py <wave-cost.csv> <squads.csv> <squad_waves.csv> <out.json>
"""
import csv
import json
import statistics
import sys
from collections import defaultdict

CATEGORY = {
    "recruit": "infantry", "regular": "infantry", "special_forces": "infantry",
    "motorbike": "vehicles", "buggy": "vehicles", "van": "vehicles",
    "mrap": "armour", "ifv": "armour", "old_tank": "armour", "modern_tank": "armour",
    "helicopter": "helicopter",
    "recon_uav": "drones", "fpv": "drones", "one_way_drone": "drones",
}
CATEGORIES = ["infantry", "vehicles", "armour", "helicopter", "drones"]


def main():
    cost_csv, squads_csv, waves_csv, out_json = sys.argv[1:5]

    squad_types = {}
    for r in csv.DictReader(open(squads_csv, newline="", encoding="utf-8")):
        squad_types[r["squad"]] = set(r["defenders"].split("|"))
    types = sorted(set().union(*squad_types.values()))

    wave_units = defaultdict(dict)
    for r in csv.DictReader(open(waves_csv, newline="", encoding="utf-8")):
        wave_units[int(r["wave"])][r["unit"]] = int(r["count"])
    wave_cats = {w: sorted({CATEGORY[u] for u in us}) for w, us in wave_units.items()}

    rows = defaultdict(dict)  # field -> wave -> squad -> (cost or None)
    fields = []
    for r in csv.DictReader(open(cost_csv, newline="", encoding="utf-8")):
        f = r["field"]
        if f not in rows:
            fields.append(f)
        w = int(r["wave"])
        cost = None if r["cost"] == "NaN" else float(r["cost"])
        rows[f].setdefault(w, {})[r["squad"]] = cost

    out = {"fields": fields, "types": types, "categories": CATEGORIES,
           "wave_units": {str(w): u for w, u in sorted(wave_units.items())},
           "wave_categories": {str(w): c for w, c in sorted(wave_cats.items())},
           "squads": {}, "type_stats": {}, "dominant": {}, "heat": {}, "best_squad": {}}

    for f in fields:
        waves = sorted(rows[f])
        n = len(waves)
        all_cost = {w: rows[f][w].get("all") for w in waves}
        holding = {w: {s: c for s, c in rows[f][w].items() if c is not None} for w in waves}
        cheapest = {w: min(holding[w].values()) for w in waves if holding[w]}
        best10 = {w: [s for s, _ in sorted(holding[w].items(), key=lambda kv: (kv[1], kv[0]))[:10]]
                  for w in waves if holding[w]}

        squad_stats = {}
        for s, stypes in squad_types.items():
            held = [w for w in waves if s in holding[w]]
            prem = [rows[f][w][s] / all_cost[w] for w in held if all_cost[w] not in (None, 0)]
            rank = sum(1 for w in held if rows[f][w][s] == cheapest[w])
            squad_stats[s] = {
                "held": len(held), "held_share": round(len(held) / n, 4),
                "premium": round(statistics.median(prem), 4) if prem else None,
                "rank": rank, "n_waves": n,
            }
        out["squads"][f] = squad_stats

        type_stats = {}
        for t in types:
            without = [s for s, st in squad_types.items() if t not in st]
            must = sum(1 for w in waves if holding[w] and all(s not in holding[w] for s in without))
            unused = sum(1 for w in waves if best10.get(w) and all(t not in squad_types[s] for s in best10[w]))
            type_stats[t] = {"must_have": round(must / n, 4), "unused": round(unused / n, 4)}
        out["type_stats"][f] = type_stats

        out["dominant"][f] = sorted(s for s, st in squad_stats.items()
                                     if st["held_share"] >= 0.9 and st["premium"] is not None
                                     and st["premium"] <= 1.2)
        # best squad: highest held share, then lowest premium, then name
        out["best_squad"][f] = min(squad_stats.items(),
                                   key=lambda kv: (-kv[1]["held_share"],
                                                   kv[1]["premium"] if kv[1]["premium"] is not None else 99,
                                                   kv[0]))[0]

        heat = {t: {} for t in types}
        for t in types:
            for c in CATEGORIES:
                cw = [w for w in waves if c in wave_cats.get(w, [])]
                if not cw:
                    heat[t][c] = None
                    continue
                hit = sum(1 for w in cw if best10.get(w) and any(t in squad_types[s] for s in best10[w]))
                heat[t][c] = round(hit / len(cw), 4)
        out["heat"][f] = heat

        print("## Field: %s (%d waves, %d squads)" % (f, n, len(squad_types)))
        print("Best squad: %s" % out["best_squad"][f])
        print("Dominant squads (held >= 90%%, premium <= 1.2): %s"
              % (", ".join(out["dominant"][f]) or "none"))
        print()
        print("| Squad | Held share | Cost premium | Rank |")
        print("| --- | --- | --- | --- |")
        best15 = sorted(squad_stats.items(),
                        key=lambda kv: (-kv[1]["rank"], -kv[1]["held_share"],
                                        kv[1]["premium"] if kv[1]["premium"] is not None else 99, kv[0]))[:15]
        for s, st in best15:
            print("| %s | %.2f | %s | %d |" % (s, st["held_share"],
                  "n/a" if st["premium"] is None else "%.3f" % st["premium"], st["rank"]))
        print()
        print("| Type | Must-have | Unused |")
        print("| --- | --- | --- |")
        for t in types:
            print("| %s | %.2f | %.2f |" % (t, type_stats[t]["must_have"], type_stats[t]["unused"]))
        print()

    with open(out_json, "w", encoding="utf-8") as fh:
        json.dump(out, fh, indent=1, sort_keys=True)
    print("wrote", out_json)


if __name__ == "__main__":
    main()
