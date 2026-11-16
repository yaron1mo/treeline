# Full 1 v 1 matrix, 2026-11-16

Same setup as `2026-11-15-matrix-groups` (1 to 5 attackers 0.3 s apart, open and trench+belt, 50 seeds; recon for mortar, ATGM, air defence and drone operators). Sheet tab `balance_1v1` replaced with this run.

Changes since the last matrix:
- FPV: our drone 20 m/s, blast 3 m; the enemy FPV 20 m/s, blast 3 m (reload unchanged).
- Rifle squad damage 7 (Sheet): a motorbike now takes 3 hits, so 1 of 3+ motorbikes gets past a lone rifle squad.
- The enemy shoots our drones (`drone_unit`).
- A shot at something in the air only hits things in the air; a shot at the ground passes under drones and aircraft.
- A hit carrier standing to let its troops out is left for later: defenders and drones go for the troops.

## Effect of the carrier rule alone

`runs-without-carrier-focus.csv` is the same run for van, IFV and MRAP with that rule switched off. Only the van moves (small arms cannot hurt the IFV or MRAP enough to make them stop). Trench+belt, killed vans / line damage / defender alive %:

| Defender | vans | without the rule | with the rule |
|---|---|---|---|
| Machine gunner | 1 | 1.0 / 0 / 100% | 0.3 / 5.2 / 18% |
| Machine gunner | 5 | 1.3 / 52 / 94% | 0.0 / 69 / 0% |
| Sniper | 1 | 1.0 / 0 / 100% | 0.0 / 9.8 / 0% |
| Sniper | 3 | 1.2 / 26 / 22% | 0.0 / 42 / 0% |
| Rifle squad | 1-5 | no change | no change |

Before, the machine gunner and the sniper kept shooting the stopped van and killed the recruits still inside (12 of them, one out every 0.6 s). Now they turn to the recruits one at a time and are overrun.
