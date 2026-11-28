# Designed map generator (issue #46)

Builds the field files of the approved concept sheets of `docs/design/maps/*.md`
(PR #51, Yaroslav's decisions of 2026-11-28): 22 maps in 5 categories — A river,
B hills and slopes, C forest-heavy, D open steppe, V village. Each sheet has a
20 x 40 grid (one character = 10 m, row 1 = enemy edge, row 40 = our line); the
generator converts the grid straight into the field JSON, so a field file and
its sheet cannot drift apart. Same sheet + same seed -> same field file.

| File | What it does |

| --- | --- |

| `grammar.py` | The sheet-grid converter: every legend char of `docs/design/maps/README.md` becomes a feature the Sim knows (terrain polygons: hill, grove, belt, pond, river, balka, embankment, heap, pipe, ford, road; objects: rock, wreck, bush, trench, wire, hedgehog, house, ruin, fence, sunflower, burned_belt, stack, shed, silo, camo_net, dugout; lines: belt/river/road with an exact poly plus the renderer's centre line, tracks, bridge_ruins; craters as points; field tones for the background). Sim fixes that keep the sheet playable, as parameters: digit markers keep the terrain they stand on; `heal_roads` lets a road cut through what lies on its own corridor (the Sim exempts road cells from the vehicle clearance rule); `span_fords` makes a ford span its river with a cleared apron; `bank_roads` joins road and ford along the bank so vehicles can leave the ford pocket. The older parameter-based generator (belts with gaps, pockets, forks, rivers, hills...) is still there for concepts without a "sheet" key. |

| `concepts.json` | The 22 maps: file, name, category, sheet path, seed, sector, subtitle, expected lanes. |

| `generate.py` | Writes `unity/Assets/Resources/Fields/<file>.json` (+ `.meta`) in the Forest belts field format, with spawns where the sheet's roads meet the top edge. Validates every sheet cell becomes a feature and the file is reproducible. |

| `check.py` | Local checks: lanes the Sim's routing finds (port of `tools/balance/Routes.cs` in `sim_routes.py`), and that infantry, transports and armour (tree_move_cost 5) can reach the base from the entries. |

| `preview.py` | One PNG per map and a contact sheet (Forest belts first, then the maps grouped A-V) in `docs/design/visual/maps/` (not in LFS). |

| `sim_routes.py` | Python port of NavGrid + FlowField + Routes from the Sim, used by `check.py`. |

Usage:

```
python3 tools/maps/generate.py          # write the 22 field JSONs + .meta
python3 tools/maps/generate.py --check-only
python3 tools/maps/check.py --json /tmp/lanes.json
python3 tools/maps/preview.py           # PNGs into docs/design/visual/maps/
```

Balance check (issue step 3): push the branch; the Balance run measures every field
in `tools/balance/run.json` with the current waves (max budget 12000). A map passes
when no wave is "not held" and the cost per wave is 0.6-1.6 x Forest belts.
`tools/balance/run.json` on the branch focuses the run on the new maps (deleted
before the PR).
