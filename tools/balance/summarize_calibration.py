#!/usr/bin/env python3
"""Aggregate Monte Carlo/open-field calibration and OAT sensitivity (paired seeds)."""
import collections
import csv
import math
import statistics
import sys

src, out_baseline, out_sensitivity, out_rank = sys.argv[1:5]
with open(src, newline="", encoding="utf-8") as fh:
    rows = list(csv.DictReader(fh))
assert len(rows) == 28620, ("Unexpected simulation count", len(rows))
assert all(int(r["killed"]) + int(r["crossed"]) == int(r["count"]) for r in rows), "Missing/extra attackers"
def as_float(r, name):
    return float(r[name])
def wilson(v, n):
    if n == 0:
        return 0, 0
    p = v / n
    z = 1.96
    den = 1 + z*z/n
    centre = (p + z*z/(2*n)) / den
    radius = z*math.sqrt(p*(1-p)/n + z*z/(4*n*n)) / den
    return max(0, 100*(centre-radius)), min(100, 100*(centre+radius))
def mean_of(rs, col):
    return statistics.mean(as_float(x,col) for x in rs)
def penetration(rs):
    return statistics.mean(as_float(x,"crossed") / as_float(x,"count") for x in rs)
def pct(v):
    return round(100*v, 2)
def ci_percent(values):
    avg = statistics.mean(values)
    if len(values) < 2: return (pct(avg),pct(avg))
    err = 1.96 * statistics.stdev(values) / math.sqrt(len(values))
    return (max(0, pct(avg-err)), min(100, pct(avg+err)))
def write(path, header, data):
    with open(path,"w",newline="",encoding="utf-8") as fh:
        out=csv.DictWriter(fh,fieldnames=header)
        out.writeheader()
        out.writerows(data)

b = collections.defaultdict(list)
s = collections.defaultdict(list)
for r in rows:
    key=(r["defender"],r["attacker"],int(r["count"]),float(r["spawn_gap_s"]))
    if r["experiment"]=="baseline": b[key].append(r)
    elif r["experiment"]=="sensitivity":
        key += (r["parameter"],float(r["factor"]))
        s[key].append(r)
    else: raise AssertionError("Unexpected experiment")

baseline=[]
for key, group in sorted(b.items()):
    assert len(group)==100
    hold_lo,hold_hi=wilson(sum(int(r["held"]) for r in group), len(group))
    alive_lo,alive_hi=wilson(sum(int(r["defender_survived"]) for r in group),len(group))
    penetration_ci=ci_percent([int(r["crossed"])/int(r["count"]) for r in group])
    baseline.append(dict(defender=key[0],attacker=key[1],count=key[2],spawn_gap_s=key[3],runs=len(group),
      penetration_pct=pct(penetration(group)),penetration_ci_low=penetration_ci[0],penetration_ci_high=penetration_ci[1],
      held_pct=pct(mean_of(group,"held")),held_ci_low=round(hold_lo,1),held_ci_high=round(hold_hi,1),
      defender_survival_pct=pct(mean_of(group,"defender_survived")),
      defender_survival_ci_low=round(alive_lo,1),defender_survival_ci_high=round(alive_hi,1),
      defender_hp_remaining_pct=round(mean_of(group,"defender_hp_pct"),1),
      mean_killed=round(mean_of(group,"killed"),2)))
write(out_baseline,list(baseline[0]),baseline)

effects=[]
for key, group in sorted(s.items()):
    assert len(group)==40, (key,len(group))
    defn,attacker,count,gap,param,factor=key
    control=b[defn,attacker,count,gap]
    ctrl={int(r["seed"]):r for r in control}
    # Common random numbers: compare changed scenario to the SAME 40 baseline seeds.
    changes=[]
    for r in group:
        seed=int(r["seed"])
        base=ctrl[seed]
        changes.append((100*(int(r["crossed"])-int(base["crossed"]))/count,
                        100*(int(r["defender_survived"])-int(base["defender_survived"])),
                        int(r["killed"])-int(base["killed"]),
                        float(r["defender_hp_pct"])-float(base["defender_hp_pct"]),
                        100*(int(r["held"])-int(base["held"]))))
    averages=[round(statistics.mean(pair[i] for pair in changes),2) for i in range(5)]
    effects.append(dict(defender=defn,attacker=attacker,count=count,spawn_gap_s=gap,
        parameter=param,factor=factor,runs=len(group),
        baseline_penetration_pct=pct(statistics.mean(int(ctrl[int(r["seed"])]["crossed"])/count for r in group)),
        penetration_pct=pct(penetration(group)),delta_penetration_pp=averages[0],
        delta_defender_survival_pp=averages[1],delta_mean_killed=averages[2],
        delta_defender_hp_pp=averages[3],delta_held_pp=averages[4]))
write(out_sensitivity,list(effects[0]),effects)

rank=[]
grouped=collections.defaultdict(list)
for e in effects:
    grouped[(e["parameter"],e["defender"],e["attacker"])].append(e)
    grouped[(e["parameter"],"ALL","ALL")].append(e)
for key, xx in sorted(grouped.items()):
    # Average absolute impact of a 20% stat change across 3,4,5 attacker wave sizes.
    rank.append(dict(parameter=key[0],defender=key[1],attacker=key[2],tested_conditions=len(xx),
      mean_abs_penetration_pp=round(statistics.mean(abs(float(e["delta_penetration_pp"])) for e in xx),2),
      mean_abs_survival_pp=round(statistics.mean(abs(float(e["delta_defender_survival_pp"])) for e in xx),2),
      mean_abs_kills=round(statistics.mean(abs(float(e["delta_mean_killed"])) for e in xx),3),
      mean_abs_defender_hp_pp=round(statistics.mean(abs(float(e["delta_defender_hp_pp"])) for e in xx),2)))
rank.sort(key=lambda r:(0 if r["defender"]=="ALL" else 1,r["defender"],r["attacker"],-r["mean_abs_penetration_pp"]))
write(out_rank,list(rank[0]),rank)
print("Validated Monte Carlo",len(rows),"runs; baselines",len(baseline),"sensitivity conditions",len(effects),"rank rows",len(rank))
print("Note: the OAT screen changes +/-20% of one stat, not optimized target behaviour.")
