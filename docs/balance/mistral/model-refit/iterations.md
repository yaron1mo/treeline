# Mistral model-refit: iterations

Task: issue #40, spec `docs/agents/mistral.md` section 12. Journal of the runs of the Model refit workflow on this branch (temporary push trigger on `mistral/model-refit`, removed before the PR).

| Iteration | Workflow run | Change before the run | Result | Verdict | Notes |
| --- | --- | --- | --- | --- | --- |
| 1 | 45989845538 | none (baseline data) | 2178 measured costs (1829 held); Forest belts 85.0% / Balka and railway 60.0% / River crossing 67.1% within 20% on held-out mixed waves; pooled 15.9% mean error, 72.5% within 20% | fail (rule: >= 80% on every field) | 36 min 15 s; metrics in the run summary, no PR. Probe waves: 726 waves from units.csv (v9 script) |
| 2 | 45996371275 | test change on this branch only: `old_tank` hp 100 -> 120 | 2178 measured costs (1829 held); Forest belts 86.2% / Balka and railway 62.2% / River crossing 70.9% within 20% on held-out mixed waves; pooled 15.0% mean error, 75.0% within 20%; only the River-crossing old_tank curve changed (n=4 1475->1925, n=6 1775->2225), all trees re-fit | fail (rule: >= 80% on every field) | 37 min 19 s; test change reverted before the PR |
