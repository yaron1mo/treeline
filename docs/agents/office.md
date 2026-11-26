# Agent office: how the agents give work to each other

Written by Claude (CTO), 2026-11-26. Approved by Yaroslav (CEO).
Write all repo text in ASD-STE100 Simplified Technical English.

## 1. Principle

The agents do not call each other directly. They write on GitHub (issues, labels, pull requests, comments). GitHub Actions reads these events and starts the correct agent. All agents use Yaroslav's subscriptions. There is no API billing.

| Agent | Starts on | Runs on | Login |
| --- | --- | --- | --- |
| Claude (review) | A PR from `mistral/**` or `codex/**` opens or becomes ready | GitHub servers | Secret `CLAUDE_CODE_OAUTH_TOKEN` (Claude subscription) |
| Claude (any request) | `@claude` in an issue, a PR comment or a PR review | GitHub servers | Same |
| Mistral | Label `agent:mistral` on an issue | Yaroslav's Windows computer (self-hosted runner, label `mistral`, Windows PowerShell) | Yaroslav's Mistral login in Vibe CLI (no API key) |
| Codex | `@codex` in an issue or PR comment | OpenAI cloud | Yaroslav's ChatGPT plan |
| Yaroslav | Label `needs:yaroslav`, label `ready-to-merge` | GitHub Mobile notifications | — |

## 2. Labels

| Label | Meaning | Who sets it |
| --- | --- | --- |
| `agent:mistral` | Start Mistral on this issue | Claude or Yaroslav |
| `agent:codex` | This issue is for Codex (Claude also writes `@codex` in a comment) | Claude or Yaroslav |
| `agent:claude` | This issue is for Claude | Anyone |
| `in-progress` | An agent works on this issue now | The workflow |
| `needs:yaroslav` | A design decision or a stop. Agents do not continue | Any agent |
| `ready-to-merge` | Claude approved the PR. Yaroslav merges | Claude |

## 3. Review (Claude)

Do these steps for each review:

1. Read the PR, the diff and the task spec (`docs/agents/mistral.md` for Mistral; the linked issue for Codex).
2. Check the CI results of the PR (`gh pr checks`). Run `bash tools/tests/run_editmode.sh` when the PR changes code in `unity/Assets/Scripts`.
3. Count your earlier reviews on this PR. If there are 3 or more, add `needs:yaroslav`, write why, and stop.
4. Write one review comment: verdict (approve or changes), the findings, and what to do next.
5. If you ask for changes: write the changes as a numbered list. For Mistral, add the label `agent:mistral` to the task issue again (remove it first), so that Mistral starts on the changes. For Codex, write `@codex` and the list.
6. If you approve: add `ready-to-merge` to the PR. Write a summary for Yaroslav in 5 lines or fewer. Then find the next task of this agent in its spec or in the open issues. If the next task is not blocked, open an issue for it with the agent label.
7. If a design decision is necessary, add `needs:yaroslav` and write the question with 2–4 options.

Do not merge. Do not change the PR branch in a review. Do not read or print secrets.

## 4. Limits

- Claude: at most 40 turns per run, 40 minutes per run.
- Mistral: one task at a time, at most 200 turns, 230 minutes per run.
- At most 3 review rounds per PR. Then Yaroslav decides.
- Daily token limits (Yaroslav, 2026-11-26): Claude 18% and Codex 19% of the weekly limit per day. The subscriptions do not show this percent to scripts. Stage 3 counts the tokens of each run for 1 week, compares them with the percent in the apps, and then sets the daily limits in tokens. Note: 18% per day for 7 days is 126% of a week.

## 5. Stages

| Stage | What | Status |
| --- | --- | --- |
| 0 | Claude review on PRs; `@claude` in comments; labels | Files ready |
| 1 | Mistral on the label `agent:mistral` (self-hosted runner) | File ready; runner not installed |
| 2 | Codex on `@codex` | Codex not connected to the repo |
| 3 | Claude dispatcher on a schedule (every 2–3 hours: who is free, what is stuck) and token counts | Not started |

## 6. Safety

- The repo is public. The self-hosted runner runs only the workflow `mistral-task.yml`, which starts on a label. Only people with write access can add a label.
- Set "Require approval for all external contributors" in Settings > Actions > General, so that a PR from a fork cannot start a workflow without Yaroslav.
- Mistral pushes and opens PRs with Yaroslav's own git and gh login on his computer. A push with `GITHUB_TOKEN` does not start other workflows.
