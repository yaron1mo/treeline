# Balance run 2026-11-06 (with drone operators)

Same tool and rules as `docs/balance/2026-11-06/` (shared vision), now with the drone operators. No enemy aviation, EW or air defence in the waves. All numbers are proposals.

`maps.csv`, `waves.csv`, `units_totals.csv` have a `loadout` column, 20 seeds per map and loadout:
- `base`: the six defenders of the first run, budget 1500.
- `base+fpv`: plus an FPV operator and a recon drone, budget 1900.
- `base+fpv+bomber`: plus a bomber operator, budget 2350.
`duels.csv` now also has the two operators (with a recon drone as spotter).

## What it shows

1. **The FPV is not overpowered.** Waves survived on average: base 1.48, +FPV 1.35, +FPV+bomber 1.43. 0 of 60 runs survive five waves in all three. The FPV operator itself is lost in 27 of 60 runs and deals about 44 damage per run (roughly one strike).
2. **Reload is not the limit.** FPV reload 15 s -> 8 s -> 4 s (separate sweep, `base+fpv`): waves 1.35 / 1.35 / 1.37, damage per run 44 / 49 / 52. Cause not found yet; to check next: how many drones launch per run, and why the operator dies (every attacker that sees it can shoot it).
3. **Duels:** the FPV operator loses to every stream except buggies and motorbikes (2 of 3). One strike is 43 damage on a regular (15 HP) and 19 on an MRAP (60 HP, high armour). The bomber kills all 8 recruits and 4 of 8 regulars, little against vehicles.
4. Everything else from the first run still holds: the defence loses to regulars and special forces, MRAPs and vans do most of the line damage.
