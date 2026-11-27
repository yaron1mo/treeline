# Mistral squads: iterations

Task: which squads hold which waves (see `docs/agents/mistral.md`, section 11). No value changes; the deliverable is the measurement and a proposal.

Wave set: `tools/balance/squad_waves.csv` — 40 mixed probe waves of the attacker-cost task with a measured cost from 500 to 6000 on Forest belts (Balance run 45411511647 of PR #7), renumbered 1-40, spread over the cost range 500-5775, covering all 11 attacker types that the probe waves contain (recruit, regular, special_forces, motorbike, buggy, van, mrap, ifv, old_tank, modern_tank, helicopter). The probe set has no wave with recon_uav, fpv or one_way_drone, so no drone-attacker wave is in the 500-6000 band.

| Iteration | Run | Change | Hypothesis | Verdict | Result | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | | `make_squads.py` + `squads.csv` (126 squads of 4 + all), `squad_waves.csv`, `run.json` (waves_file squad_waves.csv, squads_file squads.csv, max_budget 12000, 0 seeds) | Each of the 40 waves is measured once per squad on all 3 fields; the analysis script then finds held share, cost premium, rank, must-have and unused per type | pending | | Push starts the run |
