# Change log

Newest on top. Format: `YYYY-MM-DD — [agent] area — what changed and why`. Do not edit old entries.

- 2026-11-04 — [claude] sim — First C# port of the web prototype into `Treeline.Sim`: JSON reader, field file parser, terrain rasterised to the hidden 2 m nav grid (vehicles keep one cell clear of impassable terrain), Dijkstra flow fields per unit class. Terrain move costs are still the prototype numbers, to move into the data tables. Added `FieldDebugView` (Game) as a throw-away debug view, the three test fields in `Resources/Fields`, and EditMode tests in `Tests/EditMode`. Not compiled yet: no C# compiler in the sandbox; first check is Unity's Console.
- 2026-11-04 — [claude] data — Units table created in Google Sheets (link in `docs/design/units-table.md`), snapshots in `unity/Assets/Data/*.csv`. One row per unit, side/type/subtype as columns, `wave_points` dropped, `fire_on_move` and `move_accuracy_mult` added, `sight_range_m` left empty. Why: Yaroslav's decision for a single table.
- 2026-11-04 — [claude] rules — Decision (Yaroslav): infantry and weapon-carrying vehicles fire while moving, at accuracy x0.5. Why: keeps the game a tower defence; units do not stop to fight.
- 2026-11-04 — [claude] repo — Added AGENTS.md, CLAUDE.md, docs/ structure, Unity folder layout and assembly definitions (Sim without UnityEngine). Moved web prototype to `prototype-web/`. Why: start the Unity port with a clear split between Claude (rules, sim, data) and Codex (visuals, UI, story).
- 2026-11-04 — [yaroslav] unity — Created empty Unity project (Universal 2D, Unity 6.3 LTS 6000.3.28f1) in `unity/`.
- 2026-11-04 — [claude] repo — Added Unity `.gitignore` and `.gitattributes` (text assets as text, Git LFS for binaries).
- 2026-11-03 — [claude] rules — Three-mode threat-aware pathfinding written into `rules/attackers.json` and the web prototype (fastest, evade, safest-then-shortest).
