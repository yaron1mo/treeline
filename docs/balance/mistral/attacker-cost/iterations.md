# Mistral attacker-cost: model iterations

Task: find what each attacker costs the defence and a formula for the cost of any wave (see `docs/agents/mistral.md`, section 9).

| Iteration | Run | Change | Hypothesis | Verdict | Waves in target | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 45401299305 | `tools/balance/run.json` set to the probe waves (max budget 30000, 0 playtest seeds); no other change | The probe set of 443 waves (310 train, 40 test) gives a curve for every attacker type, the two models A and B, and the map coefficients | report only | 443 waves measured | Forest belts: all 443 measured. Balka and railway: 190 of 391 waves are "not held" at 30000; every wave with armour but no infantry fails, waves with the same armour plus infantry are held (for example triple special_forces+old_tank+van costs 8525). River crossing: only helicopter waves fail (15, all with a helicopter). Mixed-wave error of model A: 24% Forest, 43% Balka, 40% River; within 20%: 62/41/41 percent. Heavy types have single points only at n=4,6,8, so their entry cost is not measured; mraps saturate at 1925 on Forest belts. |
