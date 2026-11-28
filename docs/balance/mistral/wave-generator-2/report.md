# Mistral wave-generator-2 report

Task: issue #17, follow-up of PR #9. Spec: `docs/agents/mistral.md` section 10 and the Claude review 2 on PR #9 (findings 3, 4 and 5). Goal: the pure generator gives at least 24 of 30 waves within 20% of the target cost on Forest belts; stop after 3 runs.

Test target (a test only, not the game design): 30 waves, `target_cost(n) = 250 * 1.13^(n-1)` rounded to the nearest 25; every 5th wave (5, 10, 15, 20, 25, 30) is a relief wave (x 0.8). Written to `tools/balance/target_costs.csv` for the runs and deleted before the PR.

## Result: pass in 3 runs

**Run 3 (Balance run 45926887097): 25 of 30 waves within 20% of the target on Forest belts, 0 not held, mean error 14.4% — verdict pass** (gate: at least 24 of 30, no wave not held). Stop rule reached (3 runs).

| Run | Balance run | Generator result (within 20% of target, Forest belts) | Mean error | Not held | Change before the run |
| --- | --- | --- | --- | --- | --- |
| 1 | 45925461270 | 22 of 30 | 21.0% | 0 | waves 1-3 infantry rule + `targets` argument, PR #9 model; run also measured 75 armour probe waves |
| 2 | 45926402183 | 22 of 30 | 19.4% | 0 | armour probes (mixed-train4) + run 1 waves added to the training set, model refit |
| 3 | 45926887097 | **25 of 30 (pass)** | **14.4%** | 0 | run 2 waves added to the training set, model refit again |

Per Claude review 2 on PR #9, finding 2: the count of waves within 20% is noisy (PR #9: 10, 23, 19 on three refits), so the mean error is reported next to it; here it fell 21.0% -> 19.4% -> 14.4% while the count went 22 -> 22 -> 25.

## What changed against PR #9 (the three findings of the review)

1. **Finding 4, wave 2 (small waves).** `make_waves.py` now has a rule for waves 1-3: at least `MIN_EARLY_INFANTRY = 4` infantry (units of type `troops`). Reason, measured: a wave of fewer infantry leaks through the line and is "held" at the 25 minimum budget (run 45916087929, wave 2: 2 regular measured 25), while 4 infantry is the first held count (recruit x4 = 200 on Forest belts). Result: waves 1-3 measured +20.0%, +9.1%, -7.7% in run 3; wave 2 was -90.9% in PR #9.
2. **Finding 5, wave 21 (first armour).** 75 probe waves with armour and little infantry were measured in run 1 (waves 31-105 of `waves.csv`, made by `make_probe_waves_armour.py`, kept as `probe_waves_armour.csv`) and added to the training set (tag `mixed-train4`, waves 20001-20075): 1-3 old_tank / ifv / mrap / modern_tank plus 1-4 recruit or regular, armour plus special forces or light vehicles, armour pairs with little or no infantry, one helicopter with a small escort. After the refit, waves 21-23 measured +1.7%, +13.1%, +16.3% in run 3 (they were +93.9% in PR #9 run 4 and +92%/+102% in run 1 of this task with the old model).
3. **Finding 3, the generator cannot run from a clean branch.** `make_waves.py` takes an optional `targets` argument: a comma-separated list of target costs, `make_waves.py <units.csv> <out> [waves=30] [seed=7] [targets=250,275,...]` (options can also be positional). With `targets` the file `tools/balance/target_costs.csv` is not read and the list sets the wave count when `waves` is not given. Checked locally: the `targets` argument gives byte-identical waves to the file; seed 7 twice gives identical output; `waves=12 seed=3` works; a wrong-length list exits with an error. The endless mode can pass the target costs directly, without a file.
The model and the training set moved to `docs/balance/mistral/wave-generator-2/` (`model.json` is the refit that generated run 3, so the committed code reproduces the pass; `docs/balance/mistral/wave-generator/` is untouched).

## Method

1. **Pure generator, no plan** (unchanged from PR #9): for every wave count N and every seed, a randomised local search over wave compositions drives the model prediction to the target cost of the wave, keeping the task rules (caps, unlock order of first appearances, 2+ types from wave 4, no type above 70% of the predicted cost from wave 8, predicted cost at most 1.35x the previous wave except at a first appearance) plus the new waves 1-3 infantry rule. The same seed gives the same waves.
2. **Refit loop (generate -> measure -> add -> fit -> generate).** The waves measured in each run are added to the training set (`add_run_to_training.py`: run 1 waves 10121-10150, run 2 waves 10151-10180, tag `mixed-train3`; the armour probes 20001-20075, tag `mixed-train4`) and the model is refit (`fit_model.py`, held-out test waves unchanged). The run 3 waves and costs are kept as test data only (`waves_run3.csv`, `run3_wave_cost.csv`), not added to the training set, so the committed `model.json` reproduces run 3.
3. **Model metrics of the committed refit** (`metrics.json`): held-out probe waves pooled 13.2% mean error, 80.3% within 20% (Forest belts 13.8% / 78.8%). Out-of-sample on the run 3 waves (`eval_model.py`, Forest belts): 17.1% mean error, 21 of 30 within 20%.

## Final check table (run 3, 45926887097, Forest belts, the gate field)

| Wave | Target | Measured | Error |
| --- | --- | --- | --- |
| 1 | 250 | 300 | +20.0% |
| 2 | 275 | 300 | +9.1% |
| 3 | 325 | 300 | -7.7% |
| 4 | 350 | 300 | -14.3% |
| 5 | 325 | 300 | -7.7% (relief wave) |
| 6 | 450 | 400 | -11.1% |
| 7 | 525 | 600 | +14.3% |
| 8 | 600 | 500 | -16.7% |
| 9 | 675 | 600 | -11.1% |
| 10 | 600 | 500 | -16.7% (relief wave) |
| 11 | 850 | 700 | -17.6% |
| 12 | 950 | 600 | -36.8% |
| 13 | 1075 | 700 | -34.9% |
| 14 | 1225 | 900 | -26.5% |
| 15 | 1100 | 1000 | -9.1% (relief wave) |
| 16 | 1575 | 1500 | -4.8% |
| 17 | 1775 | 1800 | +1.4% |
| 18 | 2000 | 2200 | +10.0% |
| 19 | 2250 | 1800 | -20.0% |
| 20 | 2050 | 1700 | -17.1% (relief wave) |
| 21 | 2875 | 2925 | +1.7% |
| 22 | 3250 | 3675 | +13.1% |
| 23 | 3675 | 4275 | +16.3% |
| 24 | 4150 | 3675 | -11.4% |
| 25 | 3750 | 2875 | -23.3% (relief wave) |
| 26 | 5300 | 4525 | -14.6% |
| 27 | 6000 | 4475 | -25.4% |
| 28 | 6775 | 6275 | -7.4% |
| 29 | 7650 | 7275 | -4.9% |
| 30 | 6925 | 7375 | +6.5% (relief wave) |

Chart: `target-vs-measured.png` (run 3: target cost in grey, measured cost in green, misses in red).

Measured cost of the same run 3 waves on the other two fields (NaN = not held with 12000): Balka and railway 200, 200, 200, 200, 200 ... 700, 900, 900, 1000, 1500, 1800, 2200, 1800, 1700, NaN, 3575, 5025, NaN, NaN, NaN, NaN, NaN, NaN, NaN, NaN; River crossing 200, 200, 200, 200, 200 ... 2500, 600, 900, 1000, 1000, 2700, 3600, 5200, 4600, 5000, 3525, 7825, 7025, 4125, 6425, 4525, 5225, 8275, NaN, 8925. As in PR #9, the late armour waves are not held at 12000 on Balka and railway; the gate field is Forest belts.

## Misses and causes (run 3)

- Waves 12-14 (-36.8%, -34.9%, -26.5%): motorbike/van/regular escort mixes cost less than the model predicts — the same "escorts above a threshold add no cost" finding as PR #9 (waves 8, 11 there).
- Waves 25, 27 (-23.3%, -25.4%): relief wave 25 and the heavy old_tank wave 27 cost less than predicted; the model overprices some armour-plus-many-escort mixes.
## Open points

- Helicopter never appears: its cost (6375 for one unit) is above every wave target except 28-30, and the 1.35 growth limit blocks a first appearance there. A balance question for Claude and Yaroslav, not for this task (same note as PR #9).
- The run 1 report job lost its artifact upload because `tools/balance/run.json` (playtest_seeds 0) makes the workflow cat a missing playtest.csv; the wave costs were read from the run log instead. `run.json` was removed from run 2 on and is not in this PR.
- One shard of run 1 (River crossing, waves 89-90) failed on an apt-get runner error; those two costs are missing on that field only.
- `tools/balance/target_costs.csv` and the generated `unity/Assets/Data/waves.csv` were used for the runs and are removed from the branch (dev files only); the production `waves.csv` is restored.
- For the endless mode: call the generator with the `targets` argument (see above); Claude ports the method to C#.
