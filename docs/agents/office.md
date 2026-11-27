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
| Codex | Label `agent:codex` on an issue | Yaroslav's Windows computer (second self-hosted runner, Windows PowerShell) | Yaroslav's ChatGPT login in Codex CLI (`codex login`, no API key) |
| Yaroslav | A comment with `@yaron1mo` (with the label `needs:yaroslav` or `ready-to-merge`) | GitHub Mobile, e-mail, the bell on github.com | — |

## 2. Labels

| Label | Meaning | Who sets it |
| --- | --- | --- |
| `queue:mistral`, `queue:codex` | The task is ready (full spec). It waits for a free agent | Claude (Opus) or Yaroslav |
| `agent:mistral`, `agent:codex` | Start the agent on this issue now. The workflow removes the label when the run ends | Dispatcher, Claude, Yaroslav |
| `agent:claude` | This issue is for Claude | Anyone |
| `needs-spec` | The task has no full spec yet. Claude (Opus) writes it in the next dispatcher run | Dispatcher, Yaroslav |
| `in-progress` | An agent works on this issue now | The workflow |
| `needs:yaroslav` | A design decision, missing information (story, art direction) or a stop. Agents do not continue. Telegram message | Any agent |
| `ready-to-merge` | Claude approved the PR. The auto-merge workflow merges it when the checks pass | Claude |

## 3. Review (Claude)

Do these steps for each review:

1. Read the PR, the diff and the task spec (`docs/agents/mistral.md` for Mistral; the linked issue for Codex).
2. Check the CI results of the PR (`gh pr checks`). Run `bash tools/tests/run_editmode.sh` when the PR changes code in `unity/Assets/Scripts`.
3. Count your earlier reviews on this PR. If there are 3 or more, add `needs:yaroslav`, write `@yaron1mo` and why, and stop.
4. Write one review comment: verdict (approve or changes), the findings, and what to do next.
5. If you ask for changes: write the changes as a numbered list. Add the label `agent:mistral` (or `agent:codex`) to the task issue, so that the agent starts on the changes.
6. If you approve: add `ready-to-merge` to the PR. The PR then merges by itself into `codex/playable-steppe-patch-integration` when the checks pass. Approve only when the work meets its spec and needs no design decision. Write a summary in 5 lines or fewer. Close the task issue. If the spec has a next task for this agent, open an issue for it with `queue:<agent>`.
7. If a design decision is necessary, add `needs:yaroslav` and write the question with 2–4 options.
8. Each time you add `needs:yaroslav`, start your comment with `@yaron1mo`. A Telegram message also goes to Yaroslav.

Do not merge by hand. Do not change the PR branch in a review. Do not read or print secrets.

## 4. Limits

- Claude: at most 40 turns per run, 40 minutes per run.
- Mistral: one task at a time, at most 200 turns, 230 minutes per run.
- At most 3 review rounds per PR. Then Yaroslav decides.
- Daily token limits (Yaroslav, 2026-11-26): Claude 18% and Codex 19% of the weekly limit per day. The subscriptions do not show this percent to scripts. Stage 3 counts the tokens of each run for 1 week, compares them with the percent in the apps, and then sets the daily limits in tokens. Note: 18% per day for 7 days is 126% of a week.

## 5. Stages

| Stage | What | Status |
| --- | --- | --- |
| 0 | Claude review on PRs; `@claude` in comments; labels | Works (2026-11-26) |
| 1 | Mistral on the label `agent:mistral` (self-hosted runner) | Works (2026-11-27) |
| 2 | Codex on the label `agent:codex` (second runner on the same computer) | Works (2026-11-27) |
| 3 | Dispatcher (Sonnet, every 3 hours), spec writer (Opus), auto-merge, Telegram | Files ready (2026-11-27) |
| 4 | Token counts per day and daily limits | Not started |

## 6. Dispatcher

The `Dispatcher` workflow runs every 3 hours (and by hand). Model: Sonnet. Rules: `docs/agents/dispatcher.md`.
When open issues have `needs-spec`, the same workflow then starts Claude (Opus) to write the specs.

## 7. Auto-merge

The `Auto-merge` workflow merges a PR when all these conditions are true:
- Claude (`claude[bot]`) added `ready-to-merge`.
- The PR is from `mistral/**` or `codex/**` into `codex/playable-steppe-patch-integration`.
- The PR has no `needs:yaroslav` label.
- All checks of the PR pass.

Nothing merges into `main` without Yaroslav.

## 8. Telegram

The `Notify` workflow sends a Telegram message to Yaroslav when an issue or PR gets `needs:yaroslav`, when a PR merges, and when an agent run fails. Secrets: `TELEGRAM_BOT_TOKEN`, `TELEGRAM_CHAT_ID`. Without the secrets, it does nothing.

## 9. Safety

- The repo is public. The self-hosted runner runs only the workflow `mistral-task.yml`, which starts on a label. Only people with write access can add a label.
- Set "Require approval for all external contributors" in Settings > Actions > General, so that a PR from a fork cannot start a workflow without Yaroslav.
- Mistral pushes and opens PRs with Yaroslav's own git and gh login on his computer. A push with `GITHUB_TOKEN` does not start other workflows.
