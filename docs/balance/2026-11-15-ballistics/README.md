# Balance after the shooting rework (2026-11-15)

Shots now have spread, lead and flight (see `docs/design/units-table.md`, "Shots, spread and rounds in flight").
Same data as `../2026-11-15-armour-hidden/` (sniper small_arms, mortar 25 x 0.8/s). "Before" = the old hit-roll
model on the same data.

- Armour: `tools/balance/ArmourHidden.cs`, 40,000 runs: `armour-runs.csv`, `armour-summary.csv`, `armour-tables.md`.
  Compare with `../2026-11-15-armour-hidden/`.
- Infantry: `tools/balance/Calibration.cs` (Codex's runner: one defender in the open against 1-5 attackers),
  13,500 baseline runs + 15,120 sensitivity runs: `infantry-base.csv`, `infantry-eff.csv`, `infantry-rank.csv`;
  the same with the old model: `infantry-before-*.csv`.

## What changed

1. **Close range is now deadly for everyone.** Accuracy is quoted at half range; nearer, the spread covers less
   than a man, so at a quarter of the range even a 40% weapon hits about 96% of the time. Fights in the open are
   now decided by who sees whom first: results are close to all-or-nothing.
2. **Machine gun vs a column of regulars: held against 5 went from 13% to 100%.** Misses hit the men behind.
   Against special forces it still loses from 2 on.
3. **Rifle squad got weaker** (2 regulars: 20% -> 0%; 3 recruits 38% -> 21%): the regulars hit it more at close range.
4. **Hiding matters more.** Anti-tank team alone in the open: 0% against tanks (tanks now hit it at 25-40 m);
   in a trench 30-74%, in a forest belt 52-72%. FPV operator alone: in the open it dies to the IFV or modern tank,
   hidden it stops them 80-96%.
5. **Mortar with recon now stops everything (100%), modern tanks and MRAPs too** (were 20-58%). Shells land where
   the target will be (vehicles drive straight), the spread shrinks at shorter range, and the blast counts from the
   vehicle's hull. Alone it fires at vehicles 25-40 m away and kills an IFV there (no minimum range).
6. Anti-tank team and FPV with recon: 92-100%, as before.
