# Anti-armour balance — v1 Monte Carlo (2026-11-13)

## Scope

**First screen only**, not the final vehicle-balance pass. The next TD matchups are:

- Attackers: `old_tank`, `modern_tank`, `ifv` and `mrap`.
- Standalone defenders: `mortar`, `anti_tank`, and `fpv_operator` (an FPV **launcher**, not the attacking `fpv` aerial row).
- One stationary defender without cover or supporting spotter at (20,200), 40×300m flat field; release intervals 1.5 and 3 seconds. Counts 1/2/3, 100 seeded Monte Carlo runs each = 7,200 baseline simulations.
- Matched-seed one-factor screens: **sight ×2** or **direct-shot/FPV-hit damage ×2** for count 1/2, 1.5s release, 40 seeds each = 1,920 variants. Variants are *counterfactual diagnostic tests only*; never written into authoritative Sheets/CSV.
- Separate metrics: **P(all vehicles stopped)**, **P(no line damage)**, P(defender survives), mean defender HP remaining (% with deaths = 0), mean number of vehicles destroyed, mean vehicle HP lost (%) across all spawned vehicles and expected damage in HP. Two breach metrics differ when a carrier dismounts passengers.
- Attackers all spawn via the standard sim pathing; carrier dismounts and explosive wrecks stay enabled. No new combat code is introduced by the test.

## Initial stat and armour observations

- `old_tank` and `ifv`: medium armour (300HP); `modern_tank`: high armour (300HP); `mrap`: high armour (60HP). IFV and MRAP can unload troops, which can themselves cross after the vehicle is destroyed.
- `mortar` has one regular shot/s, 15 raw damage and 0.3 accuracy; explosive damage multiplier 0.6 for medium armour, 0.35 for high. Blast radius 6m, independent hit rolls per target.
- `anti_tank` has one shot/s, 40 damage, 0.7 accuracy, anti-tank armour multipliers 1.0 for medium, 0.85 for high; can only target vehicles.
- `fpv_operator` has 60 hit_damage, 0.8 accuracy, FPV armour multipliers 0.6 medium / 0.4 high, 3-second launch delay, 15-second reload, drone speed 30m/s. FPV accuracy in the current engine scales damage *deterministically* per successful drone impact; it is not an independently sampled hit/miss. Drone can be lost when launcher dies.
- All three counters have 30m own sight. Larger weapon `range_m` is not sufficient to engage targets beyond **side vision**. No recon support is part of the baseline.
- Sight ×2 and damage ×2 screens discriminate detection-time and underpowered-warhead effects without guessing new balance values.

## Critical assumptions and next decisions

1. Do not reuse the Regular infantry 1:2 open-field target for vehicles. Vehicle line-damage cost, speed, armour and transport roles need distinct counter goals.
2. Mortar may naturally be anti-infantry/area denial rather than a dedicated tank killer; assess its role from results, not an assumption that every counter should beat every vehicle.
3. The current all-vehicle `TargetClass` values may route FPV prioritization differently for fast transports and heavy armour. Drone operators select by a `target_order` list; single-target tests cannot measure multi-class priority.
4. No terrain, recon, target prioritization between different vehicle classes, realistic missile reload, FPV countermeasures, or projectile travel are assessed in this first screen.

## Repository workflow

- Authoritative unit stats: Google Sheet `Treeline — Units table`. Sniper attack speed **0.7**, synchronized from Sheets to `units.csv` before running anti-armour tests. This is a separate user-requested edit; do not modify other user values.
- Test runner: `tools/balance/ArmourCalibration.cs` with the existing `Treeline.Sim`; postprocess with `tools/balance/summarize_armour.py` and CI job `anti_armour` in `.github/workflows/open-field-balance.yml`.
- Generated artifacts: `armour-runs.csv`, `armour-baseline.csv` and `armour-effects.csv`.
