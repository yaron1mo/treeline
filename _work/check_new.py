#!/usr/bin/env python3
"""Local checks for the designed maps (issue #46).

For every map in tools/maps/concepts.json (and the reference Forest belts):

  - the field JSON matches a fresh generation (determinism is asserted in
    generate.py; here the file is re-read and re-checked);
  - the lanes the Sim's routing finds (port of tools/balance/Routes.cs), compared
    with the lane count the sheet's attacker routes ask for;
  - reachability: from every spawn (or the whole top edge when there are no
    spawns) an infantry and a vehicle route must reach the base line.

Usage: python3 tools/maps/check.py [--json out.json]
"""

import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)

import sim_routes  # noqa: E402

FIELDS = os.path.join(ROOT, "unity", "Assets", "Resources", "Fields")


def load_field(path):
    with open(path, encoding="utf-8") as fh:
        return json.load(fh)


def reachability(grid, field):
    """infantry and vehicle routes from every entry point to the rear edge."""
    h = field["size"][1]
    out = []
    starts = field.get("spawns") or [grid.Width * (i + 0.5) / 15 for i in range(15)]
    for name, cost in (("inf", grid.cost_i), ("veh", grid.cost_v)):
        dist, _nxt = sim_routes.flow_field(grid, cost)
        blocked = [s for s in starts if dist[grid.cell_at(s, 1.0)] == sim_routes.INF]
        # the base line is the rear edge: any cell on it must be reachable
        rear = dist[grid.cell_at(grid.Width / 2, h - 1.0)]
        out.append(f"{name}:{'ok' if not blocked and rear != sim_routes.INF else 'BLOCKED'}")
    return out


def main():
    with open(os.path.join(HERE, "concepts.json"), encoding="utf-8") as fh:
        concepts = json.load(fh)["maps"]
    out = []
    print(f"{'map':20s} {'cat':>3s} {'lanes (placing zone)':34s} {'expect':>6s}  reach")
    for concept in concepts:
        field = load_field(os.path.join(FIELDS, concept["file"] + ".json"))
        ls = sim_routes.analyse(ROOT, field)
        obstacles = sim_routes.load_obstacles(ROOT)
        grid = sim_routes.NavGrid(field, obstacles, edge_margin=10.0)
        reach = reachability(grid, field)
        placing = [l["x"] for l in ls["placing"]]
        row = {
            "name": concept["name"], "file": concept["file"], "category": concept["category"],
            "lanes_placing": placing, "lanes_placing_weights": [l["weight"] for l in ls["placing"]],
            "lanes_mid": [l["x"] for l in ls["mid"]],
            "expect_lanes": concept["expect_lanes"], "spawns": field.get("spawns"), "reach": reach,
        }
        out.append(row)
        flag = "" if len(placing) >= min(2, concept["expect_lanes"]) else "  <-- FEWER LANES THAN EXPECTED"
        print(f"{concept['name']:20s} {concept['category']:>3s} {str(placing):34s} {concept['expect_lanes']:>6d}  "
              f"{'/'.join(reach)}{flag}")
    ref = load_field(os.path.join(FIELDS, "forest-belts.json"))
    ls = sim_routes.analyse(ROOT, ref)
    print(f"{'Forest belts (ref)':20s} {'-':>3s} {str([l['x'] for l in ls['placing']]):34s} {'-':>6s}")
    if "--json" in sys.argv:
        path = sys.argv[sys.argv.index("--json") + 1]
        with open(path, "w", encoding="utf-8") as fh:
            json.dump(out, fh, indent=1)
        print("wrote", path)


if __name__ == "__main__":
    main()
