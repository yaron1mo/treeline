# Mistral wave-generator-2: model iterations

Task: issue #17, follow-up of PR #9 (spec: `docs/agents/mistral.md` section 10; findings 4 and 5 of the Claude review 2 on PR #9).

| Iteration | Run | Change | Hypothesis | Verdict | Waves in target | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | pending | `make_waves.py`: waves 1-3 need at least 4 infantry (`MIN_EARLY_INFANTRY`, finding 4); optional `targets` argument (comma-separated target costs, replaces `tools/balance/target_costs.csv`, finding 3); model path moved to `docs/balance/mistral/wave-generator-2/model.json` (copy of the committed PR #9 model). Run 1 measures the 30 generated waves (waves 1-30 of `waves.csv`) plus 75 armour probe waves (waves 31-105, `probe_waves_armour.csv`, tag `mixed-train4`) | The waves 1-3 rule removes the leak-at-25 wave; the armour probes teach the model the armour-with-little-infantry area (finding 5) | pending | pending | Generated waves kept as `waves_run1.csv`; predicted 29 of 30 within 20% of the target (wave 1 -26.3%). Checked locally: the `targets` argument gives the same waves as the file; seed 7 twice identical; `waves=12 seed=3` works; a wrong-length `targets` list exits with an error |
