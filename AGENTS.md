# AGENTS.md — rules for every agent working in this repo

Read this file first. It applies to Claude, Codex and any other agent. The human owner is Yaroslav (analyst, not a developer): explain in plain words, keep changes small and reviewable.

## The game

**Treeline** — a mobile tower-defence game about defending against an invasion on open terrain in eastern Ukraine. Free-form terrain (no visible grid), attackers choose their own routes and learn where the defence is, with a delay. Realistic but low-detail "drone feed" look. Store builds use fictional names (Apple guideline 1.1.2). Monetisation: ads plus a $1 remove-ads purchase. Target: phones first (Android, then iOS). Web prototype is MVP-only.

## Repo layout

| Path | What | Main owner |
| --- | --- | --- |
| `unity/` | Unity 6.3 LTS project (6000.3.28f1), Universal 2D | see below |
| `unity/Assets/Data` | Game content built from tables (CSV/JSON) | Claude |
| `unity/Assets/Scripts/Sim` | Pure C# simulation, **no UnityEngine** | Claude |
| `unity/Assets/Scripts/Game` | Bridge: runs Sim, owns game loop, scenes | Claude |
| `unity/Assets/Scripts/Analytics` | Event tracking | Claude |
| `unity/Assets/Scripts/Meta` | Everything outside the battle: campaign, endless mode, weekly challenges, commanders, progression, menus flow (created when work starts) | Codex |
| `unity/Assets/Scripts/Render` | Drawing terrain, units, effects | Codex |
| `unity/Assets/Scripts/UI`, `unity/Assets/UI` | Menus, HUD (UI Toolkit) | Codex |
| `unity/Assets/Art`, `unity/Assets/Audio` | Visuals, sound | Codex |
| `unity/Assets/Tests` | Tests | whoever changes the code |
| `rules/` | Current rules JSON (to become CSV tables in `unity/Assets/Data`) | Claude |
| `fields/`, `tools/` | Field files and helper scripts | Claude |
| `prototype-web/` | Web prototype — reference for algorithms, do not extend | frozen |
| `docs/` | Design notes and change logs | both |
| `docs/story/` | Story, setting, names, texts | Codex (Claude may edit, must log) |

**Who owns what (decided 2026-11-05, Yaroslav).** Claude: units and every in-game process (the battle: simulation, rules, balance, data tables for units and obstacles, analytics). Codex / ChatGPT: everything that is not the battle: story, maps and their content, game modes (campaign, endless, weekly challenges), commander selection and progression, menus, easter eggs and similar. Shared: the economy of progression and monetisation, decided with Yaroslav.

Ownership is a default, not a wall. Either agent may edit anything, but **say so in the log** when touching the other's area.

## The contract between layers

- `Sim` knows nothing about Unity, rendering or UI. It takes data and time steps, and exposes state and events.
- `Render` and `UI` read `Sim` state and listen to its events. They never change simulation values directly; they send commands (place defence, start wave).
- `Game` is the only place that connects the three.
- The meta layer (modes, commanders, progression) talks to `Sim` only through a run setup (field, wave list, mode, commander) and a run result. It never changes unit numbers; commanders and upgrades are data rows that point at unit and obstacle `id`s. Claude defines this interface; Codex fills it with content.
- Every number that affects balance lives in `unity/Assets/Data`, never hard-coded in scripts.
- Assembly definitions enforce this: `Treeline.Sim` has no Unity references; `Render`, `UI`, `Analytics` reference `Sim`/`Game` but not each other.

## Work rules

1. **Branches**: `claude/<topic>` and `codex/<topic>`. Never both agents on the same branch. Merge to `main` through a pull request; Yaroslav approves.
2. **Logs are mandatory.** Every change that is more than a typo gets a dated line in `docs/log/CHANGELOG.md` (newest on top): `YYYY-MM-DD — [agent] area — what changed and why`. Story, names, texts and setting changes **also** go in `docs/story/log.md` with the old and new wording. Never rewrite old log entries; add new ones.
3. **Unity files**: commit every `.meta` file together with its asset. Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`. Do not rename or move assets outside the Unity editor without moving the `.meta` too. Scenes and prefabs are text (YAML) — keep diffs small, and avoid two agents editing the same scene; prefer building content from data in code.
4. **Binaries** (png, wav, mp3, ttf…) go through Git LFS (see `.gitattributes`).
5. **Data first**: units, defences, armour and cover are rows in tables; special behaviour is a `behaviour` column mapped to code, not an `if` on a unit name.
6. **Mobile rules**: touch-only input, safe areas, readable at phone size, offline-capable assets, steady 60 fps on a mid-range phone, no per-frame allocations in `Sim`.
7. **Music and art**: only CC0 / CC-BY or explicit permission; keep licence notes in `docs/licenses/`.
8. **Tell the owner the truth about size and risk.** If something is unclear, ask Yaroslav one short question instead of guessing on anything that is hard to undo.
