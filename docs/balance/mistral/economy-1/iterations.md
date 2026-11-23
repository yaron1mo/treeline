# Mistral economy-1: balance iterations

Task: all 20 waves in the target curve on Forest belts (see `docs/agents/mistral.md`, section 8).

| Iteration | Run | Change | Hypothesis | Verdict | Waves in target | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 45280633253 | None. Baseline run | The baseline with the new scripted player (2026-11-23) stays near the 2026-11-23 balance run: wave 1 and wave 7 (first old tank) are too hard, waves 15-20 are too hard. This run gives the real start point | fail | 2 of 20 | Waves 3 and 4 only. Waves 1, 2, 5 marginally too hard; wave 6 too easy (ratio 2.83, max 1.4); wave 7 ratio 0.42 (first old tank, cost 2325); waves 8-20 all too hard (0.44-1.04), wave 19 worst (cost 9025). Cost is 3-10 times the threat sum. |
| 2 | tbd | `start_budget` 250 to 400, `wave_income` 100 to 150, `reward_value` of the attackers set to 5 times `threat_value` (helicopter 3000, ifv 750, old_tank 450) | The money curve must grow with the wave costs. A reward near the marginal cost of the attacker (about 5 times threat, from the 2026-11-17 measure) plus a higher start and income moves most waves into the target. This run shows which waves stay out, so the next iteration can set the wave counts | tbd | tbd | tbd |
