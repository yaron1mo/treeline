# Squad selection and slots

Proposal by the designer agent, 2026-11-27, for issue #19. Updated the same day to the decisions of Yaroslav on PR #24. Updated again for the veteran model of Yaroslav (section 7). Status: **5 questions are open** (section 7.8). Nothing here changes a "Decided" item in `progression.md`.

## 1. Goal

Before each mission the player takes a small squad. The choice must be hard: the wave needs some unit types, the player wants others. No single squad wins every mission. The choice takes less than one minute, so a session stays 3–5 minutes.

## 2. Player experience

The player opens a mission and sees a short brief: the map, the modifier (for example night) and a forecast of the wave. The player has 3 slots and a barracks of units, and a separate slot for recon drones. The forecast says "vehicles from the east, drones late", so the player must give up a favourite unit for a counter. In the battle, the player builds only the types in the squad. After the battle, a hurt unit is out for some missions, so the next mission needs a different mix.

## 3. Rules (decided by Yaroslav, 2026-11-27)

### 3.1 Slots

- There are **3 slots** at the start. One slot holds one unit type with its upgrade path (for example "sniper" or "marksman"). The player can build many units of that type in the battle, for money.
- **Recon drones have their own slot** with their own count. They do not use one of the 3 slots.
- A slot holds a general unit type. The veteran has a separate slot: see section 7.
- Slots open with the campaign, not with money. Proposal for later slots (not decided):

| Slots | Opens when |
| --- | --- |
| 3 | Start |
| 4 | End of chapter 1 |
| 5 | End of chapter 2 |
| 6 | End of chapter 3 (maximum; always fewer than the owned types) |

### 3.2 Forecast by intelligence level

The brief depends on the intelligence upgrades. **Each upgrade opens one item.** At level 0 the brief shows the map and the modifier only.

| Order | Level | The brief shows | Why in this place |
| --- | --- | --- | --- |
| 1 | 1 | Number of waves | Cheapest fact. The player can plan money and time. |
| 2 | 2 | Types of attackers on the level (the full list, no counts) | The player can choose counters for the squad. This is the main value for slot choice. |
| 3 | 3 | Composition of the next wave (during the battle) | A tactical help, after the squad is fixed. |
| 4 | 4 | The best attacker route on the map | Needs the most knowledge. It helps placement, not the squad. |

Reason for the order: items that help the choice of squad come first, because the squad is the core decision. Items that help the battle come later. The forecast shows true data only: it can hide, it must not lie. Weather and EW can make an item less exact (a range instead of a number).

### 3.3 Wounds only

- No fatigue. A unit that is hurt in a mission is **wounded** and is out for N missions (see the open item "Death or wounded" in `progression.md`). N is for the balance table.
- A killed unit is lost ("200") unless the player saves it (decided rule).

### 3.4 Pairs

There is **no bonus and no hint** for pairs of units. The rules make some pairs strong (for example recon drone and mortar, through shared vision). The player finds them alone.

### 3.5 Levels and modes

- **Endless is many different levels.** Levels are not the same: some have 7 waves, some 10, some 20, and so on. **Each level is a different map.** The squad is chosen before each level, as in the story.
- **Weekly challenge squad: later.** Separate balance and settings come later. This proposal does not set a squad rule for it, except in the variants of section 7.

## 4. Two examples

**Example A: chapter 1, "Forest belts", night, 3 slots, intelligence 0.**
The brief shows only the map and "night". Owned: rifleman, machine gunner, mortar, sniper. The player takes the rifleman (cheap base), the machine gunner and the mortar. At night the sight is short, so the sniper cannot use its range. The risk: a vehicle may come, and the squad has no anti-tank unit.

**Example B: chapter 3, "Open steppe", fog, 5 slots, intelligence 2.**
The brief shows the attacker types: infantry, IFVs, one-way drones. The player takes the machine gunner (infantry), anti-tank (IFVs), air defence (drones), the mortar and the marksman. The recon drones are in their own slot. The sniper is left in the barracks: its sight is short in fog.

## 5. Connection to decided items

- Uses "Squad before each mission", "Slots open with progress" and "One squad that wins everything is a failure".
- Uses "Depth from upgrade paths": slot = type + path.
- Uses "Mission modifiers" and the intelligence branch (each upgrade opens one forecast item).
- Uses "Death", "Weekly challenge" (same map, seed, modifier; nothing carried over) and "Upgrades" (experience makes upgrades cheaper).

## 6. What Claude must build or measure

1. **Squads measurement (`mistral/squads`):** for each level, test all squads of 3 slots plus the recon slot. Pass: no squad wins more than a set share of the levels, and each hard-counter type is in the best squad for at least one level.
2. Check that the wave generator can output the four forecast items (wave count, type list, next wave, best route).
3. Run setup: add a squad (list of unit `id` and path) and a recon count. Add a wound table (missions out).
4. Endless: a level list with different wave counts (7, 10, 20…) and one map per level. Codex makes the maps.
5. Veterans: see section 7.7.

## 7. Veterans and in-battle levelling

Decided direction by Yaroslav, 2026-11-27. The open items are in section 7.8.

### 7.1 Core rule: units level up inside the battle

- A unit gains experience from kills and levels up in the battle. This is the main progression in a battle in all modes (campaign, weekly, endless).
- The player can move a unit. Roman-legion play: a fast first line levels up, then the player moves it back to a line where it protects more, and builds new units in front.
- The balance of each mission is made **without veterans**. A veteran is a bonus.

### 7.2 Carry-over is a limited perk, not the default

Reason: if units carry over freely, mission 1 is hard and missions 2–7 are easy (a spike, then a collapse).

- After a battle, the player **keeps** a unit only with the perk "keep a unit after the battle". The perk gives 1 kept unit, then 2, then 3 (maximum).
- All other units are lost after the battle.
- Proposed branch for the perk: Intelligence (Yaroslav; to confirm).

### 7.3 The veteran

| Step | Rule |
| --- | --- |
| Become a veteran | A kept unit that reached the maximum level of its path becomes a veteran. |
| Name and story | It gets a call sign (random from a list) and a short story of the battle where it was promoted (section 7.5). |
| Growth | It still levels, slowly, to one strong **passive talent**. |
| Veteran slot | 1 at the start. It is separate from the 3 unit type slots and from the recon slot. More slots come from an upgrade branch (section 7.8, question 2). |
| Cost | The player pays money for each battle when the veteran is taken. The cost is less than its full value. |
| Cap | The veteran level cannot be higher than the mission level + N (N is for the balance table). A veteran helps, but it cannot skip a mission. |

Unit type slots (the 3 slots) are upgraded in the infantry / Assault branch, and some maybe in Drone forces.

### 7.4 What this means for the earlier problem

"All veterans died, where do new units come from?" is solved by the base rule: the player builds general units of the 3 slot types in the battle, for money. The veteran is one extra unit on top of the squad. A dead veteran is lost ("200") or saved by the decided pop-up. No slot is ever empty.

### 7.5 Stories without an AI model

The game builds the story from templates. Each promotion has one reason, taken from the battle statistics. Each reason has 5 templates with variables `{callsign}`, `{sector}`, `{kills}`, `{enemy}`, `{wave}`. The lists are in [veteran-stories.md](veteran-stories.md) (later a CSV in `unity/Assets/Data`). The game picks the reason with the highest priority that the battle statistics show.

### 7.6 Modes

| Mode | Carry-over | Veteran | Levelling |
| --- | --- | --- | --- |
| Campaign | Limited by the perk (7.2) | Yes | In the battle |
| Weekly challenge | None | None | In the battle only. The Roman-legion play is the point. |
| Endless | Kept units follow the same perk rule, inside the endless roster | Yes, from the endless roster | In the battle |

**Endless and the global kill counter.** Balance is not important in endless, but the difficulty must rise, so the player cannot farm the counter. Final target of the counter: open (50 000 seems possible, 100 000 is a lot). Proposed rule: kills in endless count with **diminishing returns** per level:

| Kills of the player in one endless level | Share that counts |
| --- | --- |
| First 100 | 100% |
| 101–300 | 50% |
| 301–600 | 25% |
| More than 600 | 10% |

Also: the difficulty of a new level rises with the number of levels done in the run, so a longer run is harder, not only longer. Claude measures how many levels a good player can do and checks the counter growth.

### 7.7 What Claude must build or measure (veterans)

1. Experience and level rules in `Sim` (the level is lost at the end of the battle unless the unit is kept).
2. Run result: list of kept units with kills and the promotion reason. Run setup: veteran list with call sign and talent.
3. Measure: win rate with a veteran against without one on the same mission. Pass: the veteran changes the win rate by a small set share, never by a "skip".
4. Measure: the kill counter growth in endless with the table in 7.6.

### 7.8 Open questions (new)

1. **Branch of the perk "keep a unit".** (a) Intelligence (recommended: it fits "we know our people"; the branch has two paths and little other help for squads), (b) Assault, (c) Drone forces. Reason: Yaroslav proposed Intelligence; the branch is the least crowded.
2. **Branch of the extra veteran slots ("special forces").** Only 3 meta branches are allowed. (a) Assault (recommended: infantry heavy; same branch as unit slots), (b) Intelligence (then one branch holds both carry-over and slots, too strong), (c) a new branch (not allowed by the "maximum 3" rule, so it needs a change of that rule).
3. **Veteran cap N.** (a) mission level + 1 (strict), (b) mission level + 2 (recommended: a clear gift, no skip), (c) no cap, but a higher cost for a higher level.
4. **Veteran cost per battle.** (a) fixed share of the full value, for example half, (b) a share that rises with the veteran level (recommended: keeps a late veteran a choice), (c) free for the first battle of each mission only.
5. **Endless counter rule.** (a) diminishing returns by kills per level (table 7.6, recommended), (b) a hard cap per run, (c) endless does not count for the weapon milestones, only for a separate endless score.
