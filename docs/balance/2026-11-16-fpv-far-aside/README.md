# FPV operator 70 m off the route, out of sight (2026-11-16)

Yaroslav: put the operator aside so the attackers never reach it, and see how far they get against the drones alone. Operator at (110, 280), the route runs down x = 40, recon drone over the approach, drone speed 20 m/s, blast 3 m, reload 3 s. Attackers 0.3 s apart, 50 seeds. `MATCHUPS_DEFENDERS=fpv_operator MATCHUPS_POSITIONS=far_aside`.
Before = nobody shoots our drones; after = attackers shoot them (2026-11-16).

No attacker ever saw the operator (checked). Where it still dies, it is stray bullets: they shoot at the drone flying at them from the operator, and a miss flies on along the same line to the operator behind it.

| Attacker | n | killed before → after | line damage before → after | operator alive | drones lost |
|---|---|---|---|---|---|
| buggy | 1 | 1.00 → 1.00 | 0.0 → 0.0 | 100% | 0.0 |
| buggy | 2 | 2.00 → 2.00 | 0.0 → 0.0 | 100% | 0.0 |
| buggy | 3 | 2.84 → 2.84 | 0.2 → 0.2 | 100% | 0.0 |
| buggy | 4 | 3.68 → 3.68 | 0.3 → 0.3 | 100% | 0.0 |
| buggy | 5 | 3.72 → 3.72 | 1.3 → 1.3 | 100% | 0.0 |
| fpv | 1 | 0.00 → 0.00 | 0.0 → 0.0 | 100% | 0.0 |
| fpv | 2 | 0.90 → 0.90 | 0.0 → 0.0 | 100% | 0.0 |
| fpv | 3 | 0.92 → 0.92 | 0.0 → 0.0 | 100% | 0.0 |
| fpv | 4 | 0.94 → 0.94 | 0.0 → 0.0 | 100% | 0.0 |
| fpv | 5 | 0.94 → 0.94 | 0.0 → 0.0 | 100% | 0.0 |
| helicopter | 1 | 0.00 → 0.00 | 16.4 → 19.7 | 94% | 3.7 |
| helicopter | 2 | 0.00 → 0.00 | 30.5 → 39.9 | 62% | 3.8 |
| helicopter | 3 | 0.00 → 0.00 | 48.4 → 60.0 | 20% | 3.5 |
| helicopter | 4 | 0.00 → 0.00 | 66.6 → 80.0 | 4% | 3.2 |
| helicopter | 5 | 0.00 → 0.00 | 83.0 → 100.0 | 4% | 3.0 |
| ifv | 1 | 0.98 → 0.24 | 3.0 → 10.4 | 92% | 2.4 |
| ifv | 2 | 1.94 → 0.06 | 7.5 → 23.3 | 78% | 2.5 |
| ifv | 3 | 2.52 → 0.12 | 16.5 → 34.6 | 84% | 3.1 |
| ifv | 4 | 2.06 → 0.02 | 30.9 → 47.8 | 80% | 3.4 |
| ifv | 5 | 2.12 → 0.08 | 41.8 → 59.0 | 92% | 3.4 |
| modern_tank | 1 | 0.72 → 0.72 | 2.2 → 2.2 | 100% | 0.0 |
| modern_tank | 2 | 1.44 → 1.44 | 4.5 → 4.5 | 100% | 0.0 |
| modern_tank | 3 | 2.04 → 2.04 | 7.7 → 7.7 | 100% | 0.0 |
| modern_tank | 4 | 1.62 → 1.62 | 19.0 → 19.0 | 100% | 0.0 |
| modern_tank | 5 | 1.48 → 1.48 | 28.2 → 28.2 | 100% | 0.0 |
| motorbike | 1 | 1.00 → 1.00 | 0.0 → 0.0 | 100% | 0.0 |
| motorbike | 2 | 2.00 → 2.00 | 0.0 → 0.0 | 100% | 0.0 |
| motorbike | 3 | 2.84 → 2.84 | 0.2 → 0.2 | 100% | 0.0 |
| motorbike | 4 | 3.68 → 3.68 | 0.3 → 0.3 | 100% | 0.0 |
| motorbike | 5 | 3.72 → 3.72 | 1.3 → 1.3 | 100% | 0.0 |
| mrap | 1 | 0.74 → 0.04 | 4.8 → 6.9 | 98% | 0.7 |
| mrap | 2 | 0.78 → 0.08 | 11.7 → 13.8 | 88% | 0.6 |
| mrap | 3 | 0.72 → 0.12 | 18.8 → 20.6 | 96% | 0.7 |
| mrap | 4 | 0.78 → 0.08 | 25.7 → 27.8 | 90% | 0.7 |
| mrap | 5 | 0.62 → 0.14 | 33.1 → 34.6 | 100% | 0.7 |
| old_tank | 1 | 1.00 → 1.00 | 0.0 → 0.0 | 100% | 0.0 |
| old_tank | 2 | 2.00 → 2.00 | 0.0 → 0.0 | 100% | 0.0 |
| old_tank | 3 | 2.94 → 2.94 | 0.3 → 0.3 | 100% | 0.0 |
| old_tank | 4 | 3.68 → 3.68 | 1.6 → 1.6 | 100% | 0.0 |
| old_tank | 5 | 4.28 → 4.28 | 3.6 → 3.6 | 100% | 0.0 |
| recruit | 1 | 1.00 → 1.00 | 0.0 → 0.0 | 100% | 0.1 |
| recruit | 2 | 2.00 → 1.88 | 0.0 → 0.1 | 100% | 0.7 |
| recruit | 3 | 3.00 → 3.00 | 0.0 → 0.0 | 100% | 0.5 |
| recruit | 4 | 4.00 → 3.70 | 0.0 → 0.3 | 100% | 1.2 |
| recruit | 5 | 5.00 → 4.46 | 0.0 → 0.5 | 98% | 1.5 |
| regular | 1 | 1.00 → 1.00 | 0.0 → 0.0 | 100% | 0.1 |
| regular | 2 | 2.00 → 1.84 | 0.0 → 0.2 | 100% | 0.8 |
| regular | 3 | 3.00 → 2.76 | 0.0 → 0.2 | 98% | 0.9 |
| regular | 4 | 4.00 → 3.44 | 0.0 → 0.6 | 94% | 1.7 |
| regular | 5 | 5.00 → 3.90 | 0.0 → 1.1 | 94% | 2.4 |
| special_forces | 1 | 1.00 → 1.00 | 0.0 → 0.0 | 100% | 0.4 |
| special_forces | 2 | 2.00 → 1.48 | 0.0 → 0.5 | 100% | 2.0 |
| special_forces | 3 | 3.00 → 1.48 | 0.0 → 1.5 | 98% | 2.7 |
| special_forces | 4 | 4.00 → 1.08 | 0.0 → 2.9 | 78% | 3.2 |
| special_forces | 5 | 4.92 → 0.56 | 0.1 → 4.4 | 64% | 3.8 |
| van | 1 | 1.00 → 0.98 | 0.1 → 0.3 | 100% | 0.1 |
| van | 2 | 2.00 → 2.00 | 0.0 → 0.0 | 100% | 0.0 |
| van | 3 | 2.82 → 2.78 | 2.7 → 3.1 | 100% | 0.1 |
| van | 4 | 2.86 → 2.80 | 16.1 → 16.8 | 100% | 0.1 |
| van | 5 | 2.88 → 2.82 | 29.8 → 30.5 | 100% | 0.1 |
