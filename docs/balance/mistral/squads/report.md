# Mistral squads - which squads hold which waves

Task: `docs/agents/mistral.md` section 11 (`mistral/squads`), issue #23. Date: 2026-11-27. No game value was changed. The deliverable is the measurement and a proposal.

## Method

- Tool: `WaveCost.cs` squad mode (`SQUADS_FILE`). For each squad the scripted player can buy only the defender types of that squad. `cost` is `NaN` when the squad cannot hold the wave with `max_budget` 12000. Each wave is fought alone on an empty field; the cost is the smallest budget (step 25) that holds the wave on at least 8 of 10 seeds.
- Wave set: `tools/balance/squad_waves.csv` - 40 mixed probe waves of the attacker-cost task (PR #7, run 45411511647) with a measured cost from 500 to 6000 on Forest belts, renumbered 1-40. The set covers all 11 attacker types of the probe waves (recruit, regular, special_forces, motorbike, buggy, van, mrap, ifv, old_tank, modern_tank, helicopter). 28 of 40 waves contain armour, 25 contain vehicles, all 40 contain infantry, 1 contains a helicopter. The probe set has no recon_uav, fpv or one_way_drone wave in this cost band, so the set has no drone-attacker wave (see Strange results).
- Squads: `make_squads.py` writes every squad of N types from the 9 defence types, plus one squad `all` with all 9. N = 4 (run 1), 3 (run 2), 5 (run 3). A squad name is the short type names joined by `+`, for example `rs+mg+at+rd`.
- Runs (Balance run on branch `mistral/squads`, `run.json`: waves_file `squad_waves.csv`, squads_file `squads.csv`, max_budget 12000, playtest_seeds 0):

| Run | Slots | Fields | Rows | Held |
| --- | --- | --- | --- | --- |
| 45928428025 | 4 | all 3 fields | 15240 | 9625 |
| 46048257867 | 3 | Forest belts | 3400 | 2039 |
| 46053203953 | 5 | Forest belts | 5080 | 4076 |

Run 1 took 1 h 45 min, so runs 2-3 measured only Forest belts (the gate field). Run 3 lost one shard (wave 5) to an infrastructure error ("Install Mono runtime" failed after 17 s, no tool error); the job was rerun and the 127 wave-5 rows were merged into the analysis by hand.
- Analysis: `analyze.py` reads the merged `wave-cost.csv` and writes `squads.json` (4 slots, all fields), `squads-s3.json`, `squads-s5.json`. Per squad and field: held share (share of the 40 waves the squad holds), cost premium (median of squad cost / cost of `all` on the waves the squad holds), rank (waves where the squad is the cheapest that holds). Per defender type: must-have (share of waves that no squad without the type holds), unused (share of waves where no squad in the best 10 has the type). A squad is dominant when held share >= 0.90 and premium <= 1.2.

## Results

### Best 15 squads of 4 types, per field

Note: the cost premium is the median over the waves the squad holds, so a squad with held share below 1.00 can show a premium below 1 (example: sn+at+bo+rd on River crossing, held 0.88, premium 0.831). The list is sorted by rank first.

### Field: Balka and railway - best 15 squads of 4 types
| Squad | Held share | Cost premium | Rank |
| --- | --- | --- | --- |
| all | 0.70 | 1.000 | 14 |
| rs+mg+at+rd | 0.70 | 1.000 | 14 |
| sn+at+ad+bo | 0.60 | 1.091 | 9 |
| rs+mg+at+ad | 0.80 | 1.036 | 8 |
| rs+mg+at+bo | 0.80 | 1.036 | 8 |
| rs+mg+at+fpv | 0.80 | 1.036 | 8 |
| rs+mg+mo+at | 0.80 | 1.036 | 8 |
| rs+mg+sn+at | 0.80 | 1.036 | 8 |
| sn+at+ad+fpv | 0.60 | 1.091 | 8 |
| sn+at+fpv+bo | 0.60 | 1.091 | 8 |
| rs+mg+fpv+rd | 0.78 | 1.074 | 7 |
| rs+mg+ad+fpv | 0.72 | 1.181 | 7 |
| rs+mg+fpv+bo | 0.72 | 1.181 | 7 |
| rs+mg+mo+fpv | 0.72 | 1.181 | 7 |
| rs+mg+sn+fpv | 0.72 | 1.181 | 7 |

Dominant squads of 4 (held >= 0.90, premium <= 1.2): 0

| Type | Must-have | Unused |
| --- | --- | --- |
| air_defence | 0.00 | 0.00 |
| anti_tank | 0.00 | 0.05 |
| bomber_operator | 0.00 | 0.00 |
| fpv_operator | 0.05 | 0.00 |
| machine_gun | 0.00 | 0.35 |
| mortar | 0.00 | 0.10 |
| recon_drone | 0.03 | 0.10 |
| rifle_squad | 0.05 | 0.17 |
| sniper | 0.00 | 0.23 |

### Field: River crossing - best 15 squads of 4 types
| Squad | Held share | Cost premium | Rank |
| --- | --- | --- | --- |
| sn+at+bo+rd | 0.88 | 0.831 | 14 |
| sn+at+ad+rd | 0.88 | 0.831 | 13 |
| sn+at+fpv+rd | 0.88 | 0.852 | 12 |
| sn+mo+at+rd | 0.93 | 0.852 | 10 |
| sn+at+fpv+bo | 0.90 | 0.977 | 10 |
| sn+at+ad+bo | 0.90 | 0.898 | 9 |
| sn+at+ad+fpv | 0.90 | 0.977 | 9 |
| all | 1.00 | 1.000 | 8 |
| rs+mg+at+rd | 1.00 | 1.000 | 8 |
| rs+sn+at+rd | 0.90 | 0.767 | 8 |
| sn+mo+at+fpv | 0.95 | 0.977 | 7 |
| rs+mg+at+bo | 0.95 | 1.145 | 7 |
| rs+mg+at+fpv | 0.95 | 1.145 | 7 |
| rs+mg+mo+at | 0.95 | 1.145 | 7 |
| rs+mg+sn+at | 0.95 | 1.145 | 7 |

Dominant squads of 4 (held >= 0.90, premium <= 1.2): 27

| Type | Must-have | Unused |
| --- | --- | --- |
| air_defence | 0.00 | 0.03 |
| anti_tank | 0.35 | 0.00 |
| bomber_operator | 0.00 | 0.00 |
| fpv_operator | 0.00 | 0.03 |
| machine_gun | 0.00 | 0.47 |
| mortar | 0.00 | 0.12 |
| recon_drone | 0.00 | 0.05 |
| rifle_squad | 0.00 | 0.12 |
| sniper | 0.00 | 0.00 |

### Field: Forest belts - best 15 squads of 4 types
| Squad | Held share | Cost premium | Rank |
| --- | --- | --- | --- |
| rs+mg+at+rd | 1.00 | 1.000 | 19 |
| all | 1.00 | 1.000 | 18 |
| mg+at+bo+rd | 1.00 | 1.000 | 11 |
| mg+at+fpv+rd | 1.00 | 1.000 | 11 |
| mg+mo+at+rd | 1.00 | 1.000 | 11 |
| mg+sn+at+rd | 1.00 | 1.000 | 11 |
| rs+mg+fpv+rd | 0.75 | 1.000 | 11 |
| mg+at+ad+rd | 1.00 | 1.000 | 10 |
| rs+mg+at+bo | 1.00 | 1.042 | 9 |
| rs+mg+at+fpv | 1.00 | 1.042 | 9 |
| rs+mg+mo+at | 1.00 | 1.042 | 9 |
| rs+mg+sn+at | 1.00 | 1.042 | 9 |
| rs+mg+at+ad | 1.00 | 1.051 | 8 |
| rs+mg+fpv+bo | 0.70 | 1.296 | 8 |
| rs+mg+mo+fpv | 0.70 | 1.296 | 8 |

Dominant squads of 4 (held >= 0.90, premium <= 1.2): 23

| Type | Must-have | Unused |
| --- | --- | --- |
| air_defence | 0.00 | 0.03 |
| anti_tank | 0.10 | 0.00 |
| bomber_operator | 0.00 | 0.00 |
| fpv_operator | 0.00 | 0.00 |
| machine_gun | 0.00 | 0.17 |
| mortar | 0.00 | 0.10 |
| recon_drone | 0.00 | 0.05 |
| rifle_squad | 0.00 | 0.07 |
| sniper | 0.00 | 0.23 |


### Which squad holds which wave (Forest belts, 4 slots, 12000)

| Wave | Content | Cost of all | Cheapest holding squad | Its cost | Premium |
| --- | --- | --- | --- | --- | --- |
| 1 | ifv x1, regular x3 | 2025 | rs+at+ad+rd | 1775 | 0.877 |
| 2 | buggy x1, motorbike x2, recruit x5, van x1 | 300 | all | 300 | 1.000 |
| 3 | buggy x1, motorbike x1, recruit x6, van x1 | 500 | all | 500 | 1.000 |
| 4 | buggy x1, ifv x1, regular x2 | 1875 | sn+at+ad+bo | 1800 | 0.960 |
| 5 | motorbike x3, old_tank x1, regular x3 | 2075 | rs+mg+fpv+rd | 1475 | 0.711 |
| 6 | modern_tank x1, regular x16, special_forces x1 | 2075 | all | 2075 | 1.000 |
| 7 | buggy x5, modern_tank x1, regular x17 | 2475 | all | 2475 | 1.000 |
| 8 | modern_tank x2, motorbike x10, mrap x1, regular x26 | 4625 | all | 4625 | 1.000 |
| 9 | ifv x2, modern_tank x3, old_tank x1, regular x30, special_forces x3 | 5525 | mg+at+ad+rd | 5325 | 0.964 |
| 10 | buggy x1, recruit x5, regular x1, van x1 | 500 | all | 500 | 1.000 |
| 11 | buggy x1, recruit x5, regular x1, van x1 | 500 | all | 500 | 1.000 |
| 12 | mrap x1, regular x13, van x1 | 1325 | all | 1325 | 1.000 |
| 13 | buggy x1, recruit x5, regular x1, van x1 | 500 | all | 500 | 1.000 |
| 14 | motorbike x2, old_tank x1, regular x4 | 2325 | rs+at+ad+bo | 1600 | 0.688 |
| 15 | motorbike x2, recruit x5, regular x1, van x1 | 500 | all | 500 | 1.000 |
| 16 | regular x20 | 1600 | rs+sn+ad+bo | 1100 | 0.688 |
| 17 | regular x4, special_forces x1 | 500 | all | 500 | 1.000 |
| 18 | modern_tank x2, old_tank x2, regular x26 | 4675 | all | 4675 | 1.000 |
| 19 | modern_tank x1, motorbike x6, old_tank x1, regular x12 | 3575 | all | 3575 | 1.000 |
| 20 | ifv x1, motorbike x1, regular x3 | 1875 | rs+at+ad+bo | 1600 | 0.853 |
| 21 | modern_tank x1, mrap x1, regular x16 | 2825 | all | 2825 | 1.000 |
| 22 | mrap x1, regular x15 | 1225 | all | 1225 | 1.000 |
| 23 | helicopter x1, recruit x10 | 2875 | mg+at+bo+rd | 1200 | 0.417 |
| 24 | old_tank x1, regular x7, special_forces x1, van x1 | 3175 | rs+mo+fpv+rd | 2125 | 0.669 |
| 25 | motorbike x3, old_tank x1, regular x3 | 2075 | rs+mg+fpv+rd | 1475 | 0.711 |
| 26 | mrap x2, old_tank x4, regular x30, special_forces x2, van x10 | 5325 | rs+sn+at+rd | 4875 | 0.915 |
| 27 | old_tank x1, regular x18 | 2975 | mg+ad+fpv+rd | 2375 | 0.798 |
| 28 | buggy x3, mrap x1, regular x8 | 1125 | rs+at+ad+bo | 1050 | 0.933 |
| 29 | modern_tank x2, motorbike x10, old_tank x1, regular x30, special_forces x1 | 4175 | all | 4175 | 1.000 |
| 30 | buggy x8, regular x18, special_forces x2 | 1800 | rs+sn+ad+bo | 1600 | 0.889 |
| 31 | modern_tank x1, mrap x1, regular x23 | 2725 | all | 2725 | 1.000 |
| 32 | ifv x1, regular x3 | 2025 | rs+at+ad+rd | 1775 | 0.877 |
| 33 | modern_tank x1, motorbike x9, old_tank x1, regular x10 | 3025 | mg+at+ad+bo | 2950 | 0.975 |
| 34 | buggy x1, old_tank x1, regular x4 | 2125 | rs+at+fpv+rd | 1425 | 0.671 |
| 35 | motorbike x4, mrap x1, regular x8 | 1225 | rs+at+ad+bo | 1050 | 0.857 |
| 36 | buggy x3, mrap x1, regular x8 | 1125 | rs+at+ad+bo | 1050 | 0.933 |
| 37 | regular x15, special_forces x1 | 1500 | rs+ad+bo+rd | 1100 | 0.733 |
| 38 | motorbike x9, regular x10, van x4 | 1200 | mg+ad+bo+rd | 1000 | 0.833 |
| 39 | old_tank x1, regular x5 | 2325 | rs+at+ad+rd | 1675 | 0.720 |
| 40 | modern_tank x2, old_tank x1, regular x30 | 3675 | all | 3675 | 1.000 |

### 3 vs 4 vs 5 slots (Forest belts)

| Slots | Best squad | Held share | Premium | Dominant squads |
| --- | --- | --- | --- | --- |
| 3 | mg+at+rd | 1.00 | 1.000 | 9 |
| 4 | rs+mg+at+rd | 1.00 | 1.000 | 23 |
| 5 | rs+mg+at+bo+rd | 1.00 | 1.000 | 36 |

The best squad of each size holds all 40 waves at premium 1.000 (rank 11 for 3 slots, 19 for 4 and 5 slots; the dominant counts include `all`). The slot count changes how many squads are dominant (9 -> 23 -> 36) and how often the cheapest squad needs the type, not the held share: in the 500-6000 cost band on Forest belts, even 3 well-chosen types hold everything at 12000.

### Target check (definitions of the findings)

| Check | Target | Result |
| --- | --- | --- |
| Dominant squads of 4 types | none | FAIL: 23 on Forest belts, 27 on River crossing, 0 on Balka and railway. Every dominant squad contains anti_tank |
| Each type in the best 10 for at least 20% of the waves | yes | met on all fields (worst unused share 0.475, machine_gun on River crossing) |
| Type must-have on more than 50% of the waves | none | met (worst anti_tank 0.35 on River crossing, 0.10 on Forest belts) |
| Type unused on 90% of the waves or more | none | met (worst 0.475) |

## Charts

- `held-vs-premium-<field>.png`: held share against cost premium, one point per squad, 4 slots, all 3 fields; `held-vs-premium-forest-belts-3slots.png` and `-5slots.png` for the other slot counts.
- `heat-<field>.png`: defender type against attacker category (share of the waves with that attacker where the type is in the best 10), 4 slots; `heat-forest-belts-3slots.png` and `-5slots.png`.

## Strange results

1. Every dominant squad contains anti_tank. Reason in the data: `tools/balance/stop.csv` gives anti_tank the only strong values against all four armour types (mrap 5.95, old_tank 5.10, ifv 2.98, modern_tank 3.77); fpv_operator is the only other type above 0 (0.18-2.43); all other defenders are 0.00 against armour. 28 of 40 waves contain armour.
2. air_defence is in the best 10 for 0% of the helicopter waves on every field. The one helicopter wave (wave 23: helicopter x1 + recruit x10) is held cheapest by mg+at+bo+rd at 1200, while `all` pays 2875 (premium 0.417). The stop table gives air_defence 0.80 vs helicopter, but machine_gun 0.25, sniper 0.22 and rifle_squad 0.10 also damage it, and the scripted player kills the dismounts. A 500-cost air defence gun is never the cheapest answer in this wave set.
3. No drone-attacker wave is in the 500-6000 band of the probe set, so the anti-drone values (air_defence vs fpv 2.43, machine_gun vs fpv 1.43, fpv_operator vs fpv 0.23) are not measured at all.
4. On Balka and railway, 12 of 40 waves (8, 9, 18, 19, 21, 23, 28, 31, 33, 35, 36, 40) are not held even by `all` at 12000, and wave 23 is held by no 4-type squad. These waves cost more than 12000 on that open field.
5. rifle_squad (build_cost 100) covers infantry almost as well as machine_gun (200): stop vs regular 4.37 vs 20.00, but the scripted player buys several rifle squads for the price of one machine gun, so machine_gun is the most unused type on River crossing (0.475).

## Proposal (not a change)

Goal: no dominant squad of 4, and every defender type has a measured role. Candidate values, with the reason for each:

1. Give armour a second counter, so that anti_tank is not in every dominant squad: `stop.csv` `fpv_operator,modern_tank` 1.00 -> 2.00 and `fpv_operator,old_tank` 2.43 -> 3.00. Reason: the FPV warhead is anti_tank damage (1.0 vs medium and 0.85 vs high armour in `unity/Assets/Data/armour.csv`), so the armour table already allows it; today anti_tank is the only type with stopped > 0 against all four armour rows.
2. Make machine_gun the clear anti-elite pick: `stop.csv` `machine_gun,special_forces` 2.43 -> 4.00 (rifle_squad 0.97 unchanged). Reason: machine_gun is the most unused type on River crossing (0.475); special_forces waves are the only waves where it clearly beats rifle_squad.
3. Make air_defence worth its 500 cost on helicopter waves: `stop.csv` `air_defence,helicopter` 0.80 -> 2.00, or add waves with 2+ helicopters to `squad_waves.csv`. Reason: air_defence is in the best 10 for 0% of helicopter waves; its unique roles (helicopter, fpv drones) are small or unmeasured in this band. The finding rests on the single helicopter wave of the set (wave 23), so adding 2+ helicopter waves should come first.
4. Before the squad design is final, add drone-attacker waves (recon_uav spotting plus fpv or one_way_drone) to `squad_waves.csv` in the 500-6000 band. Reason: no drone wave is in the set, so air_defence, fpv_operator and the recon-dependent guns have no measured role.
5. Sniper needs no change: it is in the best 10 for 77-100% of the waves on all fields (unused 0.15-0.225).

## Files

- `tools/balance/make_squads.py`, `tools/balance/squads.csv`, `tools/balance/squad_waves.csv` - inputs of the runs.
- `docs/balance/mistral/squads/` - `iterations.md` (journal), `analyze.py`, `charts.py`, `pngchart.py`, `squads.json` (4 slots), `squads-s3.json`, `squads-s5.json`, this report, 10 PNG charts.
- No change to `waves.csv`, `battle_rules.csv`, `units.csv`, `armour.csv`, `stop.csv` or any Sim code. `tools/balance/run.json` is removed before this pull request (as in PR #7), so a new `mistral/**` branch does not run the squad mode.

## Open questions

1. The 500-6000 cost band on Forest belts is holdable by 3 types, so the squad choice only shows in cost premium. Should the wave set go to 6000-12000, where held share starts to drop?
2. Should the next measurement add drone-attacker waves (proposal 4) before Yaroslav fixes the squad size?
