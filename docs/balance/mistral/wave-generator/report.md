# Mistral wave-generator report

Task: `mistral/wave-generator` (see `docs/agents/mistral.md`, section 10).
Goal: `tools/balance/make_waves.py` generates 30 waves. The measured cost of each wave must follow the target cost `target(n) = 250 * 1.13^(n-1)`, rounded to the nearest 25. Waves 5, 10, 15, 20, 25 and 30 are relief waves (target times 0.8).

## Result

Balance run 45867645128 (branch `mistral/wave-generator`, second run, verdict `pass`):

- 27 of 30 waves within 20% of the target cost (gate: at least 24).
- 0 waves "not held" (gate: 0).
- Mean error: 11.1%.

Chart: `target-vs-measured.png` (target cost in grey, measured cost in green, misses in red).

## Method

1. Iteration 1 (run 45865138318): `make_waves.py` used a randomised local search over wave compositions. The search drove the model prediction to the target. Result: 18 of 30 waves within 20%, mean error 33%. The model overpriced small waves (wave 4: predicted 345, measured 25) and underpriced armour-heavy waves 21 to 23 (measured 2.0 to 2.5 times the prediction).
2. Iteration 2 (run 45867645128): the script now uses a reviewed plan (`PLAN` in `make_waves.py`). Waves with a composition measured in run 1 reuse that exact composition. The other waves use compositions of the same families. The plan keeps all task rules: unit caps, unlock order of first appearances (motorbike wave 4, buggy wave 5, van wave 6, special_forces wave 8, old_tank wave 21, ifv wave 22, mrap wave 23, modern_tank wave 24), at least 2 unit types from wave 4, no type above 70% of the predicted cost from wave 8, predicted cost at most 1.35 times the previous wave except at a first appearance. The local search stays in the script as a fallback.

## Waves

| Wave | Target | Measured | Error |
| --- | --- | --- | --- |
| 1 | 250 | 200 | -20.0% |
| 2 | 275 | 300 | +9.1% |
| 3 | 325 | 300 | -7.7% |
| 4 | 350 | 300 | -14.3% |
| 5 | 325 | 325 | 0.0% |
| 6 | 450 | 500 | +11.1% |
| 7 | 525 | 500 | -4.8% |
| 8 | 600 | 500 | -16.7% |
| 9 | 675 | 400 | -40.7% |
| 10 | 600 | 600 | 0.0% |
| 11 | 850 | 1000 | +17.6% |
| 12 | 950 | 1100 | +15.8% |
| 13 | 1075 | 1100 | +2.3% |
| 14 | 1225 | 1300 | +6.1% |
| 15 | 1100 | 1100 | 0.0% |
| 16 | 1575 | 1400 | -11.1% |
| 17 | 1775 | 2300 | +29.6% |
| 18 | 2000 | 2000 | 0.0% |
| 19 | 2250 | 2300 | +2.2% |
| 20 | 2050 | 2300 | +12.2% |
| 21 | 2875 | 3125 | +8.7% |
| 22 | 3250 | 2325 | -28.5% |
| 23 | 3675 | 3325 | -9.5% |
| 24 | 4150 | 4725 | +13.9% |
| 25 | 3750 | 4375 | +16.7% |
| 26 | 5300 | 5625 | +6.1% |
| 27 | 6000 | 5625 | -6.2% |
| 28 | 6775 | 6625 | -2.2% |
| 29 | 7650 | 8275 | +8.2% |
| 30 | 6925 | 7225 | +4.3% |

Errors of the run 2 artifact report. Positive error means the wave cost more than the target.

## Misses and causes

- Wave 9 (-40.7%): the composition (`6 motorbike, 5 regular, 1 buggy`) came from the same family as wave 6 (measured 500), but the extra regular squads did not add cost. Escorts above a threshold do not make the wave harder.
- Wave 17 (+29.6%): the composition is near wave 18 (measured 2000), but one extra van and one extra recruit raised the measured cost from 2000 to 2300. The cost of this family moves in steps of about 300.
- Wave 22 (-28.5%): `1 old_tank, 1 ifv` alone costs less than the armour-only run of the attacker-cost task showed, because the defender has no infantry pressure. A cheaper pair was not available: the next lower armour pair measures far under the target.

## Open points

- Helicopter never appears: its cost (6375 for one unit) is above every wave target except 28 to 30, and the 1.35 growth limit blocks a first appearance there.
- The plan depends on measured costs from run 1. A change to the game or to the units changes the measured costs, and the plan needs a new calibration run.
