# FPV active-shot lifecycle, 3s cooldown, radial explosions — results (2026-11-13)

## Implemented and tested
- Operator has 20m own sight; airborne FPV uses separate 20m onboard sight and the player-commanded waypoint.
- While airborne, `Defender.DroneState == InFlight`; after FPV impact, forced shootdown or loss, `Reloading` for 3 seconds; `Ready` thereafter. Only one active drone at once. `Battle.TryDestroyDrone(droneId)` simulates destruction and exposes an interception integration hook; no hostile aircraft interception is implemented yet.
- FPV flight speed 35m/s (30 × 1.2 = 36, rounded to nearest 5).
- Explosive damage is concentrated at blast centre and falls linearly to zero at radius. FPV 15m, mortar 30m, ATGM 5m. Bomber 3m unchanged.
- One accuracy check per mortar / ATGM shot, then spatial damage/armour for every victim. FPV currently scales damage by accuracy deterministically (0.9), and armour reduces each victim.
- Unified `damage` (FPV operator 70, bomber operator 25) and `attack_speed` (shots/s): 1/3 FPV, 1/7 ATGM, 1/20 bomber. Removed redundant `hit_damage`, `reload_s`, and `launch_delay_s` columns; launch is now instant when ready and mission exists, but drone flight still consumes time.
- Synced live Sheet edits including mortar accuracy .4, ATGM 80 raw damage/85% accuracy/800m range, ATGM unarmoured, and AA gun unarmoured.

## 9,120-run Monte Carlo

Successful run using current gameplay code: https://github.com/yaron1mo/treeline/actions/runs/43475797278 . 7,200 baseline cases (100 seeds × defenders / vehicles / counts / release gaps), 1,920 paired sight or damage ×2 experiments.

Single unfortified defender vs one vehicle; P(vehicle killed before crossing), 100 seeds each:

| Defender | Old tank | Modern tank | IFV | MRAP |
|---|---:|---:|---:|---:|
| ATGM | 6% | 1% | 0% | 91% |
| Mortar | 0% | 0% | 8% | 0% |
| FPV operator (20,80 waypoint) | 100% | 100% | 100% | 100% |

Mean vehicle HP damage (as percentage of the initial HP) when testing one vehicle:

| Defender | Old tank | Modern tank | IFV | MRAP |
|---|---:|---:|---:|---:|
| ATGM | 68.4% | 57.4% | 76.8% | 91.0% |
| Mortar | 19.4% | 10.8% | 62.6% | 35.9% |
| FPV operator | 100% | 100% | 100% | 100% |

**Do not infer these are tuned roles**. The FPV is now extremely strong because its drone loiters and reacquires targets autonomously, hostile fire cannot hit the drone, and rearming after each detonation takes only 3s. ATGM became comparatively weak after a user-specified 7-second cooldown. Mortar lacks damage output per armoured target, even with the much larger area effect. This experiment intentionally excludes cover, upgrades, external spotting, enemy drone countermeasures and class-target priority in mixed waves.

## Damage model examples

- FPV impact at blast centre against medium armour: `70 × .9 × .6 = 37.8 HP`; high armour `70 × .9 × .4 = 25.2 HP`. At 7.5m from the 15m centre, damage is exactly half (18.9/12.6), at 15m zero.
- ATGM: `80 × 1.0 = 80 HP` to medium, `80 × .85 = 68 HP` to high on each successful shot at the centre, then linear falloff out to 5m; 7s interval between shots.
- Mortar: `20 × .6 = 12 HP` medium and `20 × .35 = 7 HP` high on a successful central hit, then linear falloff out to 30m. Hit roll accuracy 40%.
- Attacks/sec conversion matters: `attack_speed = 1/3` = 3-second post-FPV-detonation reload, **not** 3 attacks each second. `attack_speed = 1/7` = 7-second ATGM fire interval.

These values are game simulation parameters, not estimates of actual military weapon performance.
