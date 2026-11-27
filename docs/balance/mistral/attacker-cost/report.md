# Attacker cost: what each attacker costs the defence

Task: `mistral/attacker-cost` (see `docs/agents/mistral.md`, section 9). Date: 2026-11-24.
Branch: `mistral/attacker-cost`. Data: 3 Balance runs, 726 probe waves (see `tools/balance/probe_waves.csv`, v8).

## 1. Method

The probe set has single-type waves (one type, counts 1 to 30), pair waves, triple waves and mixed game-like waves. The Balance run holds each wave with the smallest defence that wins, and gives the cost of that defence. "Not held" means the defence did not win with a budget of 30000.

The tables below give, per field, a straight-line fit of the cost curve: `cost = entry + each x count`. The fit line is a summary only. The real curves have steps, and the model in section 5 uses the full curves, not the fit lines.

## 2. Cost per attacker type

Counter of each type (from `attacker_cost.py`): recruit, regular, special_forces (special forces), motorbike, buggy and van are machine-gun types; mrap, ifv, old_tank (old tank), modern_tank (modern tank) are anti-tank types; helicopter is an air-defence type.

### Forest belts

| Attacker | Counter | Entry | Each | r2 | Steps and notes |
| --- | --- | --- | --- | --- | --- |
| recruit | machine_gun | 198 | 16 | 0.79 | Steps at 4, 6, 12, 20; flat at 8-10 and 16-30 |
| regular | machine_gun | 165 | 59 | 0.94 | Smooth steps each 2 units |
| special_forces | machine_gun | 267 | 118 | 0.95 | Steps at 8, 12, 16 |
| motorbike | machine_gun | -31 | 31 | 0.88 | 1 to 3 units cost 25 (one rifle squad holds them); then +100 steps |
| buggy | machine_gun | 9 | 20 | 0.79 | 1 to 3 units cost 25; then +100 steps |
| van | machine_gun | 413 | 74 | 0.90 | Flat 500 to n=3; +100 steps from n=4 |
| mrap | anti_tank | 822 | 153 | 0.92 | Cost stops at 1925 from n=6 (the defence holds 8 with the same money as 6) |
| ifv | anti_tank | 862 | 757 | 0.96 | Big step from n=3 to n=4: 2575 to 4275 (+1700) |
| old_tank | anti_tank | 1489 | 174 | 0.80 | Cost goes DOWN at n=2 (1925 to 1675); flat to n=4 |
| modern_tank | anti_tank | 1496 | 232 | 0.98 | Flat 2375 from n=3 to n=4; steps after |
| helicopter | air_defence | 1175 | 5750 | 0.97 | 1 = 6375, 2 = 13775, 3 = 17875. The second helicopter costs +7400, the third +4100. One air-defence unit is needed even for one helicopter |

### Balka and railway

| Attacker | Counter | Entry | Each | r2 | Steps and notes |
| --- | --- | --- | --- | --- | --- |
| recruit | machine_gun | 133 | 5 | 0.66 | Flat 200 from n=6 to n=20 |
| regular | machine_gun | -234 | 112 | 1.00 | Smooth |
| special_forces | machine_gun | 105 | 222 | 0.98 | Smooth |
| motorbike | machine_gun | -2 | 19 | 0.89 | 25 to n=3, as on Forest belts |
| buggy | machine_gun | 31 | 18 | 0.62 | 25 to n=3 |
| van | machine_gun | 171 | 62 | 0.91 | Cheaper than Forest belts at each count |
| mrap / ifv / old_tank / modern_tank | anti_tank | not held | not held | - | See section 3 |
| helicopter | air_defence | not held | not held | - | See section 3 |

### River crossing

| Attacker | Counter | Entry | Each | r2 | Steps and notes |
| --- | --- | --- | --- | --- | --- |
| recruit | machine_gun | 139 | 9 | 0.76 | Steps of 100 |
| regular | machine_gun | -267 | 132 | 0.99 | Smooth, more costly than Forest belts |
| special_forces | machine_gun | 762 | 396 | 0.98 | Much more costly than the other fields |
| motorbike | machine_gun | -2 | 19 | 0.89 | 25 to n=3 |
| buggy | machine_gun | 31 | 18 | 0.62 | 25 to n=3 |
| van | machine_gun | 251 | 54 | 0.89 | Similar to Forest belts |
| mrap | anti_tank | 1046 | 68 | 0.61 | Flat 1475 from n=4 |
| ifv | anti_tank | 1269 | 216 | 0.94 | Smooth steps |
| old_tank | anti_tank | 1181 | 124 | 0.79 | Flat 1475 to n=4, then steps |
| modern_tank | anti_tank | 1075 | 235 | 0.95 | Smooth steps |
| helicopter | air_defence | -28625 | 29300 | 1.00 | 1 = 675, 2 = 29975. The first helicopter is cheap, the second needs much more air defence |

## 3. Waves that the defence does not hold at 30000

Balka and railway: 310 of 407 probe waves are not held. All 34 waves with armour but no infantry are not held. Waves with the same armour plus infantry are held (for example: special_forces + old_tank + van is held). Reason: the defence on this field has no line of sight across the railway cut, so armour that arrives without infantry to clear the obstacles stays out of range and wins on time. Helicopter waves on this field are not held at any count.

River crossing: 20 of 697 probe waves are not held. Only waves with 2 or more helicopters, or a helicopter plus heavy armour, fail. Field is otherwise easier to hold than the cost table shows, because the bridge limits the attack direction.

Forest belts: all probe waves are held.

## 4. Map coefficients

Median cost ratio against Forest belts, on probe waves present on both fields:
- Balka and railway: 0.68 (but only waves that the defence holds; see section 3)
- River crossing: 1.0

These ratios do not hold for special_forces (much more costly on River crossing) or for armour (no held waves on Balka and railway). The model in section 5 is fit per field, so it needs no map coefficient.

## 5. Mixed-wave model

Model A (sum of the single-type curves) and model B (shared entry per counter) are far off (mean error 21 to 50 percent, depending on field). Reason: the units of one wave share the same defence. Two types that use the same counter do not add up.

The final model works per field:

1. Start from a linear-ridge formula on features of the wave: counts, type presence, presence of type pairs, and interpolated single-type curve costs.
2. Correct the result with gradient boosting (60 trees, depth 5) in square-root space. The tree features add pair-min terms: the cost of the cheapest unit of a pair drives the shared-defence effect.

Fit: `fit_model.py` in this folder. Input: `tools/balance/probe_waves.csv` and `wave-cost.csv` from the run artifact. Output: `model.json` (all coefficients, readable by a script) and `metrics.json`.

Test on 213 mixed waves that the fit never used (tags "mixed" and "mixed-test2"):

| Field | Waves | Mean error | Within 20% |
| --- | --- | --- | --- |
| Forest belts | 80 | 13.0% | 81.2% |
| Balka and railway | 54 | 13.4% | 81.5% |
| River crossing | 79 | 13.6% | 87.3% |
| Pooled | 213 | 13.3% | 83.6% |

Stop rule met: 83.6% of mixed waves within 20% is more than 80%, with a mean error of 13% (under 25%).

Limit: the model says nothing about waves that the defence does not hold, because those waves have no measured cost. On Balka and railway, do not use the model for waves with armour and no infantry.

## 6. What is strange

1. The old tank on Forest belts costs LESS at n=2 than at n=1 (1675 against 1925). More attackers helps the defence hold the line.
2. Motorbike and buggy waves with 1 to 3 units cost 25: one rifle squad holds them. Only the fourth unit starts to cost money.
3. Mrap on Forest belts saturates at 1925: the same defence holds 8 as holds 6.
4. The first helicopter on River crossing costs 675, but the second costs 29975. The first air-defence unit is cheap; the second one has no place in the build.
5. Armour with no infantry never holds on Balka and railway (section 3). This may be a bug or a field feature; it is a design question.
6. Wave 654 (30 regular, 10 motorbike, 3 ifv, 5 old_tank, 1 helicopter) costs 9525 on Forest belts and 24175 on River crossing (2.5 times more).

## 7. Proposal: how make_waves.py can use this model

This is a proposal for the next task. It needs no change now.

1. Read `model.json` for the field of the wave.
2. Try candidate waves: start from the wave of the previous run plus or minus one unit of a random type (keep the first-appearance order and the unit caps).
3. Predict the cost of each candidate with the model.
4. Pick the candidate whose predicted cost is nearest to `target_cost = the money available before the wave / the target ratio`.
5. Do 30 to 50 tries per wave and take the best.
6. The money available before wave w stays: `start_budget + sum over earlier waves of (wave_income + kill_reward_share x reward of each kill)`, as used in `mistral/economy-1`.

Open question for the CEO: the target ratio bands in `tools/balance/target_curve.csv` were set for the old cost measure. With this model, a late wave at ratio 1.0-1.2 needs many heavy units (10 or more). Is that the design that you want?

## 8. Files

- `report.md` - this report
- `model.json` - model coefficients (readable by a script)
- `metrics.json` - test metrics and all 213 test predictions
- `fit_model.py`, `fitlib.py` - model fit
- `make_charts.py`, `pngchart.py` - chart scripts
- `cost-by-type.png` - cost against count, all attacker types, all fields
- `pred-vs-measured.png` - predicted against measured cost, mixed waves
- `iterations.md` - run journal
