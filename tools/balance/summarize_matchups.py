#!/usr/bin/env python3
"""Summary of tools/balance/Matchups.cs runs: summary.csv (one row per defender, attacker, count, position) and
grid.json, the two blocks written to the Sheet tab balance_1v1 (killed % / defender alive %, then line damage).
Usage: summarize_matchups.py <runs.csv> <summary.csv> <grid.json> <title line>"""
import collections, csv, json, sys

src, out_csv, out_json, title = sys.argv[1:5]
g = collections.defaultdict(list)
with open(src, newline="", encoding="utf-8") as fh:
    for r in csv.DictReader(fh):
        g[(r["defender"], r["vehicle"], int(r["count"]), r["position"])].append(r)

D = ["rifle_squad", "machine_gun", "sniper", "mortar", "anti_tank", "air_defence", "fpv_operator", "bomber_operator"]
UA = {"rifle_squad": "Стрілець", "machine_gun": "Кулеметник", "sniper": "Снайпер", "mortar": "Міномет*",
      "anti_tank": "ПТУР*", "air_defence": "ППО*", "fpv_operator": "FPV*", "bomber_operator": "Бомбер*"}
V = ["recruit", "regular", "special_forces", "old_tank", "modern_tank", "ifv", "mrap", "buggy", "motorbike", "van", "fpv", "helicopter"]
VA = {"recruit": "Новобранець", "regular": "Regular", "special_forces": "Спецназ", "old_tank": "Старий танк",
      "modern_tank": "Сучасний танк", "ifv": "БМП", "mrap": "МРАП", "buggy": "Баггі", "motorbike": "Мотоцикл",
      "van": "Фургон", "fpv": "FPV (ворог)", "helicopter": "Гелікоптер"}
P = ["open", "trench_belt"]

def stats(d, v, c, p):
    rr = g[(d, v, c, p)]
    n = len(rr)
    return dict(killed=100 * sum(int(x["killed"]) for x in rr) / (n * c),
                alive=100 * sum(int(x["defender_alive"]) for x in rr) / n,
                held=100 * sum(float(x["line_damage"]) == 0 for x in rr) / n,
                line=sum(float(x["line_damage"]) for x in rr) / n)

rows = []
for d in D:
    for v in V:
        for c in range(1, 6):
            for p in P:
                s = stats(d, v, c, p)
                rows.append(dict(defender=d, attacker=v, count=c, position=p, killed_pct=round(s["killed"], 1),
                                 defender_alive_pct=round(s["alive"], 1), held_pct=round(s["held"], 1),
                                 line_damage_mean=round(s["line"], 2)))
with open(out_csv, "w", newline="", encoding="utf-8") as fh:
    w = csv.DictWriter(fh, fieldnames=list(rows[0]))
    w.writeheader()
    w.writerows(rows)

head = [["Нападник", "К-сть"] + sum([[UA[d], ""] for d in D], []), ["", ""] + ["відкрито", "окоп+смуга"] * len(D)]
T = [[title]] + head
for v in V:
    for c in range(1, 6):
        T.append([VA[v] if c == 1 else "", c] + [f"{stats(d, v, c, p)['killed']:.0f} / {stats(d, v, c, p)['alive']:.0f}" for d in D for p in P])
T += [[""], ["Шкода лінії в середньому (скільки дійшло до тилу; десант з техніки рахується). Лінія = 20."]] + head
for v in V:
    for c in range(1, 6):
        T.append([VA[v] if c == 1 else "", c] + [round(stats(d, v, c, p)["line"], 1) for d in D for p in P])
with open(out_json, "w", encoding="utf-8") as fh:
    json.dump(T, fh, ensure_ascii=False)
print(len(T), "rows")
