# Bot terrain: the scripted player uses houses and hills (issue #49)

2026-11-28, Mistral. Branch `mistral/bot-terrain` from `codex/playable-steppe-patch-integration`.

## What changed

The scripted players in `tools/balance` did not know the houses (Sim 2026-11-27: one defender per house
with `house_inside` cover, `Battle.HouseAt` / `HouseOccupant` / `VisionMap.Houses`) or the on-top bonus of
hills (`obstacles.csv` `hill`: `on_range_mult` 1.15, `on_sight_mult` 1.3, `on_cover_mult` 0.7).

- `tools/balance/Routes.cs` — new `Routes.TerrainSpots(...)`: candidate spots for a defender of a given
  type, scored, best first. `Routes.Lane` now also keeps the whole traced route (`All`, every third cell),
  so "near the route" and "sees the route" can be measured anywhere on the route, not only in the placing
  band. Houses inside the placing band and within 60 m of a route score high for short-range (infantry)
  types: `3 + 0.5 * (1 - dist/60)`. Hill tops (obstacles with an on-bonus) inside the band that see the
  route (own sight with the on-sight bonus, `SeeChance >= 0.25` to some route point) score high (3) for
  long-range types (`range_m >= 200`: sniper, mortar, anti-tank, air defence) and recon; all other
  combinations score 0.8, below a plain lane point (`LaneScore` 1). Sort is stable (feature order as
  tie-break), so the same field and type always give the same list: deterministic, same seed -> same
  result.
- `tools/balance/WaveCost.cs` (`Put`) and `tools/balance/PlaytestRun.cs` (`Place`) — before the random lane
  points, the terrain spots with a score above `LaneScore` are tried in order; taken houses are skipped
  (`HouseOccupant`). If none fits, the old lane-point loop runs unchanged.
- `unity/Assets/Tests/EditMode/BotTerrainTests.cs` (new) — a small field with one house next to the route:
  the scripted player puts a rifle squad in it (`TryPlace` on the best spot, `d.House == 0`); long-range
  types and recon do not prefer the house; a hill top that sees the route is taken by sniper / anti-tank /
  recon (and the on-sight bonus is there) but not by infantry; the spot list is deterministic. The test
  compiles `tools/balance/Routes.cs` at run time (the tools are not part of the Sim assembly) and drives it
  through the Sim exactly like `WaveCost.Put` does.

Only spots inside the placing band (60%-90% of the height in WaveCost, 55%-90% in PlaytestRun) count.
First version allowed houses and hills anywhere on the field; that made River crossing much worse
(+105% sum): its hill (centre 165,84) is on the enemy side of the river, and defenders placed there are
seen from the spawn edge and shot before they can help (they were all dead at the end of the wave). The
players defend from the band, so terrain on the enemy side is not a spot for them.

No change in `unity/Assets/Scripts/Sim/**`.

## How the numbers were measured

`WaveCost` as in CI (same compile line: Sim + `Routes.cs` + `WaveCost.cs`), run per field:
`wave-cost <repo> tools/balance/stop.csv "<field filter>" 1 20` — 10 seeds, held on 8 of 10, budget step
25, search limit 12000, allowance `line_health / 20` = 3. "Before" = the branch point (`8a894dc`),
"after" = this branch. "Not held" = not held even at 12000 (shown as 12000 in the sums). Local build with
the .NET SDK (the runner has no Mono); behaviour identical to the Mono build (same sources).

PlaytestRun (saver, 10 seeds per field, all fields): waves held per run, before vs after.

## Wave cost, waves 1-20, before -> after

### Forest belts

| wave | before | after | change |
| --- | --- | --- | --- |
| 1 | 200 | 200 | +0.0% |
| 2 | 300 | 300 | +0.0% |
| 3 | 300 | 300 | +0.0% |
| 4 | 200 | 200 | +0.0% |
| 5 | 500 | 500 | +0.0% |
| 6 | 300 | 300 | +0.0% |
| 7 | 2325 | 2325 | +0.0% |
| 8 | 2125 | 2125 | +0.0% |
| 9 | 1225 | 1225 | +0.0% |
| 10 | 1525 | 1525 | +0.0% |
| 11 | 2275 | 2275 | +0.0% |
| 12 | 3025 | 3025 | +0.0% |
| 13 | 2175 | 2175 | +0.0% |
| 14 | 2975 | 2975 | +0.0% |
| 15 | 4375 | 4375 | +0.0% |
| 16 | 5375 | 5375 | +0.0% |
| 17 | 4475 | 4475 | +0.0% |
| 18 | 6325 | 6325 | +0.0% |
| 19 | 9025 | 9025 | +0.0% |
| 20 | 6025 | 6025 | +0.0% |
| **sum 1-20** | **55050** | **55050** | **+0.0%** |

### Village street

| wave | before | after | change |
| --- | --- | --- | --- |
| 1 | 200 | 100 | -50.0% |
| 2 | 200 | 100 | -50.0% |
| 3 | 200 | 100 | -50.0% |
| 4 | 200 | 100 | -50.0% |
| 5 | 200 | 200 | +0.0% |
| 6 | 200 | 100 | -50.0% |
| 7 | 1475 | 1225 | -16.9% |
| 8 | 1475 | 1275 | -13.6% |
| 9 | 1225 | 725 | -40.8% |
| 10 | 1225 | 725 | -40.8% |
| 11 | 2675 | 1325 | -50.5% |
| 12 | 3275 | 1375 | -58.0% |
| 13 | 2175 | 1025 | -52.9% |
| 14 | not held | not held | +0.0% |
| 15 | 4125 | 3425 | -17.0% |
| 16 | 3425 | 1975 | -42.3% |
| 17 | 4275 | 3125 | -26.9% |
| 18 | 6125 | 4575 | -25.3% |
| 19 | 7975 | 4575 | -42.6% |
| 20 | 7375 | 4975 | -32.5% |
| **sum 1-20** | **60025** | **43025** | **-28.3%** |

### Crossroads

| wave | before | after | change |
| --- | --- | --- | --- |
| 1 | 200 | 100 | -50.0% |
| 2 | 200 | 100 | -50.0% |
| 3 | 100 | 100 | +0.0% |
| 4 | 200 | 100 | -50.0% |
| 5 | 300 | 300 | +0.0% |
| 6 | 200 | 100 | -50.0% |
| 7 | 1375 | 1375 | +0.0% |
| 8 | 1275 | 1175 | -7.8% |
| 9 | 1225 | 725 | -40.8% |
| 10 | 1425 | 825 | -42.1% |
| 11 | 2875 | 2175 | -24.3% |
| 12 | 3075 | 2075 | -32.5% |
| 13 | 2575 | 1975 | -23.3% |
| 14 | not held | not held | +0.0% |
| 15 | 4575 | 3025 | -33.9% |
| 16 | 3925 | 2875 | -26.8% |
| 17 | 4775 | 2925 | -38.7% |
| 18 | 8375 | 5425 | -35.2% |
| 19 | 8725 | 6025 | -30.9% |
| 20 | 9125 | 3725 | -59.2% |
| **sum 1-20** | **66525** | **47125** | **-29.2%** |

### River crossing

| wave | before | after | change |
| --- | --- | --- | --- |
| 1 | 100 | 100 | +0.0% |
| 2 | 200 | 200 | +0.0% |
| 3 | 200 | 200 | +0.0% |
| 4 | 200 | 200 | +0.0% |
| 5 | 400 | 400 | +0.0% |
| 6 | 200 | 200 | +0.0% |
| 7 | 1475 | 1475 | +0.0% |
| 8 | 1475 | 1475 | +0.0% |
| 9 | 1025 | 1025 | +0.0% |
| 10 | 2025 | 2025 | +0.0% |
| 11 | 4625 | 4625 | +0.0% |
| 12 | 3175 | 3175 | +0.0% |
| 13 | 3325 | 3325 | +0.0% |
| 14 | 2975 | 2975 | +0.0% |
| 15 | 5375 | 5375 | +0.0% |
| 16 | 10175 | 10175 | +0.0% |
| 17 | 6175 | 6175 | +0.0% |
| 18 | 9975 | 9975 | +0.0% |
| 19 | not held | not held | +0.0% |
| 20 | not held | not held | +0.0% |
| **sum 1-20** | **77100** | **77100** | **+0.0%** |

## PlaytestRun (saver style, 10 seeds): waves held, mean

| field | before (seeds) | after (seeds) |
| --- | --- | --- |
| Forest belts | 8.9 (8,8,9,9,9,9,9,9,9,10) | 8.9 (identical) |
| Village street | 8.7 (7,8,8,8,9,9,9,9,10,10) | 9.0 (9,9,9,9,9,9,9,9,9,9) |
| Crossroads | 8.7 (8,8,8,9,9,9,9,9,9,9) | 9.1 (9,9,9,9,9,9,9,9,9,10) |
| River crossing | 10.4 | 10.4 (identical) |
| Balka and railway | 9.6 | 9.6 (identical) |

## What the player now does

On the village maps the machine gunners and rifle squads go into the houses beside the road (the route),
one defender per house, best house first; long-range types (anti-tank, sniper, mortar) and the recon drone
stay on the lane points because a house scores below a lane point for them. Example, Village street, wave 12,
budget 12000, seed 1 (defender positions at the end): `machine_gun@113,246 machine_gun@114,262
machine_gun@114,302 machine_gun@87,254 machine_gun@87,270 machine_gun@86,294 machine_gun@86,310 ...`
— the two house columns next to the road at x~86 and x~114, filled from the route outward; the
anti-tanks hold the lane points (`anti_tank@109,337 anti_tank@99,353 ...`).

On River crossing the hill top (165,84) is outside the placing band (240-360), so it is not a candidate
and the map plays exactly as before. Forest belts has no houses and no hills in the band, so its cost is
unchanged wave by wave (+0.0%, requirement was at most 5%).

## Tests

`unity/Assets/Tests/EditMode/BotTerrainTests.cs` (new file + .meta). Run on this machine with
`tools/unity/unity.ps1 -Tests EditMode`: 97 tests, 0 failed (93 existing + 4 new). CI runs the same tests
with Mono via `tools/tests/run_editmode.sh`; the test file compiles there too (CodeDom provider of Mono).

## Open questions

- The hill on River crossing is on the enemy side of the river and is not usable by the scripted player
  (outside its placing band). A human player can still place there (`placement_zone_start` = 0). If the
  bot should also use enemy-side hill tops, the band rule in `Routes.TerrainSpots` must change — with the
  current one-defender-per-spot behaviour that cost +105% on River crossing, so it needs a survival rule,
  not just a wider band.
- Wave 14 on both village maps is not held even at 12000, before and after.
