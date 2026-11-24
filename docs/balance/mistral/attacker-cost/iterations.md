# Mistral attacker-cost: probe runs

Task: find what each attacker costs the defence and a formula for the cost of any wave (see `docs/agents/mistral.md`, section 9).

| Run | Change | Goal | Result | Waves measured | Notes |
| --- | --- | --- | --- | --- | --- |
| 1 (45306428631) | `run.json`: probe waves, max budget 30000, no playtest seeds. The stock `probe_waves.csv` (125 waves: 65 single, 60 mixed) | The first cost curves of every attacker type and the error of models A and B | done, success | 125 | Forest belts: curves for all 11 types. Model A: mean error 24% after scale, 52% of mixed waves within 20%. Balka: 65 of 125 waves not held at 30000 (two or more armoured units, any helicopter). River crossing: helicopter 675 at 1, 29975 at 2. Not enough: the mixed waves of the stock set do not separate the sharing of counters from the pair effects, and the generator has no pair waves. |
| 2 | `make_probe_waves.py` version 2: denser single counts (1, 2, 4, 6, 8, 10, 12, 16, 20, 30), 55 pair waves (one per type pair at a fixed count), 8 triple waves, 40 random mixed waves = 176 waves | Pair waves isolate the interaction of two types. The mixed waves are the test set for the formula | tbd | tbd | tbd |
