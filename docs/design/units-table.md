# Units table

Source of truth for unit balance: Google Sheet **Treeline — Units table**
https://docs.google.com/spreadsheets/d/1xkO3OgYYPyFnTIRYXtRGm7PYSxfLa0NGlNVfll9NcdQ/edit

Tabs: `README`, `units` (one row per unit, side/type/subtype as columns, no wave column), `armour`, `obstacles` (every obstacle: sight, cover, move cost), `battle_rules` (battle-wide numbers: line health, route weights, memory).

Workflow: edit in the sheet, export each tab as CSV into `unity/Assets/Data/` (`units.csv`, `armour.csv`, `obstacles.csv`, `battle_rules.csv`), log the change in `docs/log/CHANGELOG.md`. The CSVs in the repo are snapshots of the sheet; `rules/attackers.json` is the older format, kept until the Unity loader replaces it.

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

## Ground vehicles (2026-11-06)

- Carriers (`ifv`, `mrap`, `van`): drive with `carries_count` troops of `carries_unit` inside. When hit points fall to `dismount_at_hp` of the maximum the carrier stops and lets one out every `dismount_interval_s`, then drives on empty. Troops still inside die with the vehicle. Reaching the rear edge loaded, each passenger adds its own `line_damage` (a van with 12 recruits = 2 + 12).
- `blast_radius_m` on a carrier: destroyed while loaded or unloading, it kills attacking troops within that radius (rule `dismount_blast_kills`, a proposal). On the mortar (`behaviour = indirect_fire`) it is the splash of each shot.
- `wreck_radius_m`: a destroyed vehicle leaves a `wreck` obstacle of that radius: it hides, gives cover and blocks vehicles (infantry 3, from the obstacles table). Routes are rebuilt when it appears.
- `tree_move_cost`: a vehicle with it uses that cost in groves and forest belts (tanks push through groves, which are closed to other vehicles).
- Move costs now come from the `obstacles` tab for terrain and objects (rock blocked, wreck 3 / blocked, bush 1.5 / 1, wire 6 / 2, hedgehog 1.5 / blocked).

## Routes and memory (2026-11-06)

From `rules/attackers.json`, numbers in `battle_rules`:
- Waves are numbered. Attackers remember where defenders were seen or fired from. A unit type with `adaptation_lag_waves` = L in wave W knows what was learnt up to wave W - L, fading by `memory_decay_per_wave` per wave without a new sighting.
- Threat zone (changed): where a known defender can hit this unit: inside its range **and** its own sight (belts and bushes count); for the mortar, inside its range and seen by any known defender. Weapons that do no damage after armour (rifles against tanks) are not a threat.
- Fastest when nothing is known; Evade when a way exists around every threat zone (travel time + 0.1 s per metre); Safest otherwise (1 point of expected damage = 1000 m). With threats known attackers enter where the route is best (within 10%).

## Firing rule (open question, 2026-11-06)

`battle_rules.direct_fire_needs_own_sight`. FALSE (current): anyone may fire at what any friendly unit sees, out to `range_m`. Because `range_m` (200 to 1000 m) is larger than the maps (200 x 400 m), one spotter lets the whole enemy column fire. TRUE: direct fire only at what the shooter itself sees; shared vision only for the mortar. See `docs/balance/2026-11-06/`.
