# Monte Carlo + sensitivity — results (2026-11-13)

Run: https://github.com/yaron1mo/treeline/actions/runs/43449948465
Input: current `unity/Assets/Data/units.csv`, `armour.csv`, `obstacles.csv`, `battle_rules.csv` from the Codex PR branch.
Result artifact `open-field-balance`: `calibration-runs.csv`, `calibration-baseline.csv`, `calibration-effects.csv`, `calibration-rank.csv`, plus earlier open-field files.

## Protocol and validity
- 13,500 Monte Carlo runs: 3 defenders × 3 attackers × five group sizes (1–5) × three spawn intervals (0.75,1.5,3.0 seconds) × 100 seeds.
- 15,120 OAT +/-20% sensitivity simulations: 3 defenders × 3 attackers × three wave sizes (3/4/5) × seven parameters × two directions × 40 matched seeds.
- Same 40 seed IDs used as baseline reference for sensitivity comparisons.
- Single stationary unfortified defender on flat 40×300m field, no cover/obstacles; shared detection model per rules. No combined formations.
- `held_pct` is P(no attacker crosses); `defender_survival_pct` is P(defender lives), and `defender_hp_remaining_pct` averages post-wave percentage including 0% on defender death. These happened to match exactly for binary outcomes across this test's 28,620 runs, which should not be assumed elsewhere.
- Wilson 95% CIs are descriptive Monte Carlo uncertainty under this seed generator, **not** confidence over unknown real-world scenarios.

## Baseline, standard spawn interval 1.5 seconds
P(hold), percent, n=100 each:

| Defender vs attacker | n=3 | n=4 | n=5 |
|---|---:|---:|---:|
| Rifleman vs recruit | 38 | 5 | 1 |
| Rifleman vs regular | 0 | 0 | 0 |
| Rifleman vs special forces | 0 | 0 | 0 |
| Machine gunner vs recruit | 89 | 67 | 48 |
| Machine gunner vs regular | 2 | 0 | 0 |
| Machine gunner vs special forces | 0 | 0 | 0 |
| Sniper vs recruit | 100 | 100 | 98 |
| Sniper vs regular | 97 | 95 | 92 |
| Sniper vs special forces | 0 | 0 | 0 |

Baseline `rifleman vs recruit n=3`: P(hold)=38% (Wilson 95% CI 29.1–47.8%), mean killed 1.85, average surviving HP 18.6% of initial including dead.
Baseline `rifleman vs regular n=2`: P(hold)=20%; at n=3, P(hold)=0%.
Baseline `machine gunner vs recruit n=5`: P(hold)=48% (CI 38.5–57.7%), mean killed 3.28, average remaining HP 27.4%.
1v1 vs special forces: P(hold) rifleman 3%, machine gun 44%, sniper 29%. This fails the intended sniper anti-elite specialty.

## Wave release timing is a material factor
For `rifleman vs 3 recruits`: P(hold) at 0.75 / 1.5 / 3.0s = **31 / 38 / 57%**.
For `machine gunner vs 5 recruits`: **40 / 48 / 60%**.
For `sniper vs 5 regulars`: **62 / 92 / 97%**.
These are time gaps, **not physical formation distance**, since units can choose different entry cells/paths.

## One-at-a-time sensitivity (+/-20%, fixed gap 1.5s)
Mean absolute effect on penetration (percentage points) across all three defending and three attacking classes, counts 3/4/5, 40 shared seeds:
1. Defender sight range: 11.82 pp.
2. Attacker HP: 4.74 pp.
3. Defender accuracy: 4.52 pp.
4. Defender attack speed: 4.21 pp.
5. Defender damage per hit: 3.47 pp.
6. Defender HP: 2.35 pp.
7. Attacker speed: 1.39 pp.
Do not mistake these pooled values for universal causal effects: many 100%/0% outcomes are saturated and effects can have thresholds and class interactions.

Examples of nonlinear threshold effects:
- Rifleman vs 3 recruits: -20% attacker HP (10→8) changes baseline to one-hit rifleman kills, improving defence survival by **+60 pp** across paired n=40 seeds. +20% HP leaves two shots to kill, almost no change.
- Sniper vs 3 special forces: +20% sniper shot damage (15→18; effective vs low armour 16.2) crosses special forces 15HP one-hit threshold, reducing penetration by **38.33 pp**; defendant still survives only 7.5% of these perturbation runs.
- Rifleman vs 3 recruits: increasing defender sight range 20% raised defender survival **+37.5 pp** on paired 40 seeds; reducing sight 20% lowered survival **−27.5 pp**.

## Interpretation and next design decision
- Need nominate a **reference attacker** (regular is a good default candidate) for the intended 1:3/1:4/1:5 ordinary-defender threshold; do not force every unit pairing to the same ratio.
- No blanket stat tuning yet: sniper currently specializes in low/regular troops, not fast expensive threats; this may require priority targeting and engagement opportunity mechanics, not just HP/DPS.
- Sensitivity demonstrates engagement sight and shot-to-kill thresholds dominate many runs; first stabilize mechanics and define class-counter objectives, then grid-search to explicit targets.
