#!/usr/bin/env python3
"""Which stat of a unit matters most (Yaroslav, 2026-11-16).

For each variant (one column of one unit changed in a copy of unity/Assets/Data) runs Matchups.exe against every
defender, open and trench+belt, 1 to 5 attackers, and compares with the unchanged data.

Usage: sensitivity.py <Matchups.exe> <repo root> <work dir> <out dir> [jobs]
Writes <out dir>/variants.csv (one row per variant: mean line damage, kills, defender alive, change against base)
and <out dir>/by_defender.csv (the same per defender).
"""
import collections, csv, io, os, shutil, subprocess, sys
from concurrent.futures import ThreadPoolExecutor

exe, root, work, out = sys.argv[1:5]
jobs = int(sys.argv[5]) if len(sys.argv) > 5 else 2

# unit -> list of (label, {column: value})
VARIANTS = {
    "mrap": [
        ("hp 30", {"hp": "30"}), ("hp 90", {"hp": "90"}),
        ("armour medium", {"armour": "medium"}), ("armour low", {"armour": "low"}),
        ("speed 5.5", {"speed_mps": "5.5"}), ("speed 16.5", {"speed_mps": "16.5"}),
        ("carries 2", {"carries_count": "2"}), ("carries 6", {"carries_count": "6"}),
        ("carries regular", {"carries_unit": "regular"}),
        ("dismount at 0.4", {"dismount_at_hp": "0.4"}), ("dismount at 0.1", {"dismount_at_hp": "0.1"}),
        ("dismount every 0.35 s", {"dismount_interval_s": "0.35"}), ("dismount every 1.4 s", {"dismount_interval_s": "1.4"}),
        ("no gun", {"damage": "0"}), ("gun damage 4", {"damage": "4"}),
        ("sight 15", {"sight_range_m": "15"}), ("sight 45", {"sight_range_m": "45"}),
        ("hit radius 1", {"hit_radius_m": "1"}), ("hit radius 3", {"hit_radius_m": "3"}),
        ("blast 0", {"blast_radius_m": ""}), ("blast 12", {"blast_radius_m": "12"}),
    ],
    "van": [
        ("hp 25", {"hp": "25"}), ("hp 75", {"hp": "75"}),
        ("armour low", {"armour": "low"}), ("armour medium", {"armour": "medium"}),
        ("speed 5.5", {"speed_mps": "5.5"}), ("speed 16.5", {"speed_mps": "16.5"}),
        ("carries 6", {"carries_count": "6"}), ("carries 18", {"carries_count": "18"}),
        ("carries regular", {"carries_unit": "regular"}),
        ("dismount at 0.4", {"dismount_at_hp": "0.4"}), ("dismount at 0.1", {"dismount_at_hp": "0.1"}),
        ("dismount every 0.3 s", {"dismount_interval_s": "0.3"}), ("dismount every 1.2 s", {"dismount_interval_s": "1.2"}),
        ("hit radius 1", {"hit_radius_m": "1"}), ("hit radius 3", {"hit_radius_m": "3"}),
        ("blast 0", {"blast_radius_m": ""}), ("blast 12", {"blast_radius_m": "12"}),
    ],
    "buggy": [
        ("hp 38", {"hp": "38"}), ("hp 50", {"hp": "50"}),
        ("speed 7", {"speed_mps": "7"}), ("speed 15", {"speed_mps": "15"}),
        ("armour low", {"armour": "low"}), ("hit radius 1", {"hit_radius_m": "1"}),
    ],
    "motorbike": [
        ("hp 23", {"hp": "23"}), ("hp 30", {"hp": "30"}),
        ("speed 7", {"speed_mps": "7"}), ("speed 15", {"speed_mps": "15"}),
        ("armour low", {"armour": "low"}), ("hit radius 0.5", {"hit_radius_m": "0.5"}),
    ],
}

src = os.path.join(root, "unity/Assets/Data")


def make_variant(unit, label, changes):
    d = os.path.join(work, unit, label.replace(" ", "_").replace(".", "p"))
    data = os.path.join(d, "unity/Assets/Data")
    if os.path.exists(d):
        shutil.rmtree(d)
    shutil.copytree(src, data)
    path = os.path.join(data, "units.csv")
    with open(path, newline="", encoding="utf-8") as fh:
        rows = list(csv.reader(fh))
    h = rows[0]
    for r in rows[1:]:
        if r[0] == unit:
            for k, v in changes.items():
                r[h.index(k)] = v
    buf = io.StringIO()
    csv.writer(buf, lineterminator="\n").writerows(rows)
    with open(path, "w", newline="", encoding="utf-8") as fh:
        fh.write(buf.getvalue())
    return d


def run(unit, label, data_root):
    dest = os.path.join(work, unit + "__" + label.replace(" ", "_") + ".csv")
    env = dict(os.environ, MATCHUPS_ATTACKERS=unit)
    subprocess.run(["mono", exe, dest, data_root], env=env, check=True, stdout=subprocess.DEVNULL)
    return unit, label, dest


tasks = []
for unit, vs in VARIANTS.items():
    tasks.append((unit, "base", root))
    for label, ch in vs:
        tasks.append((unit, label, make_variant(unit, label, ch)))

with ThreadPoolExecutor(jobs) as ex:
    results = list(ex.map(lambda t: run(*t), tasks))


def stats(path):
    tot = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0])
    with open(path, newline="", encoding="utf-8") as fh:
        for r in csv.DictReader(fh):
            for key in ("all", r["defender"]):
                t = tot[key]
                t[0] += float(r["line_damage"]); t[1] += float(r["killed"]); t[2] += float(r["defender_alive"]); t[3] += 1
    return {k: (v[0] / v[3], v[1] / v[3], 100 * v[2] / v[3]) for k, v in tot.items()}


S = {(u, l): stats(p) for u, l, p in results}
os.makedirs(out, exist_ok=True)
with open(os.path.join(out, "variants.csv"), "w", newline="", encoding="utf-8") as fh:
    w = csv.writer(fh)
    w.writerow(["unit", "variant", "line_damage", "line_damage_change_pct", "killed", "defender_alive_pct"])
    for unit, vs in VARIANTS.items():
        base = S[(unit, "base")]["all"]
        for label in ["base"] + [l for l, _ in vs]:
            s = S[(unit, label)]["all"]
            w.writerow([unit, label, round(s[0], 2), round(100 * (s[0] - base[0]) / base[0], 1) if base[0] else "",
                        round(s[1], 2), round(s[2], 1)])
with open(os.path.join(out, "by_defender.csv"), "w", newline="", encoding="utf-8") as fh:
    w = csv.writer(fh)
    w.writerow(["unit", "variant", "defender", "line_damage", "killed", "defender_alive_pct"])
    for (unit, label), s in S.items():
        for dname, v in s.items():
            if dname != "all":
                w.writerow([unit, label, dname, round(v[0], 2), round(v[1], 2), round(v[2], 1)])
print("variants:", len(results))
