# Mistral wave-generator report

Task: `mistral/wave-generator` (see `docs/agents/mistral.md`, section 10).
Goal: `tools/balance/make_waves.py` generates 30 waves. The measured cost of each wave must follow the target cost `target(n) = 250 * 1.13^(n-1)`, rounded to the nearest 25. Waves 5, 10, 15, 20, 25 and 30 are relief waves (target times 0.8).

This version answers Claude review 1 on PR #9: the waves now come from the pure generator (no `PLAN`), and the cost model was refit with the waves measured in the runs of this task.

## Result after review 1 (3 runs, stop rule reached)

Best run: **45916087929 (run 4)**: **23 of 30 waves within 20% of the target cost**, 0 waves "not held", mean error 18.8%. The pass gate of review 1 (at least 24 of 30) was not reached; the stop rule (3 runs) was reached, so this is the final result of this round.

| Run | Balance run | Generator result (within 20% of target) | Mean error | Not held | Model error on the 30 new measured waves (Forest belts) |
| --- | --- | --- | --- | --- | --- |
| 1 (before review 1, search fallback) | 45865138318 | 18 of 30 | 33.0% | 0 | old model, generator read the features in the wrong order (bug below): 40.2% mean, 22 of 60 within 20% on the runs 1+2 waves |
| 2 (before review 1, `PLAN`) | 45867645128 | 27 of 30 (from the plan, not the generator) | 11.1% | 0 | — |
| 3 | 45915363348 | 10 of 30 | 42.0% | 0 | 54.9% mean, 18 of 30 within 20% |
| 4 | 45916087929 | **23 of 30** | 18.8% | 0 | 49.7% mean, 22 of 30 within 20% |
| 5 | 45916673189 | 19 of 30 | 17.5% | 0 | 22.0% mean, 19 of 30 within 20% |

Model errors are out-of-sample: each run's waves were not yet in the training set when the model was evaluated on them (`eval_model.py`, field Forest belts, small-wave floor applied).

Chart: `target-vs-measured.png` (run 4: target cost in grey, measured cost in green, misses in red).

## Method

1. **Pure generator.** `PLAN` and the measured-cost anchors (`MEASURED`) are removed from the generation path of `make_waves.py`. Every `N` and every seed now goes through the same randomised local search that drives the model prediction to the target cost of the wave. The old plan is kept as test data only: `plan_run2.csv`. The search keeps all task rules: unit caps, unlock order of first appearances, 2+ types from wave 4, no type above 70% of the predicted cost from wave 8, predicted cost at most 1.35x the previous wave except at a first appearance. The same seed gives the same waves (checked: seed 7 twice gives identical output; N=12, seed 3 also works).
2. **Feature-order bug, found after run 3.** `make_waves.py` built the feature list in unlock order, but the model bins and trees index the `types` order of the fit. All predictions in runs 1-3 used permuted features. `TYPES` now comes from `model.json` ("types"); `predict()` now matches `eval_model.py` exactly (checked on several compositions).
3. **Refit loop (generate → measure → add → fit → generate).** The waves measured in the Balance runs are added to the training set of the model copy in this folder (`training_waves.csv` / `training_wave_cost.csv`, tag `mixed-train3`, waves 10001+; runs 1+2 = waves 10001-10060, run 3 = 10061-10090, run 4 = 10091-10120). `fit_model.py` refits the PR #7 model on them (the held-out probe test waves are unchanged, so the metrics stay comparable). The model that generated each run is kept: `model_iter3.json`, `model_iter4.json`, `model_iter5.json`; the current one is `model.json`. `make_waves.py` reads `docs/balance/mistral/wave-generator/model.json`.
4. **Small-wave floor.** A wave predicted below 2x the price of one rifle squad (100, measured: one rifle squad is the first budget above the 25 minimum, for example buggy x4 = 100 on Forest belts) costs at least 100 (`small_wave_floor` in `model.json`, applied in `predict()`). The floor never applies above 200.

Model metrics of the final refit (`metrics.json`): held-out probe waves 13.4% mean error, 76% within 20% pooled; on the 120 measured wave-generator waves (in-sample) Forest belts 24.5% mean error, 83% within 20%.

## Final check table (run 4, 45916087929, Forest belts, the gate field)

| Wave | Target | Measured | Error |
| --- | --- | --- | --- |
| 1 | 250 | 200 | -20.0% |
| 2 | 275 | 25 | -90.9% |
| 3 | 325 | 300 | -7.7% |
| 4 | 350 | 300 | -14.3% |
| 5 | 325 | 300 | -7.7% |
| 6 | 450 | 500 | +11.1% |
| 7 | 525 | 500 | -4.8% |
| 8 | 600 | 400 | -33.3% |
| 9 | 675 | 600 | -11.1% |
| 10 | 600 | 500 | -16.7% |
| 11 | 850 | 600 | -29.4% |
| 12 | 950 | 900 | -5.3% |
| 13 | 1075 | 1100 | +2.3% |
| 14 | 1225 | 1400 | +14.3% |
| 15 | 1100 | 1200 | +9.1% |
| 16 | 1575 | 2000 | +27.0% |
| 17 | 1775 | 2200 | +23.9% |
| 18 | 2000 | 2100 | +5.0% |
| 19 | 2250 | 2500 | +11.1% |
| 20 | 2050 | 2300 | +12.2% |
| 21 | 2875 | 5575 | +93.9% |
| 22 | 3250 | 4125 | +26.9% |
| 23 | 3675 | 2975 | -19.0% |
| 24 | 4150 | 3725 | -10.2% |
| 25 | 3750 | 4475 | +19.3% |
| 26 | 5300 | 5275 | -0.5% |
| 27 | 6000 | 5375 | -10.4% |
| 28 | 6775 | 6225 | -8.1% |
| 29 | 7650 | 6925 | -9.5% |
| 30 | 6925 | 7525 | +8.7% |

Measured cost of the same run 4 waves on the other fields (wave 1-5 and 26-30): Balka and railway 100, 25, 100, 200, 200 ... 5 waves not held at 12000 in the late waves (NaN); River crossing 100, 25, 200, 200, 200 ... 6875, 8625, 10125, 9475, 8325.

## Misses and causes (run 4)

- Wave 2 (-90.9%): the generator picked 2 regular (predicted 289); the wave leaks with 3 or less line damage and is measured at the 25 minimum. The floor in the model (at least 100) does not cover this case: the scripted player "holds" a leaking wave at the minimum budget.
- Wave 21 (+93.9%): the first old_tank wave. The model underprices a lone tank with little infantry pressure; the armour area was already wrong in run 1 (measured 2.0-2.5x predicted).
- Waves 16-17 (+27.0%, +23.9%): van/motorbike mixes cost more than the model predicts.
- Waves 8, 11 (-33.3%, -29.4%): buggy/motorbike escorts above a threshold add no cost (same finding as run 2).

## Open points

- Helicopter never appears: its cost (6375 for one unit) is above every wave target except 28 to 30, and the 1.35 growth limit blocks a first appearance there. This is a balance question for Claude and Yaroslav, not for this task.
- The pass gate of review 1 (at least 24 of 30 within 20%) was not reached in 3 runs: best 23 of 30 (run 4). The stop rule (3 runs) ended this round. The remaining error is concentrated in small waves (leak at the 25 minimum) and the first armour waves.
- `tools/balance/target_costs.csv` and the generated `unity/Assets/Data/waves.csv` were used for the runs and are removed from the branch (dev files only).
