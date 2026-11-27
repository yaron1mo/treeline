# Designed map generator (issue #46)

Rule-based generator for the 20 designed maps of the brief of 2026-11-27 (replaces
group A), in 4 categories: A river, B hills and slopes, C forest-heavy, D open steppe.
Each map has its own main idea; at most one map per category may be "parallel belts
across the field" (none is). Real data is not used: every element has a game reason
from the design.

| File | What it does |

| --- | --- |

| `grammar.py` | The elements of `docs/design/maps.md` section 4.1 (branch `design/maps`) as parameters: belts with gaps, pocket, fork, road, tracks; groves (also scattered); pond, craters, field tones; rivers (edge to edge), fords, pipes, ruined bridges; hills, ridges (a row of hills with saddles), slopes; balkas, embankments; rocks, wrecks (also a wreck column), bushes, hedgehogs, heaps; trench lines, wire; a sunflower field (bushes until the Sim has a crop rule); a burned strip (a normal belt until the Sim has a burned-belt rule). Same seed + same parameters -> same map. |

| `concepts.json` | The 20 maps as parameter sets (name, category, idea, parameters). |

| `generate.py` | Writes `unity/Assets/Resources/Fields/<file>.json` (+ `.meta`) in the Forest belts field format, with spawns. Validates every gap stays open and the file is reproducible. |

| `check.py` | Local checks: lanes the Sim's routing finds (port of `tools/balance/Routes.cs` in `sim_routes.py`), gap shift, spawns, hold points, and that infantry and vehicles can reach the base from every spawn. |

| `preview.py` | One PNG per map and a contact sheet (Forest belts first, then the maps grouped A-D) in `docs/design/visual/maps/` (not in LFS). |

| `sim_routes.py` | Python port of NavGrid + FlowField + Routes from the Sim, used by `check.py`. |

Usage:

```
python3 tools/maps/generate.py          # write the 20 field JSONs + .meta
python3 tools/maps/generate.py --check-only
python3 tools/maps/check.py --json /tmp/lanes.json
python3 tools/maps/preview.py           # PNGs into docs/design/visual/maps/
```

Balance check (issue step 3): push the branch; the Balance run measures every field
in `tools/balance/run.json` with the current waves (max budget 12000). A map passes
when no wave is "not held" and the cost per wave is 0.6-1.6 x Forest belts.
`tools/balance/run.json` on the branch focuses the run on the new maps (deleted
before the PR).
