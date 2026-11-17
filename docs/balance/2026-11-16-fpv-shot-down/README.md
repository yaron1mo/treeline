# FPV operator: the enemy can now shoot our drones (2026-11-16)

FPV operator with recon, drone speed 20 m/s, blast 3 m, reload unchanged (3 s). Attackers arrive in a group, 0.3 s apart, 50 seeds per cell.
Before = nobody shoots our drone; after = attackers shoot it by the `fpv` row's `targetable_by` (small arms, machine gun, sniper, fpv, air defence): infantry, MRAP, dismounted troops. Tanks and the IFV's gun cannot.

Cell: attackers killed per run (before → after) / operator alive % (before → after)

## trench_belt

| Attacker | 1 | 3 | 5 |
|---|---|---|---|
| buggy | 1.0 → 1.0 / 100 → 100 | 3.0 → 3.0 / 100 → 100 | 4.9 → 4.9 / 100 → 100 |
| fpv | 0.0 → 0.0 / 20 → 20 | 0.2 → 0.2 / 4 → 4 | 0.1 → 0.1 / 0 → 0 |
| helicopter | 0.0 → 0.0 / 0 → 0 | 0.0 → 0.0 / 0 → 0 | 0.0 → 0.0 / 0 → 0 |
| ifv | 1.0 → 0.2 / 32 → 4 | 2.7 → 0.2 / 2 → 2 | 2.4 → 0.1 / 0 → 0 |
| modern_tank | 1.0 → 1.0 / 100 → 100 | 2.5 → 2.5 / 86 → 86 | 1.9 → 1.9 / 66 → 66 |
| motorbike | 1.0 → 1.0 / 100 → 100 | 3.0 → 3.0 / 100 → 100 | 4.9 → 4.9 / 100 → 100 |
| mrap | 0.5 → 0.4 / 4 → 12 | 0.8 → 0.6 / 8 → 8 | 0.6 → 0.7 / 4 → 4 |
| old_tank | 1.0 → 1.0 / 100 → 100 | 3.0 → 3.0 / 100 → 100 | 4.9 → 4.9 / 98 → 98 |
| recruit | 1.0 → 1.0 / 100 → 100 | 3.0 → 3.0 / 100 → 100 | 5.0 → 4.7 / 100 → 92 |
| regular | 1.0 → 1.0 / 100 → 100 | 3.0 → 2.6 / 100 → 88 | 5.0 → 3.6 / 100 → 64 |
| special_forces | 1.0 → 1.0 / 100 → 96 | 3.0 → 1.5 / 100 → 42 | 5.0 → 1.2 / 98 → 6 |
| van | 1.0 → 1.0 / 98 → 98 | 3.0 → 2.9 / 98 → 98 | 4.7 → 4.6 / 94 → 94 |

## open

| Attacker | 1 | 3 | 5 |
|---|---|---|---|
| buggy | 1.0 → 1.0 / 100 → 100 | 3.0 → 3.0 / 100 → 100 | 4.9 → 4.9 / 100 → 100 |
| fpv | 0.0 → 0.0 / 20 → 20 | 0.2 → 0.2 / 4 → 4 | 0.1 → 0.1 / 0 → 0 |
| helicopter | 0.0 → 0.0 / 0 → 0 | 0.0 → 0.0 / 0 → 0 | 0.0 → 0.0 / 0 → 0 |
| ifv | 1.0 → 0.1 / 4 → 4 | 2.1 → 0.1 / 2 → 2 | 2.1 → 0.1 / 0 → 0 |
| modern_tank | 0.7 → 0.7 / 72 → 72 | 2.1 → 2.1 / 70 → 70 | 1.5 → 1.5 / 52 → 52 |
| motorbike | 1.0 → 1.0 / 100 → 100 | 3.0 → 3.0 / 100 → 100 | 4.9 → 4.9 / 100 → 100 |
| mrap | 0.0 → 0.1 / 4 → 12 | 0.0 → 0.1 / 4 → 6 | 0.0 → 0.0 / 6 → 4 |
| old_tank | 1.0 → 1.0 / 100 → 100 | 3.0 → 3.0 / 100 → 100 | 4.9 → 4.9 / 94 → 94 |
| recruit | 1.0 → 1.0 / 100 → 100 | 3.0 → 2.9 / 100 → 98 | 5.0 → 4.1 / 100 → 78 |
| regular | 1.0 → 1.0 / 100 → 100 | 3.0 → 2.3 / 100 → 76 | 5.0 → 2.6 / 100 → 46 |
| special_forces | 1.0 → 0.9 / 100 → 92 | 3.0 → 1.4 / 96 → 28 | 4.9 → 0.5 / 92 → 0 |
| van | 1.0 → 1.0 / 98 → 98 | 2.9 → 2.9 / 98 → 98 | 4.7 → 4.6 / 94 → 94 |
