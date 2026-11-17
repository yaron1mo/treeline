# Wave cost, 20-wave playtest (2026-11-17)

Tool: `tools/balance/WaveCost.cs`. Stop table: `stop.csv`.

## Method

1. The tool fights each wave alone, on an empty field, with no memory of earlier waves.
2. A scripted player buys defenders by need from the stop table, in proportion, until the budget ends.
3. A wave is held when the line takes 3 damage or less (`line_health` 60 / 20 waves).
4. The cost is the smallest budget, to 25 points, that holds the wave on 8 of 10 seeds.

The cost is an upper bound. The scripted player plays worse than a person.

## Results

`summary.txt` compares the cost with the money a player has before each wave.
The money is 250 at the start, plus 100 and 25% of the kill rewards after each held wave.

Use the Forest belts field for decisions. That field has 2 entry points, so the scripted player knows where to stand.
The Balka and River fields have no `spawns` list. The scripted player puts all defenders in the middle.
Their costs show the scripted player, not the waves. `nan` means not held with 12000.

## Findings (Forest belts)

1. The real cost is 3 to 10 times the sum of `threat_value`. `threat_value` is a marginal cost at scale. A single attacker costs a whole defender.
2. Waves 1-6: the money is enough, except wave 1 (cost 300, money 250).
3. Wave 7, the first old tank: the cost goes from 400 to 1675. One tank needs an anti-tank defender (450) and a recon drone at each entry point.
4. From wave 7, the cost is more than the money on most waves (money / cost 0.45-0.9). Wave 14 (helicopter) is the exception.
5. Waves 15-20 need about 2 times the money a player has.
