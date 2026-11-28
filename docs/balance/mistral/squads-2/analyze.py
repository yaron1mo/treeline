#!/usr/bin/env python3
"""Analysis for the mistral/squads-2 task (2026-11-28, issue #57).

Reads the merged wave-cost.csv of the local WaveCost squad runs
(field,wave,squad,threat,cost,held_at_cost; cost NaN = the squad cannot hold the wave
with max_budget 12000), the squads csv per slot size (squad,defenders) and
tools/balance/squad_waves.csv (wave,unit,count,...).

Bands: by the measured cost of the squad "all" on Forest belts in this run:
500-6000 and 6000-12000 (waves with NaN cost of "all" form the group "not held").

Per slot size, field and band: held share, cost premium (median of squad cost / cost of
"all" on the waves the squad holds), rank (waves where the squad is the cheapest that
holds). Per defender type: must-have (share of waves that no squad without the type
holds), best-10 share and unused (share of waves where no squad in the best 10 has the
type). A squad is dominant when it holds 90% of the band waves or more with a cost
premium of 1.2 or less.

Writes squads2.json and prints markdown tables for report.md.

Usage: analyze.py <wave-cost.csv> <squads-s3.csv> <squads-s4.csv> <squads-s5.csv> <squad_waves.csv> <out.json>
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
BANDS = ["500-6000", "6000-12000", "not held"]


def band_of(cost):
    if cost is None:
        return "not held"
    return "500-6000" if cost < 6000 else "6000-12000"


def main():
    cost_csv, s3_csv, s4_csv, s5_csv, waves_csv, out_json = sys.argv[1:7]

    squad_types = {}
    for p in (s3_csv, s4_csv, s5_csv):
        for r in csv.DictReader(open(p, newline="", encoding="utf-8")):
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

    # band per wave from the cost of "all" on Forest belts
    forest = "Forest belts"
    wave_band = {}
    for w in wave_units:
        c = rows[forest].get(w, {}).get("all")
        wave_band[w] = band_of(c)

    out = {"fields": fields, "types": types, "categories": CATEGORIES, "bands": BANDS,
           "wave_units": {str(w): u for w, u in sorted(wave_units.items())},
           "wave_categories": {str(w): c for w, c in sorted(wave_cats.items())},
           "wave_band": {str(w): b for w, b in sorted(wave_band.items())},
           "forest_all_cost": {str(w): rows[forest].get(w, {}).get("all") for w in sorted(wave_units)},
           "sizes": {}, "type_stats": {}, "dominant": {}, "heat": {}, "best_squad": {}}

    # squad -> size (number of types, "all" = 9)
    squad_size = {s: len(t) for s, t in squad_types.items()}

    for size in (3, 4, 5):
        sqs = {s: t for s, t in squad_types.items() if squad_size[s] == size or s == "all"}
        out["sizes"][size] = {}
        for f in fields:
            waves = sorted(rows[f])
            out["sizes"][size][f] = {}
            for band in BANDS:
                bw = [w for w in waves if wave_band[w] == band]
                if not bw:
                    continue
                all_cost = {w: rows[f][w].get("all") for w in bw}
                holding = {w: {s: c for s, c in rows[f][w].items() if c is not None and s in sqs}
                           for w in bw}
                cheapest = {w: min(holding[w].values()) for w in bw if holding[w]}
                best10 = {w: [s for s, _ in sorted(holding[w].items(), key=lambda kv: (kv[1], kv[0]))[:10]]
                          for w in bw if holding[w]}
                n = len(bw)

                squad_stats = {}
                for s, stypes in sqs.items():
                    held = [w for w in bw if s in holding[w]]
                    prem = [rows[f][w][s] / all_cost[w] for w in held if all_cost[w] not in (None, 0)]
                    rank = sum(1 for w in held if rows[f][w][s] == cheapest[w])
                    squad_stats[s] = {
                        "held": len(held), "held_share": round(len(held) / n, 4),
                        "premium": round(statistics.median(prem), 4) if prem else None,
                        "rank": rank, "n_waves": n,
                    }
                out["sizes"][size][f][band] = squad_stats

                type_stats = {}
                for t in types:
                    without = [s for s, st in sqs.items() if t not in st]
                    must = sum(1 for w in bw if holding[w] and all(s not in holding[w] for s in without))
                    hit = sum(1 for w in bw if best10.get(w) and any(t in sqs[s] for s in best10[w]))
                    unused = sum(1 for w in bw if best10.get(w) and all(t not in sqs[s] for s in best10[w]))
                    type_stats[t] = {"must_have": round(must / n, 4),
                                     "best10": round(hit / n, 4),
                                     "unused": round(unused / n, 4)}
                out["type_stats"].setdefault(size, {}).setdefault(f, {})[band] = type_stats

                out["dominant"].setdefault(size, {}).setdefault(f, {})[band] = \
                    sorted(s for s, st in squad_stats.items()
                           if s != "all" and st["held_share"] >= 0.9 and st["premium"] is not None
                           and st["premium"] <= 1.2)
                out["best_squad"].setdefault(size, {}).setdefault(f, {})[band] = \
                    min(squad_stats.items(),
                        key=lambda kv: (-kv[1]["held_share"],
                                        kv[1]["premium"] if kv[1]["premium"] is not None else 99,
                                        kv[0]))[0]

                heat = {t: {} for t in types}
                for t in types:
                    for c in CATEGORIES:
                        cw = [w for w in bw if c in wave_cats.get(w, [])]
                        if not cw:
                            heat[t][c] = None
                            continue
                        h = sum(1 for w in cw if best10.get(w) and any(t in sqs[s] for s in best10[w]))
                        heat[t][c] = round(h / len(cw), 4)
                out["heat"].setdefault(size, {}).setdefault(f, {})[band] = heat

                print("## size %d, field: %s, band %s (%d waves)" % (size, f, band, n))
                print("Best squad: %s" % out["best_squad"][size][f][band])
                print("Dominant squads (held >= 90%%, premium <= 1.2, without 'all'): %s"
                      % (", ".join(out["dominant"][size][f][band]) or "none"))
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
                print("| Type | Must-have | Best-10 share | Unused |")
                print("| --- | --- | --- | --- |")
                for t in types:
                    print("| %s | %.2f | %.2f | %.2f |" % (t, type_stats[t]["must_have"],
                          type_stats[t]["best10"], type_stats[t]["unused"]))
                print()

    with open(out_json, "w", encoding="utf-8") as fh:
        json.dump(out, fh, indent=1, sort_keys=True)
    print("wrote", out_json)


if __name__ == "__main__":
    main()
