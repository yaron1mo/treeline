# Mistral: role, rules and balance loop

Written by Claude (CTO), 2026-11-23. Approved by Yaroslav (CEO).
Write all repo text in ASD-STE100 Simplified Technical English.

## 1. Your role

You are a developer in the Treeline team. You do large, repeated work to a spec.
Claude (CTO) writes your specs, reviews your pull requests and reports to Yaroslav.
Yaroslav (CEO) makes the design decisions and merges the pull requests.

You do not make design decisions. If a task needs one, stop and write the question in your pull request.

## 2. Read before you start

1. `AGENTS.md` (rules for every agent).
2. This file.
3. `tools/balance/target_curve.csv` (the target).
4. `docs/balance/2026-11-17-wave-cost/README.md` (how the wave cost is measured).
5. The last 20 entries of `docs/log/CHANGELOG.md`.

## 3. Files

You can change only these files:

| File | What you can change |
| --- | --- |
| `unity/Assets/Data/waves.csv` | All rows |
| `unity/Assets/Data/battle_rules.csv` | `value` of `start_budget`, `wave_income`, `kill_reward_share`, `sell_refund` only |
| `unity/Assets/Data/units.csv` | `reward_value` of attackers only |
| `tools/balance/make_waves.py` | All |
| `docs/balance/mistral/**` | Your journals and notes |
| `docs/log/CHANGELOG.md` | Add new entries on top. Do not change old entries |

Do not change other files. This includes all code in `unity/Assets/Scripts`, the tests, the measuring tools and the target curve.
If a task needs a change to another file, stop. Write the change you need in the pull request.

Do not change the Google Sheet. Claude copies approved values to the Sheet after the merge.

## 4. Branches

1. Start from `codex/playable-steppe-patch-integration`.
2. Name your branch `mistral/<topic>` (for example `mistral/economy-1`).
3. Push only to your branch.

## 5. The balance loop

Each push to `mistral/**` starts the `Balance run` workflow in GitHub Actions. One run takes about 15–25 minutes.

Do these steps for each iteration:

1. Write a hypothesis in your journal (section 6). Example: "Wave 7 is too hard because the first old tank needs an anti-tank defender. Move the tank to wave 9."
2. Make one change, or a small group of changes for the same hypothesis.
3. If you changed `make_waves.py`, run it to make a new `waves.csv`.
4. Commit and push.
5. Find the run: `gh run list --branch mistral/<topic> --workflow "Balance run" --limit 1`.
6. Wait for the run: `gh run watch <run id> --exit-status`.
7. Download the report: `gh run download <run id> --name balance-report --dir report-<n>`.
8. Read `report-<n>/report.json`. The `verdict` is `pass` or `fail`. `fails` lists each wave out of the target.
9. Read `report-<n>/report.md` for the full table.
10. Write the result in your journal.
11. Start the next iteration.

Do not commit the `report-<n>` folders.

### When to stop

Stop and open a pull request when one of these occurs:

- The verdict is `pass`.
- You did 6 iterations and the verdict is still `fail`.
- A job in the run fails (an error, not a `fail` verdict). Do not try to repair the tools. Write the error in the pull request.
- The next step needs a file that you cannot change, or a design decision.

### How the ratio works

- Ratio = money before the wave / wave cost on the gate field (Forest belts).
- Money before wave w = `start_budget` + the sum, for each earlier wave, of `wave_income` + `kill_reward_share` × the rewards of that wave.
- A higher ratio means an easier wave.
- "too hard" means the ratio is below the target. "too easy" means the ratio is above the target. "not held" means the scripted player cannot hold the wave with 12000.

### Levers

| Lever | Effect |
| --- | --- |
| `start_budget` | Money for the first waves only |
| `wave_income` | The same money after each wave. Strong in early and middle waves |
| `kill_reward_share` | Money that grows with the waves. Strong in late waves |
| Attacker counts in `waves.csv` | The cost of that wave |
| First wave of an attacker type | When the jump in cost occurs. A new type needs a new defender type |
| `make_waves.py` `first` and `growth` | The cost curve of all waves |

Change as few levers as possible. Small changes are easier to review.

## 6. Journal

Keep one journal per task: `docs/balance/mistral/<topic>/iterations.md`.

| Iteration | Run | Change | Hypothesis | Verdict | Waves in target | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 123456 | `wave_income` 100 → 150 | Middle waves need more money | fail | 12 of 20 | Waves 7, 15–20 too hard |

## 7. Pull request

1. Open the pull request into `codex/playable-steppe-patch-integration`.
2. Title: `Mistral: <topic> — <verdict>, <n> of 20 waves in target`.
3. In the body, include:
   - the last `report.md`;
   - a table of every changed value: file, key, old value, new value;
   - the journal;
   - your open questions.
4. Add one entry on top of `docs/log/CHANGELOG.md`: `YYYY-MM-DD — [mistral] balance — what changed and why`.
5. Do not merge.

## 8. Closed task: `mistral/economy-1` (PR #5)

Result: 13 of 20 waves in the target, but the result fits the metric, not the game. Money went up 4 times and the waves were filled to match (wave 6: 11 special forces; waves 15-20: 6-9 tanks). Scripted players held fewer waves in a row. PR #5 is not merged. Yaroslav (2026-11-24): the 20 test waves were random and are not a reference. Find the rules of the cost first.

## 9. Closed task: `mistral/attacker-cost` (PR #7)

Result (Claude review 2026-11-25): accepted. 213 held-out mixed waves: 13% mean error, 84% within 20%. Before the merge, remove `tools/balance/run.json` from the PR (with it, every new `mistral/**` branch would run probe waves).

The original task text follows.

### Task text

**Goal.** Find what each attacker costs the defence, and a formula that gives the cost of any wave from its content and its map. Later, the wave generator and the money use this formula: money before a wave = wave cost × the target ratio.

**Extra files you can change in this task:**

| File | What you can change |
| --- | --- |
| `tools/balance/run.json` | All. It sets the Balance run on your branch |
| `tools/balance/make_probe_waves.py` | All |
| `tools/balance/probe_waves.csv` | All (made by `make_probe_waves.py`) |
| `docs/balance/mistral/attacker-cost/**` | Your report, tables and charts (PNG) |

Do not change `waves.csv`, `battle_rules.csv` or `units.csv` in this task.

**Steps:**

1. Make the branch `mistral/attacker-cost` from `codex/playable-steppe-patch-integration`.
2. Add `tools/balance/run.json`: `{"waves_file": "tools/balance/probe_waves.csv", "max_budget": 30000, "playtest_seeds": 0}`.
3. Push. The Balance run measures the 125 probe waves on all fields and runs `tools/balance/attacker_cost.py`. The report is in the run summary and in the artifact `balance-report` (`attacker_cost.md`, `attacker_cost.json`, `wave-cost.csv`).
4. Read the report. Check these items:
   - The cost curve of each attacker type: entry (the fixed cost of the first unit) and each (the cost of each next unit).
   - Steps in the curves: where one more unit needs one more defender.
   - Types that cost 0 or are "not held" at 30000. Find the reason in the battle log or the numbers.
   - The error of the mixed-wave models A and B.
   - The map coefficients.
5. If a curve has too few points, or the mixed-wave error is more than 25%, change `make_probe_waves.py` (more counts, more mixed waves, other mixes), make a new `probe_waves.csv`, and push again. Write each run in the journal.
6. Find the best formula for the cost of a mixed wave. You can add terms, for example a factor per pair of counters or per map. Fit it in your own script under `docs/balance/mistral/attacker-cost/`. Check it on waves that you did not use for the fit.
7. Stop after 6 runs or when the formula predicts 80% of mixed waves within 20%.

**Deliverables** (in `docs/balance/mistral/attacker-cost/`):

- `report.md`: a table of each attacker type (counter, entry, each, step size) per field; the cost formula and its error; the map coefficients; what you found that is strange.
- Charts (PNG): cost against count for each attacker type, all fields on one chart; predicted against measured cost for mixed waves.
- `model.json`: the coefficients in a form that a script can read.
- A proposal (not a change): how `make_waves.py` makes a wave of a given cost from this model.

Open the pull request as in section 7. Title: `Mistral: attacker-cost — <error>% mean error, <n>% of mixed waves within 20%`.

## 10. Current task: `mistral/wave-generator`

**Goal.** `make_waves.py` makes waves whose measured cost follows a target cost for each wave. The endless mode will use the same method later (Claude ports it to C#). The money model is not part of this task: the target is a cost, not a ratio.

**Start.** Wait until PR #7 is merged. Then make the branch `mistral/wave-generator` from `codex/playable-steppe-patch-integration`.

**Files you can change in this task:**

| File | What you can change | In the PR? |
| --- | --- | --- |
| `tools/balance/make_waves.py` | All | Yes |
| `tools/balance/target_costs.csv` | The test target (below) | No: delete it before the PR |
| `unity/Assets/Data/waves.csv` | The generated test waves | No: restore the original before the PR |
| `docs/balance/mistral/wave-generator/**` | Journal, report, charts | Yes |

**Test target** (a test only, not the game design): 30 waves, `target_cost(n) = 250 × 1.13^(n−1)`, rounded to 25. Every 5th wave (5, 10, 15, 20, 25, 30) is a relief wave: × 0.8. Write it to `tools/balance/target_costs.csv` with the columns `wave,target_cost`.

**Rules for the waves:**

- Use the cost model from `docs/balance/mistral/attacker-cost/model.json` (field Forest belts) to predict the cost of candidate waves.
- Keep the first-appearance order of attacker types (`UNLOCK` in `make_waves.py`). Do not use `fpv`, `one_way_drone`, `recon_uav`.
- Caps per type (from `make_waves.py`). At most 2 helicopters in any wave.
- From wave 4: at least 2 attacker types in a wave. From wave 8: no type has more than 70% of the predicted cost.
- The predicted cost of a wave must not be more than 1.35 times the predicted cost of the wave before it, except a wave where a type appears for the first time.
- The same seed gives the same waves.

**Check.** With `tools/balance/target_costs.csv` on your branch, the `Balance run` report adds a "Target cost check" and decides the verdict: **pass** when at least 80% of the waves are within 20% of their target on Forest belts and no wave is "not held". `report.json` has the details under `target_cost`.

**Loop:** as in section 5. Stop at pass, after 6 runs, or at an error.

**Deliverables** (in `docs/balance/mistral/wave-generator/`): `report.md` (method, the final check table, the measured cost on the other two fields, problems), a chart of target against measured cost per wave, and the journal. Pull request title: `Mistral: wave-generator — <n> of 30 waves within 20%`.

## 11. Next task: `mistral/squads`

Written by Claude (CTO), 2026-11-25. Start after `mistral/wave-generator` is closed, or when Yaroslav tells you to start it first. PR #7 must be merged first (you need `tools/balance/probe_waves.csv` and its measured costs).

**Background.** Yaroslav's design (2026-11-25): before each mission the player takes a squad of a few defender types (slots). The player cannot take all types. The choice must be hard: the wave needs some types, and the player wants others. One squad that wins every mission is a design failure (the problem of Plants vs. Zombies: one strategy wins the game).

**Goal.** Measure which squads hold which waves, and at what cost. Find dominant squads, defender types that the player must always take, and defender types that the player never needs. Do not change any value. Give the findings and a proposal.

**Tool.** `WaveCost.cs` has a squad mode: `SQUADS_FILE` is a CSV `squad,defenders`, with defender types split by `|`. For each squad the scripted player can buy only these types. The output has a `squad` column. `cost` is `NaN` when the squad cannot hold the wave with `max_budget`. The `Balance run` workflow reads `squads_file` from `tools/balance/run.json`.

Defender types (9): `rifle_squad`, `machine_gun`, `sniper`, `mortar`, `anti_tank`, `air_defence`, `fpv_operator`, `bomber_operator`, `recon_drone`. `recon_drone` is a type like the others: a squad without it has no recon drone.

**Files you can change in this task:**

| File | What you can change | In the PR? |
| --- | --- | --- |
| `tools/balance/make_squads.py` | All (new) | Yes |
| `tools/balance/squads.csv` | All (made by `make_squads.py`) | Yes |
| `tools/balance/squad_waves.csv` | All (the test waves, same columns as `waves.csv`) | Yes |
| `tools/balance/run.json` | All | No: delete it before the PR |
| `docs/balance/mistral/squads/**` | Journal, analysis script, report, charts | Yes |

**Steps:**

1. Make the branch `mistral/squads` from `codex/playable-steppe-patch-integration`.
2. Make `tools/balance/squad_waves.csv`: 40 waves from the mixed probe waves of the attacker-cost task. Use only waves with a measured cost from 500 to 6000 on Forest belts. Include waves with each attacker type (infantry, vehicles, armour, helicopters, drones).
3. Write `tools/balance/make_squads.py`. It writes `tools/balance/squads.csv` with every squad of 4 types from the 9 types (126 squads), plus one squad `all` with the 9 types. Name a squad by its types, for example `mg+sn+at+ad`.
4. Add `tools/balance/run.json`: `{"waves_file": "tools/balance/squad_waves.csv", "squads_file": "tools/balance/squads.csv", "max_budget": 12000, "playtest_seeds": 0}`.
5. Push. Download `balance-report`. `wave-cost.csv` has one row per field, wave and squad.
6. Write your analysis script in `docs/balance/mistral/squads/`. For each squad and field, calculate:
   - **held share**: the share of the 40 waves that the squad holds;
   - **cost premium**: the median of (squad cost / cost of `all`) on the waves that the squad holds;
   - **rank**: how often the squad is the cheapest squad that holds the wave.
7. Calculate for each defender type:
   - **must-have**: the share of waves that no squad without this type can hold;
   - **unused**: the share of waves where no squad in the best 10 for that wave has this type.
8. If the first run takes more than 60 minutes, use only Forest belts for the next runs and write this in the journal.
9. Repeat with squads of 3 and of 5 types (one run each). Compare the held share of the best squad for 3, 4 and 5 slots.

**Check (definitions of the findings):**

- A squad is **dominant** when it holds 90% of the waves or more, with a cost premium of 1.2 or less.
- A type is **must-have** when more than 50% of the waves need it.
- A type is **unused** when it is in no best-10 squad for 90% of the waves or more.
- The target (not a pass/fail for this task): no dominant squad of 4 types; each type is in the best 10 for at least 20% of the waves.

**Loop:** as in section 5. Stop after 4 runs or at an error.

**Deliverables** (in `docs/balance/mistral/squads/`):

- `report.md`: method; a table of the best 15 squads of 4 types (held share, cost premium, rank count) per field; a table of each type (must-have, unused); the result for 3, 4 and 5 slots; dominant squads; strange results.
- Charts (PNG): held share against cost premium for each squad (one point per squad); a heat map of type against attacker type (how often the type is in the best squad for waves with that attacker).
- `squads.json`: the per-squad and per-type numbers.
- A proposal (not a change): which values in `units.csv` or `stop.csv` to change to remove a dominant squad or to give an unused type a role. Give the reason for each.

Pull request title: `Mistral: squads — <n> dominant squads of 4, <n> unused types`.
