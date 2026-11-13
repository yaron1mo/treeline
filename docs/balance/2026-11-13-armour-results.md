# Anti-armour Monte Carlo — baseline results (2026-11-13)

Successful workflow: https://github.com/yaron1mo/treeline/actions/runs/43460891101
Artifact `armour-calibration` (`armour-runs.csv`, `armour-baseline.csv`, `armour-effects.csv`). **9,120 simulations:** 7,200 baseline + 1,920 matched perturbations.
All results from the actual `Treeline.Sim` using unit data snapshotted in the Codex PR branch (sniper attack speed 0.7 synced, though sniper not used in armour tests).

## Single vehicle vs single standalone defender — 100 seeds each

Standard 1.5s release (irrelevant for count=1). Numbers are percentages; `Stop vehicle` is P(vehicle destroyed before crossing the line). `Veh. HP damaged` is average fraction of initial HP removed (includes cases with 0 and 100% damage). `Defender lives` is P(defender survives):

| Defence | Vehicle | Stop vehicle | Vehicle HP damaged | Defender lives |
|---|---|---:|---:|---:|
| Anti-tank | Old tank (300 HP, medium) | 2 | 43.6 | 2 |
| Anti-tank | Modern tank (300 HP, high) | 0 | 34.3 | 0 |
| Anti-tank | IFV (300 HP, medium) | 11 | 85.5 | 21 |
| Anti-tank | MRAP (60 HP, high) | 99 | 99.6 | 100 |
| FPV operator | Old tank | 0 | 1.5 | 0 |
| FPV operator | Modern tank | 0 | 0.0 | 0 |
| FPV operator | IFV | 0 | 9.6 | 65 |
| FPV operator | MRAP | 0 | 32.0 | 100 |
| Mortar | Old tank | 0 | 2.1 | 0 |
| Mortar | Modern tank | 0 | 1.4 | 0 |
| Mortar | IFV | 0 | 8.8 | 97 |
| Mortar | MRAP | 0 | 15.1 | 100 |

**Carrier caveat:** IFV destruction can still lead to line breach from dismounted soldiers. For anti-tank vs 1 IFV, vehicles are stopped 11% of the time but **full line held = 0%**. Metrics must not be conflated.

## Larger groups (1.5s release)

| Defence | 2 MRAPs: P(stop all) | 3 MRAPs: P(stop all) |
|---|---:|---:|
| Anti-tank | 91% | 55% |
| FPV operator | 0% | 0% |
| Mortar | 0% | 0% |

Against two or three Old Tanks, Modern Tanks or IFVs, **all three standalone defenders scored 0% stopping all vehicles** in the 100 seeds per pair.

## Sensitivity / counterfactual screening

Factor ×2 of one variable, 40 *paired* seeds for each variant. No actual game stats edited.

- Anti-tank vs single **Old tank**: doubling sight improves vehicle-stop probability by **+57.5 percentage points** (matched 40 seeds); doubling weapon hit damage **+27.5 pp**. Old tank at base 30m detection is bottlenecked by engagement opportunity.
- Anti-tank vs single **IFV**: doubling shot damage improves P(stop) by **+80 pp**; doubling sight **+15 pp**. Indicates a shots-to-kill threshold.
- FPV vs single **MRAP**: double hit damage improves mean percentage vehicle HP damaged by **+32 pp**, but vehicle-stop probability still **0%**. The operator's 15-second reload and 3-second launch delay matter; a single standalone FPV operator is not a credible tank counter at present.
- FPV vs **Modern tank**: zero HP damage in the baseline. Sight×2 improves it by 6.4 percentage points of HP on average but still yields 0 kills. The launcher is eliminated before useful engagement in the basic case.
- Mortar versus every single armoured vehicle: 0% stops even with either doubled sight or doubled damage. This is consistent with its role being soft/area targets rather than defeating heavy armour.

## Expected damage and mechanics sanity check

- Anti-tank: 40 damage at 70% accuracy × armour multiplier 1 (medium) or 0.85 (high). **8 successful shots** to kill 300HP medium tank, **9** for high, before aiming/travel.
- FPV: `hit_damage` 60 × 0.8 accuracy (treated as *damage multiplier* by engine, not stochastic hit chance) × armour 0.6 medium / 0.4 high = **28.8 / 19.2 HP per impact**, requiring **11 / 16 drone impacts** against a 300HP tank. This exceeds realistic engagement time for a single operator without spotter.
- Mortar: 15 damage at 30% accuracy, 6m blast, 30m own detection; explosive armour multiplier 0.6 medium / 0.35 high. Expected damage per shot hitting target in field ≈2.7 HP medium / 1.575 HP high. Models neither fire-based area denial nor tank mobility damage/suppression.
- `range_m` values 600–1000m do **not** permit firing at unseen vehicles. All three counters have only **30m sight** in this baseline. Recon is intentionally not added yet.

## Next design decision

**Do not tune armour data blindly.** First agree desired vehicle matchup:
- Anti-tank = primary tank/IFV counter; define how many successful missile hits should stop Old/Modern tank and IFV.
- FPV = single-use precision strike against a valuable/armoured target; decide intended time-to-impact and whether a target must first be spotted by recon.
- Mortar = indirect/AoE soft-target control; decide if mortar should damage heavy tanks materially, or focus on light vehicles and infantry.

Then test (a) calibrated per-hit warhead damage, (b) engagement/sight opportunity and launch cadence; only after that expand to recon and positioned cover. These are software-balancing questions, not empirical real-world military effectiveness.
