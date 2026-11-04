# Change log

Newest on top. Format: `YYYY-MM-DD — [agent] area — what changed and why`. Do not edit old entries.

- 2026-11-04 — [claude] repo — Added AGENTS.md, CLAUDE.md, docs/ structure, Unity folder layout and assembly definitions (Sim without UnityEngine). Moved web prototype to `prototype-web/`. Why: start the Unity port with a clear split between Claude (rules, sim, data) and Codex (visuals, UI, story).
- 2026-11-04 — [yaroslav] unity — Created empty Unity project (Universal 2D, Unity 6.3 LTS 6000.3.28f1) in `unity/`.
- 2026-11-04 — [claude] repo — Added Unity `.gitignore` and `.gitattributes` (text assets as text, Git LFS for binaries).
- 2026-11-03 — [claude] rules — Three-mode threat-aware pathfinding written into `rules/attackers.json` and the web prototype (fastest, evade, safest-then-shortest).
