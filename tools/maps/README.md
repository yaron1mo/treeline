# Designed map generator (issue #46, group A)

Rule-based generator for the 12 designed maps of `docs/design/maps.md` section 5.1
(branch `design/maps`), on the Forest belts grammar. Real data is not used: every
element has a game reason from the design.

| File | What it does |
| --- | --- |
| `grammar.py` | The elements and rules of the design's "map grammar" section as parameters: belt count and spacing, gaps per belt and their shift, pocket, road, tracks through gaps, groves, pond, craters, field patchwork and tones. Same seed + same parameters -> same map. |
| `concepts.json` | Maps 1-12 of the design as parameter sets (name, idea, parameters). |
| `generate.py` | Writes `unity/Assets/Resources/Fields/sector-<name>.json` (+ `.meta`) in the Forest belts field format, with spawns. Validates every gap stays open and the file is reproducible. |
| `check.py` | Local checks: lanes the Sim's routing finds (port of `tools/balance/Routes.cs` in `sim_routes.py`), gap shift, spawns, hold points. |
| `preview.py` | One PNG per map and a contact sheet (Forest belts first) in `docs/design/visual/maps/` (not in LFS). |
| `sim_routes.py` | Python port of NavGrid + FlowField + Routes from the Sim, used by `check.py`. |

Usage:

```
python3 tools/maps/generate.py          # write the 12 sector-*.json + .meta
python3 tools/maps/generate.py --check-only
python3 tools/maps/check.py --json /tmp/lanes.json
python3 tools/maps/preview.py           # PNGs into docs/design/visual/maps/
```

Balance check (issue step 4): push the branch; the Balance run measures every field
with the current waves (max budget 12000). A map passes when no wave is "not held"
and the cost per wave is 0.6-1.6 x Forest belts. `tools/balance/run.json` on the
branch can focus the run on the new maps (deleted before the PR).
