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

## 9. Current task: `mistral/attacker-cost`

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
