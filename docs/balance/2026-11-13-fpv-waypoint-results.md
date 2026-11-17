# FPV waypoint + 100 HP armour check (2026-11-13)

## Implemented
- Source: Treeline — Units table, `units` tab. Old tank, modern tank and IFV HP 300→100; MRAP remains 60 HP.
- Operator and FPV drone sight each **20m**. New `drone_sight_range_m` column stores the onboard radius independently of `sight_range_m` for the operator.
- Pilot command: `Battle.TrySetDroneWaypoint(operatorId, x, y, out why)`, bounded by operator `range_m` (600m) and the field. Single-use FPV launches without a pre-spotted enemy, travels to selected point, actively searches locally, waits for unseen incoming targets and pursues targets it itself acquires. A drone contributes its own local sight to shared vision. UI must expose waypoint setting in future; compatibility with old no-waypoint automatic targeting retained.
- The user's raw FPV damage 35 was entered in the `damage` column, which does **not** affect drone impacts in the current engine. The value has been moved to `fpv_operator.hit_damage=35` and `damage` cleared to avoid contradiction. Drones still use the current deterministic accuracy×armour multiplier; physical warhead hit physics are not implemented.
- During live Sheet sync, pre-existing Sheet edits were also picked up: mortar damage 15→20 and sight range 30→40, anti-tank sight range 30→40. No further numeric tuning was invented.

## Anti-armour retest

Successful CI run https://github.com/yaron1mo/treeline/actions/runs/43470510155 . Headless FPV mission smoke check passed; 7,200 Monte Carlo baseline and 1,920 paired variations also passed. One standalone defender vs one vehicle in a 40×300m field without cover, 100 seeds.

| Defender | Old tank: vehicle stopped % | Modern tank stopped % | IFV stopped % | MRAP stopped % |
|---|---:|---:|---:|---:|
| anti_tank | 99 | 90 | 85 | 99 |
| fpv_operator (waypoint 20,80) | 0 | 0 | 0 | 0 |
| mortar | 0 | 0 | 3 | 0 |

Mean percentage of starting vehicle HP damaged before resolution:

| Defender | Old tank | Modern tank | IFV | MRAP |
|---|---:|---:|---:|---:|
| anti_tank | 99.8 | 95.4 | 97.0 | 99.6 |
| fpv_operator | 39.1 | 22.4 | 33.6 | 18.7 |
| mortar | 15.0 | 8.1 | 47.6 | 26.8 |

**Caveat:** for the IFV, `vehicle_stopped_pct` differs from `line_held_pct` due to surviving dismounted infantry. E.g., anti_tank vs one IFV: 85% vehicle stops but 71% no-line-damage. FPV waypoint target acquisition can work yet not destroy the vehicle; damage and kills are separate.

## Damage maths before future tuning
- Current FPV impact: 35 × accuracy 0.8 × armour-factor .6 (medium) = **16.8 effective HP**; versus high .4 = **11.2 HP**. Thus, even at 100 HP, one FPV drone cannot destroy a tank/IFV; 6 medium or 9 high impacts are needed absent counterfire/repair. Against low/no armour one FPV impact causes 28 HP, sufficient for base infantry.
- Anti-tank: 40 raw vs medium, 34 vs high armour; at 100 HP, **three successful hits** suffice for medium or high. Its observed improvements are also driven by sight range 40m and behaviour of vehicle counterfire.
- Mortar: 20 raw damage, 0.3 per-shot hit chance; medium multiplier .6 and high .35. Blast radius 6m. Mortar/FPV are intended **dual-purpose** versus infantry and vehicles, even though their current anti-armour results remain limited.
- Detection and other bonuses (cover, upgrades, recon, combined defence) are intentionally excluded; they are future mechanisms. These are balance numbers for a game, not combat estimates.

## Next balance review
Decide expected **FPV impacts-to-kill** each 100HP armoured class, and the degree of mortar damage against armour. Do not adjust either blindly: set target combat roles and test the exact damage/HP choice using the same independent waypoint missions.
