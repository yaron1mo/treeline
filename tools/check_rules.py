#!/usr/bin/env python3
"""Check rules/attackers.json for mistakes, and print the attacker table.

    python3 tools/check_rules.py            # check only
    python3 tools/check_rules.py --table    # check, then print a Markdown table for the design doc
"""
import json
import sys
from pathlib import Path

RULES = Path(__file__).resolve().parent.parent / "rules" / "attackers.json"


def get(obj, dotted):
    for part in dotted.split("."):
        if not isinstance(obj, dict):
            return None
        obj = obj.get(part, obj.get("weapon", {}).get(part) if isinstance(obj.get("weapon"), dict) else None) if part not in obj else obj[part]
    return obj


def find_field(unit, name):
    """A 'proposed' entry may name a top-level field, a nested one (carries.unit) or a weapon/spotting field."""
    if "." in name:
        return get(unit, name) is not None
    if name in unit:
        return True
    return any(isinstance(v, dict) and name in v for v in unit.values())


def check(rules):
    errors = []
    types, armours = set(rules["unit_types"]), set(rules["tiers"]["armour"])
    dtypes = set(rules["damage_types"])
    tags = {k for k in rules["defender_weapon_tags"] if k != "notes"} | set(rules.get("tag_groups", {}))
    units = rules["units"]
    for uid, u in units.items():
        where = f"units.{uid}"
        if u["type"] not in types:
            errors.append(f"{where}: unknown type {u['type']!r}")
        if u["armour"] not in armours:
            errors.append(f"{where}: unknown armour {u['armour']!r}")
        w = u.get("weapon")
        if w and w.get("damage_type") not in dtypes:
            errors.append(f"{where}: unknown damage type {w.get('damage_type')!r}")
        for t in u.get("targetable_by", []):
            if t not in tags:
                errors.append(f"{where}: unknown weapon tag {t!r} in targetable_by")
        if not u.get("targetable_by"):
            errors.append(f"{where}: nothing can hit this unit")
        c = u.get("carries")
        if c and c["unit"] not in units:
            errors.append(f"{where}: carries unknown unit {c['unit']!r}")
        if u.get("requires") and u["requires"] not in units:
            errors.append(f"{where}: requires unknown unit {u['requires']!r}")
        for f in u.get("proposed", []):
            if not find_field(u, f):
                errors.append(f"{where}: 'proposed' names {f!r}, which the unit does not have")
        if u["type"] != "aviation" and u.get("adaptation_lag") not in (1, 2, 3, 4):
            errors.append(f"{where}: ground units need an adaptation_lag of 1-4 waves")
    if "line" in rules and not isinstance(rules["line"].get("hp"), int):
        errors.append("line.hp must be a whole number")
    for uid, u in units.items():
        if u.get("crosses_line", u["type"] != "aviation") and "line_damage" not in u:
            errors.append(f"units.{uid}: crosses our line but has no line_damage")
    return errors


def table(rules):
    star = lambda u, f: "*" if f in u.get("proposed", []) else ""
    rows = ["| Unit | Type | HP | Armour | Damage /s | Accuracy | Range | Speed | Adapts | Line dmg | Special |",
            "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |"]
    for uid, u in rules["units"].items():
        w = u.get("weapon") or {}
        dmg = "—" if not w else (f"{w['hit_damage']} per hit" if "hit_damage" in w else f"{w['dps']} {w['damage_type'].replace('_', ' ')}")
        acc = f"{w['accuracy']}{star(u, 'accuracy')}" if "accuracy" in w else "—"
        rng = f"{w['range_m']} m{star(u, 'range_m')}" if "range_m" in w else "—"
        lag = u.get("adaptation_lag")
        lag = "—" if lag is None else f"{lag} wave{'s' if lag > 1 else ''}{star(u, 'adaptation_lag')}"
        ld = u.get('line_damage')
        ld = '—' if ld is None else f"{ld}{star(u, 'line_damage')}"
        special = []
        if u.get("ew") == "always":
            special.append("EW always")
        elif u.get("ew") == "sometimes":
            special.append(f"EW {int(u['ew_chance'] * 100)}%")
        if u.get("nvg"):
            special.append("NVG")
        if u.get("tree_move_cost"):
            special.append(f"trees cost {u['tree_move_cost']}")
        c = u.get("carries")
        if c:
            name = rules["units"][c["unit"]]["name"].lower()
            name = name if name.endswith("forces") else name + "s"
            when = f"lands at {int(c['drop_at_distance'] * 100)}% distance" if "drop_at_distance" in c else f"dismounts at {int(c['dismount_at_hp'] * 100)}% HP"
            special.append(f"carries {c['count']} {name}, {when}")
        if u.get("blast"):
            special.append(f"explodes {u['blast']['radius_m']} m")
        if u.get("wreck"):
            special.append("wreck blocks land units")
        if u.get("requires_detection"):
            special.append(f"needs {u['requires_detection']} to target")
        if u.get("spotting"):
            special.append(f"spots {u['spotting']['radius_m']} m, calls FPV")
        if u.get("requires"):
            special.append(f"needs a {rules['units'][u['requires']]['name'].lower()}")
        if u.get("behaviour") == "fly_to_rear":
            special.append("ignores defenders, flies to the rear")
        tb = u["targetable_by"]
        if tb != ["ground"]:
            special.append("hit by " + ", ".join(t.replace("_", " ") for t in tb))
        rows.append(f"| {u['name']} | {u['type']} | {u['hp']} | {u['armour']} | {dmg} | {acc} | {rng} | {u['speed']} m/s | {lag} | {ld} | {'; '.join(special)} |")
    return "\n".join(rows)


def main():
    rules = json.loads(RULES.read_text(encoding="utf-8"))
    errors = check(rules)
    if errors:
        print("\n".join(errors))
        sys.exit(1)
    print(f"OK: {len(rules['units'])} attacker units, no problems found.", file=sys.stderr)
    if "--table" in sys.argv:
        print(table(rules))


if __name__ == "__main__":
    main()
