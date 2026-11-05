# Units table

Source of truth for unit balance: Google Sheet **Treeline — Units table**
https://docs.google.com/spreadsheets/d/1xkO3OgYYPyFnTIRYXtRGm7PYSxfLa0NGlNVfll9NcdQ/edit

Tabs: `README`, `units` (one row per unit, side/type/subtype as columns, no wave column), `armour`, `cover`, `vision`.

Workflow: edit in the sheet, export each tab as CSV into `unity/Assets/Data/` (`units.csv`, `armour.csv`, `cover.csv`), log the change in `docs/log/CHANGELOG.md`. The CSVs in the repo are snapshots of the sheet; `rules/attackers.json` is the older format, kept until the Unity loader replaces it.

## Vision (decided 2026-11-05, Yaroslav)

- Attackers have a separate sight radius, `sight_range_m`, distinct from `range_m` (attack range). Base is 30 m and it rises with unit level. Current values: recruit 25, regular 30, special forces 40 (proposal), vehicles 30, recon UAV 40 (from its notes); the rest are empty.
- Obstacles cut the sight circle into sectors, like pizza slices. A `block` obstacle (rock, wreck, heap, relief) hides everything behind it inside its shadow sector. A `soft` obstacle (bush, grove, forest belt) does not hide it, it blurs it: the chance to recognise something starts at `detect_chance_at_obstacle` right behind the obstacle and drops by `detect_falloff_per_m` with distance. Inside a belt or grove the unit also sees blurred.
- Obstacles live in the `vision` tab (`unity/Assets/Data/vision.csv`). Its numbers are my proposals, to be balanced.
- Shared vision: all attackers share what any attacker sees (no delay yet).
- Open: how firing flash and trenches affect being seen, night effects, defenders are not in the table yet.
