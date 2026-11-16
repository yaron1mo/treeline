# Economy test: kill rewards 50% vs 25% over 20 waves (2026-11-16)

Yaroslav: rifle squad = 100 is the anchor; test two reward levels, paid after each held wave: A = half of each kill's
threat value, B = a quarter. 20 waves that grow step by step.

## How the numbers were made

1. **How many of each attacker one defender stops** (`stop.csv`): every defender against every attacker, 1, 2, 3, 5,
   8, 12 and 20 of them 1 s apart, defender in a trench in a belt 25 m beside the route, recon for mortar, ATGM, air
   defence and drone operators, 30 seeds (`capacity-runs.csv`). "Stopped" = line damage prevented / the attacker's own
   line damage (a carrier counts with its troops).
2. **Threat value** (`units.csv` column `threat_value`) = the cheapest defender's cost per unit it stops, with today's
   costs, rounded: recruit 10, regular 15, special forces 80, motorbike 10, buggy 15, van 30, old tank 90, modern tank
   120, IFV 150, MRAP 80, enemy FPV 140, helicopter 600. Not measured, set by hand: recon UAV 50, one-way drone 200.
   Machine gunner and sniper stopped all 20 recruits/regulars, so infantry values are an upper bound.
3. **Waves** (`waves-20.csv`, `tools/balance/make_waves.py`): wave 1 is worth 50 (5 recruits), each next one 22% more;
   new enemies in order: regular 2, motorbike 3, buggy 4, van 5, special forces 6, old tank 7, IFV 8, MRAP 9, FPV 10,
   modern tank 12, helicopter 14, one-way drone 16. Wave 20 is worth about 2,200.
4. **Money**: start 250 (two rifle squads and change), +100 per held wave, plus the kill rewards of the wave.
5. **Scripted players** (`tools/balance/PlaytestRun.cs`), 10 runs per field, 3 fields:
   - saver: covers each entry point, buys what this and the next wave need (from `stop.csv`, times 2), keeps the rest;
   - greedy: the same, then spends everything on machine gunners and rifle squads;
   - reactive: places nothing before the wave, then covers each lane the enemy actually uses (defenders run in).

## Results: mean waves held (best run)

| Line health | Variant | Saver | Greedy | Reactive |
|---|---|---|---|---|
| 20 (today) | A, 50% | 6.6 (20) | 6.0 (7) | 6.1 (8) |
| 20 | B, 25% | 5.2 (7) | 5.9 (7) | 5.9 (7) |
| 60 | A, 50% | 13.8 (20) | 8.9 (11) | 11.0 (16) |
| 60 | B, 25% | 11.4 (17) | 8.7 (10) | 10.3 (14) |

- With line health 20 no style gets far in either variant: small leaks in the first waves add up over 20 waves, and the
  first van (wave 5, 9 line damage if it gets through) or the first tank (wave 7) ends the run.
- With line health 60 variant A gives about what we wanted: a careful player holds 13-14 waves, some runs reach 20;
  B is about 2-3 waves harder.
- Saving pays: the saver beats the greedy player in both variants, more so with bigger rewards.
- Reactive (wait, see the lane, then place) is between the two.

The scripted players are crude (random spots, no moving or selling), so absolute numbers say less than the comparison.
