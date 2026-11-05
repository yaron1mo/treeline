# Units table

Source of truth for unit balance: Google Sheet **Treeline — Units table**
https://docs.google.com/spreadsheets/d/1xkO3OgYYPyFnTIRYXtRGm7PYSxfLa0NGlNVfll9NcdQ/edit

Tabs: `README`, `units` (one row per unit, side/type/subtype as columns, no wave column), `armour`, `obstacles` (every obstacle: sight, cover, move cost).

Workflow: edit in the sheet, export each tab as CSV into `unity/Assets/Data/` (`units.csv`, `armour.csv`, `obstacles.csv`), log the change in `docs/log/CHANGELOG.md`. The CSVs in the repo are snapshots of the sheet; `rules/attackers.json` is the older format, kept until the Unity loader replaces it.

## Vision and obstacles (decided 2026-11-05, Yaroslav)

- Attackers have a separate sight radius, `sight_range_m`, distinct from `range_m` (attack range). Base is 30 m and it rises with unit level. Current values: recruit 25, regular 30, special forces 40 (proposal), vehicles 30, recon UAV 40; the rest are empty.
- All obstacles live in the `obstacles` tab (`unity/Assets/Data/obstacles.csv`). It replaces the old `cover` and `vision` tabs and takes over the terrain move costs that were hard-coded in `NavGrid.cs` (code still uses its own numbers until the loader is written).
- Obstacles are not destructible.
- Sight: a `full` obstacle (rock, broken vehicle, bush, heap, embankment, hill) hides everything behind it inside its shadow sector, like a slice cut from the sight circle. A `soft` obstacle (grove, forest belt) blurs it: the chance to recognise something starts at `detect_chance` where the ray enters or leaves the obstacle and drops by `detect_falloff_per_m` with distance. A unit standing inside a belt sees clearly past the edge of it, so its sight circle is an hourglass: short along the belt, full towards the open ground.
- Cover: it works for both sides, so mortars sit behind rocks and wrecks. A bush hides but gives no protection. Hill: behind it works like a rock; on top it gives more range (`on_range_mult`), more sight (`on_sight_mult`) and less accurate enemy direct fire (`on_cover_mult`).
- Shared vision: all attackers share what any attacker sees (no delay yet).
- Obstacle numbers are proposals, except those moved from the old `cover` rows and the code move costs.
- Open: how firing flash affects being seen, night effects, defenders are not in the table yet.

## Defenders (added 2026-11-05)

Six proposal rows with `side = defence`: rifle_squad, machine_gun, sniper, mortar, anti_tank, air_defence. They use the same columns as attackers plus `weapon_class` (small_arms, machine_gun, sniper, explosive, anti_tank, air_defence) and `build_cost`. All numbers are proposals to balance. Mortars fire at what any defender sees.
