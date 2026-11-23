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

## 8. Current task: `mistral/economy-1`

**Goal.** All 20 waves are in the target curve on Forest belts (verdict `pass`).

**Limits:**

- Keep 20 waves.
- Keep the order in which attacker types first appear. You can move the first wave of a type by up to 2 waves.
- Do not add `fpv` or `one_way_drone`. They are not in this playtest.
- Do not change `line_health`.

**Start point** (2026-11-17 measure, the old scripted player):

- Waves 1–6: mostly in target. Wave 1 costs 300, but the start budget is 250.
- Wave 7 (first `old_tank`): cost 400 → 1675, ratio 0.59.
- Waves 15–20: ratio 0.45–0.63. The player needs about 2 times more money.

The first run on your branch gives the real start point. The scripted player changed on 2026-11-23: it now stands on the traced routes.
