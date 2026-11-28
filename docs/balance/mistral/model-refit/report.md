# Mistral model-refit: report

Task: issue #40, spec `docs/agents/mistral.md` section 12 (`mistral/model-refit`). Goal: one workflow that measures the probe waves, refits the wave cost model, checks it on held-out waves and proposes the new model in a pull request. It starts on a data change and by hand.

## What was built

| File | Change |
| --- | --- |
| `.github/workflows/model-refit.yml` | New workflow "Model refit": `workflow_dispatch` (inputs `fields`, `waves`) and a push to `codex/playable-steppe-patch-integration` that changes `unity/Assets/Data/units.csv`, `armour.csv`, `stop.csv`, `battle_rules.csv`, `obstacles.csv`, `tools/balance/stop.csv` or `unity/Assets/Resources/Fields/**`. Jobs: `probe` (make_probe_waves.py from units.csv), `plan` (builds wave-cost.exe, plans the same shards as `balance-run.yml`: field x wave range, at most 200 jobs), `wave-cost` (matrix, `MAX_BUDGET` 30000, `WAVES_FILE` = the fresh probe waves), `fit` (merge shards, `tools/balance/fit_model.py`, check), `pr` (on pass only: branch `model-refit/<run number>` from the integration branch, commit the new `tools/balance/model.json` + `model_metrics.json`, open a PR into `codex/playable-steppe-patch-integration`, title `Model refit <run>: <mean error>% mean error, <share>% within 20%`, body with the old and new metrics) |
| `tools/balance/fit_model.py`, `tools/balance/fitlib.py` | The fit moved from `docs/balance/mistral/attacker-cost/` (the old copies stay there). Input: repo root, the probe waves and the merged `wave-cost.csv`. The attacker types, the single-type curves and the counter table are computed from `units.csv` and `tools/balance/stop.csv`, so no other artifact is needed. Output: `model.json` (same format as before, read by `make_waves.py`) and `model_metrics.json` (mean error and share within 20% on the held-out mixed waves, per field, plus the pass flag). Held-out rule unchanged from the attacker-cost task: tags `mixed` and `mixed-test2` never train. Fixed seed 7: the same input gives the same model (checked: two local fits on the same input are byte-identical) |
| `tools/balance/make_probe_waves.py` | Version 9: the attacker types come from `units.csv` (`side` = attack) in file order; only `EXCLUDED = [fpv, one_way_drone, recon_uav]` stays a fixed list at the top. Unlock waves, triple waves and the infantry filler are derived from `units.csv` too (threat_value spread over waves 1..14, fixed-seed sample of triples, cheapest infantry as filler). A new attacker type in `units.csv` gets single, pair, triple and mixed probe waves with no other change. Deterministic (fixed seeds); checked: two runs give byte-identical output |
| `tools/balance/make_waves.py` | Only the model path: reads `tools/balance/model.json` instead of `docs/balance/mistral/wave-generator-2/model.json`. Checked: with the seeded model (a byte-identical copy of the wave-generator-2 run 3 model), the generated waves are byte-identical to the old script (`waves=30 seed=7 targets=...`, `cmp` clean) |
| `tools/balance/model.json`, `tools/balance/model_metrics.json` | Seeded with the wave-generator-2 run 3 model and its metrics (converted to the new metrics format), so `make_waves.py` reproduces run 3 from the new path and the first refit PR has an "old" to compare against. The workflow replaces both files |
| `docs/log/CHANGELOG.md` | Entry on top |

Not changed (per the spec): `balance-run.yml`, the C# tools, the data files, `tools/balance/probe_waves.csv` (the workflow generates its own probe waves per run into `tools/balance/probe_waves_refit.csv` in the ephemeral checkout; the committed file stays as it was).

## Test runs (spec step 6)

Both runs were started on this branch by hand (a temporary push trigger on `mistral/model-refit`, removed again before this PR; `workflow_dispatch` via the API only resolves workflows that are already registered on a scanned branch, so a brand-new workflow file cannot be dispatched by hand until it is synced to the default branch — same for every new workflow in this repo).

| Run | Workflow run | Data | Duration | Result |
| --- | --- | --- | --- | --- |
| 1 | 45989845538 (push) | unchanged data | 36 min 15 s | 726 probe waves x 3 fields = 2178 measured costs (1829 held); fit OK; check: Forest belts 80 waves 11.3% mean error 85.0% within 20%, Balka and railway 45 waves 24.8% 60.0%, River crossing 79 waves 15.4% 67.1%; pooled 204 waves 15.9% 72.5% — verdict **fail** (rule: at least 80% on every field), run failed with the metrics in the run summary, no PR (pr job skipped) |
| 2 | 45996371275 (push) | test change on this branch only: `old_tank` hp 100 -> 120 | 37 min 19 s | 2178 measured costs (1829 held); check: Forest belts 9.5% mean error 86.2% within 20%, Balka and railway 24.5% 62.2%, River crossing 15.1% 70.9%; pooled 204 waves 15.0% 75.0% — verdict **fail**, metrics in the run summary, no PR |

### What changed in the model after the test change (old_tank hp +20%)

Both models were fitted by the same deterministic `fit_model.py` (seed 7) on the probe waves measured in the run; run 1 was re-fitted locally from the run artifacts (198 shard CSVs) and reproduces the workflow fit exactly (same metrics), run 2 is the `model` artifact of the run.

- Only one measured curve changed: `old_tank` on River crossing, n=4: 1475 -> 1925, n=6: 1775 -> 2225 (the +20% hp made those waves cost more there; on Forest belts and Balka and railway the holding budget stayed the same, the cost is quantised to 25).
- `F0` moved on all three fields (Forest belts 47.82 -> 47.94, Balka and railway 30.84 -> 30.63, River crossing 49.05 -> 49.23) and all 60 trees per field were re-fit (leaves changed in 60 of 60 on Forest belts).
- Held-out metrics moved with the data: pooled mean error 15.9% -> 15.0%, within 20% 72.5% -> 75.0% (Forest belts 85.0% -> 86.2%, Balka and railway 60.0% -> 62.2%, River crossing 67.1% -> 70.9%).

So a data change in `units.csv` refits the model by itself and the change lands exactly where the data changed.

## Notes and open points

- The check fails on the current data (72.5% pooled within 20%, 85.0% on the gate field Forest belts but 60.0% on Balka and railway). Per the spec that is the defined behaviour: on fail no PR is opened and the run fails with the metrics in the summary. The gate field alone is above 80%; the other two fields are not. This is a property of the current data and probe waves, not of the workflow — the workflow reports it.
- The refit takes about 36 minutes end to end (198 shard jobs, each ~11 probe waves on one field, ~20 concurrent; the fit itself takes about 1 second).
- `make_waves.py` embeds the single-type curves as constants (`CURVES`) for its search features; those are not refreshed by the refit (the spec allows only the model-path change in `make_waves.py`). The model prediction itself uses only `model.json`. A follow-up could read the curves from `model.json` too.
- The workflow file must be synced to the default branch (as with every new workflow in this repo) before `workflow_dispatch` by hand works; the push trigger on `codex/playable-steppe-patch-integration` works as soon as this PR is merged there.
