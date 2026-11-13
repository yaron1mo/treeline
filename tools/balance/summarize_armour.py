#!/usr/bin/env python3
"""Aggregate armoured vehicle Monte Carlo outcomes and paired sight/firepower screens."""
import collections
import csv
import math
import statistics
import sys

src, base_path, effects_path = sys.argv[1:]
with open(src, newline="", encoding="utf-8") as fh:
    rows = list(csv.DictReader(fh))
assert len(rows) == 9120, len(rows)
assert all(int(r["vehicles_killed"]) + int(r["vehicles_crossed"]) == int(r["count"]) for r in rows), "Vehicles lost in outcomes"
def mean(g,key):
    return statistics.mean(float(row[key]) for row in g)
def wilson(k,n):
    z=1.96
    p=k/n
    denom=1+z*z/n
    m=(p+z*z/(2*n))/denom
    e=z*math.sqrt(p*(1-p)/n+z*z/(4*n*n))/denom
    return round(100*(m-e),1),round(100*(m+e),1)
def file_write(path, recs):
    with open(path,"w",newline="",encoding="utf-8") as f:
        w=csv.DictWriter(f,fieldnames=list(recs[0]))
        w.writeheader()
        w.writerows(recs)

baseline=collections.defaultdict(list)
variants=collections.defaultdict(list)
for row in rows:
    key=(row["defender"],row["vehicle"],int(row["count"]),float(row["spawn_gap_s"]))
    if row["experiment"]=="baseline": baseline[key].append(row)
    elif row["experiment"] in {"sight_x2","damage_x2"}:
        variants[(*key,row["experiment"])].append(row)
    else: raise AssertionError(row["experiment"])

bases=[]
for (d,v,n,gap), g in sorted(baseline.items()):
    assert len(g)==100, (d,v,n,gap,len(g))
    stopped=sum(int(r["stopped_all_vehicles"]) for r in g)
    held=sum(int(r["held_line"]) for r in g)
    alive=sum(int(r["defender_survived"]) for r in g)
    stopci=wilson(stopped,len(g))
    bases.append({
      "defender":d,"vehicle":v,"count":n,"spawn_gap_s":gap,"runs":len(g),
      "vehicle_stopped_pct":round(mean(g,"stopped_all_vehicles")*100,1),
      "stopped_ci_low":stopci[0],"stopped_ci_high":stopci[1],
      "line_held_pct":round(mean(g,"held_line")*100,1),
      "defender_survived_pct":round(mean(g,"defender_survived")*100,1),
      "defender_hp_remaining_pct":round(mean(g,"defender_hp_pct"),1),
      "vehicles_killed_mean":round(mean(g,"vehicles_killed"),2),
      "vehicle_damage_pct":round(mean(g,"vehicle_damage_pct"),1),
      "vehicle_damage_hp":round(mean(g,"vehicle_damage_hp"),1),
      "line_damage_mean":round(mean(g,"line_damage"),1),
      "round_seconds_mean":round(mean(g,"seconds"),1),
    })
file_write(base_path,bases)

effects=[]
for (d,v,n,gap,variant),g in sorted(variants.items()):
    assert len(g)==40, (d,v,n,variant,len(g))
    controls={int(row["seed"]):row for row in baseline[d,v,n,gap]}
    pairs=[(row,controls[int(row["seed"])]) for row in g]
    def diff(k):return statistics.mean(float(a[k])-float(b[k]) for a,b in pairs)
    effects.append({
      "defender":d,"vehicle":v,"count":n,"spawn_gap_s":gap,
      "test_variant":variant,"runs":len(g),
      "baseline_vehicle_stopped_pct":round(100*statistics.mean(float(b["stopped_all_vehicles"]) for a,b in pairs),1),
      "variant_vehicle_stopped_pct":round(100*mean(g,"stopped_all_vehicles"),1),
      "delta_vehicle_stopped_pp":round(100*diff("stopped_all_vehicles"),1),
      "delta_line_held_pp":round(100*diff("held_line"),1),
      "delta_defender_survival_pp":round(100*diff("defender_survived"),1),
      "delta_vehicle_damage_pct_points":round(diff("vehicle_damage_pct"),1),
      "delta_vehicle_damage_hp":round(diff("vehicle_damage_hp"),1),
      "delta_defender_hp_pct_points":round(diff("defender_hp_pct"),1)
    })
file_write(effects_path,effects)
print("Validated",len(rows),"runs:",len(bases),"baseline conditions,",len(effects),"paired sensitivity conditions.")
