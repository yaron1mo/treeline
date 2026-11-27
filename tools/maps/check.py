#!/usr/bin/env python3
"""Local checks for the designed sector maps (issue #46).

For every map in tools/maps/concepts.json (and the reference Forest belts):
  - the generated JSON is valid and the spawns are inside the open width;
  - every designed gap stays open (checked in generate.py);
  - the gap shift between belt rows (design rule: >= 50 m for 1-gap belts);
  - the lanes the Sim's routing finds (port of tools/balance/Routes.cs), compared
    with the lane count the design asks for;
  - hold points: groves + pond + pocket count (design: 3-5 places with cover).

Usage: python3 tools/maps/check.py [--json out.json]
"""

import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)

import grammar  # noqa: E402
import sim_routes  # noqa: E402

FIELDS = os.path.join(ROOT, "unity", "Assets", "Resources", "Fields")


def load_field(path):
    with open(path, encoding="utf-8") as fh:
        return json.load(fh)


def main():
    with open(os.path.join(HERE, "concepts.json"), encoding="utf-8") as fh:
        concepts = json.load(fh)["maps"]
    out = []
    print(f"{'map':24s} {'lanes':>10s} {'expect':>6s} {'gap shift min':>13s} {'hold pts':>8s}  spawns")
    for concept in concepts:
        field = load_field(os.path.join(FIELDS, concept["file"] + ".json"))
        gen = grammar.generate(concept)
        lay = gen["_layout"]
        # gap shift between consecutive rows (distance from every gap to the nearest above)
        shifts = []
        for i in range(1, len(lay["gaps"])):
            for g in lay["gaps"][i]:
                shifts.append(min(abs(g - p) for p in lay["gaps"][i - 1]))
        min_shift = min(shifts) if shifts else None
        ls = sim_routes.analyse(ROOT, field)
        # design lanes: the band between the first and the last belt row
        grid = sim_routes.NavGrid(field, sim_routes.load_obstacles(ROOT), edge_margin=10.0)
        h = field["size"][1]
        design = sim_routes.lanes(grid, lay["belt_ys"][0] + 10, lay["belt_ys"][-1] - 10)
        design_lanes = [{"x": round(l["x"], 1), "weight": l["weight"]} for l in design]
        hold = sum(1 for f in field["features"] if f["type"] in ("grove", "pond"))
        if concept.get("pocket"):
            hold += 1
        row = {
            "name": concept["name"], "file": concept["file"],
            "lanes_design": [l["x"] for l in design_lanes], "lanes_design_weights": [l["weight"] for l in design_lanes],
            "lanes_placing": [l["x"] for l in ls["placing"]],
            "expect_lanes": concept["expect_lanes"], "min_gap_shift": round(min_shift, 1) if min_shift else None,
            "hold_points": hold, "spawns": field["spawns"],
            "belts": lay["belt_ys"], "gaps": lay["gaps"],
        }
        out.append(row)
        print(f"{concept['name']:24s} design={str([l['x'] for l in design_lanes]):22s} "
              f"zone={str([l['x'] for l in ls['placing']]):16s} expect={concept['expect_lanes']} "
              f"shift={str(round(min_shift,1)) if min_shift else '-':>6s} hold={hold}  spawns={field['spawns']}")
    # reference map, for comparison
    ref = load_field(os.path.join(FIELDS, "forest-belts.json"))
    ls = sim_routes.analyse(ROOT, ref)
    print(f"{'Forest belts (ref)':24s} design={str([l['x'] for l in ls['mid']]):22s} "
          f"zone={str([l['x'] for l in ls['placing']]):16s} expect=- "
          f"shift=-    hold={sum(1 for f in ref['features'] if f['type'] in ('grove','pond'))}  spawns={ref.get('spawns')}")
    if "--json" in sys.argv:
        path = sys.argv[sys.argv.index("--json") + 1]
        with open(path, "w", encoding="utf-8") as fh:
            json.dump(out, fh, indent=1)
        print("wrote", path)


if __name__ == "__main__":
    main()
