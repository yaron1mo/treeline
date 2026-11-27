# Squad selection and slots

Proposal by the designer agent, 2026-11-27, for issue #19. Updated the same day to the decisions of Yaroslav on PR #24. Status: **one question is open** (veteran and general slots, section 7). Nothing here changes a "Decided" item in `progression.md`.

## 1. Goal

Before each mission the player takes a small squad. The choice must be hard: the wave needs some unit types, the player wants others. No single squad wins every mission. The choice takes less than one minute, so a session stays 3–5 minutes.

## 2. Player experience

The player opens a mission and sees a short brief: the map, the modifier (for example night) and a forecast of the wave. The player has 3 slots and a barracks of units, and a separate slot for recon drones. The forecast says "vehicles from the east, drones late", so the player must give up a favourite unit for a counter. In the battle, the player builds only the types in the squad. After the battle, a hurt unit is out for some missions, so the next mission needs a different mix.

## 3. Rules (decided by Yaroslav, 2026-11-27)

### 3.1 Slots

- There are **3 slots** at the start. One slot holds one unit type with its upgrade path (for example "sniper" or "marksman"). The player can build many units of that type in the battle, for money.
- **Recon drones have their own slot** with their own count. They do not use one of the 3 slots.
- How a slot is filled (a named veteran or a general unit) is open: see section 7.
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
- Uses "Death" and "Weekly challenge" (same map, seed, modifier; nothing carried over).

## 6. What Claude must build or measure

1. **Squads measurement (`mistral/squads`):** for each level, test all squads of 3 slots plus the recon slot. Pass: no squad wins more than a set share of the levels, and each hard-counter type is in the best squad for at least one level.
2. Check that the wave generator can output the four forecast items (wave count, type list, next wave, best route).
3. Run setup: add a squad (list of unit `id` and path) and a recon count. Add a wound table (missions out).
4. Endless: a level list with different wave counts (7, 10, 20…) and one map per level. Codex makes the maps.
5. For the chosen variant of section 7: measure the win rate of a squad of only veterans against a mixed squad.

## 7. Open question: veteran slots and general slots

**The problem.** Building many units in a battle is allowed. If the player takes only upgraded veterans and all of them die, there are no new units. Yaroslav's idea: veteran slots and general slots are separate. This conflicts a little with the weekly challenge (one map, nothing carried over), but this can be a feature. Three complete variants:

| | A. Fixed split | B. Veteran leads, general builds | C. Free slots, free base unit |
| --- | --- | --- | --- |
| **Rule** | The 3 slots are split: for example 1 veteran slot and 2 general slots. A veteran slot holds a named unit with its upgrades. A general slot holds a type at base level, built for money. | Each slot holds one type. A veteran is the "lead" and starts on the field for free. All new units of that type are built at base level (or with the upgrades of the type, bought in the run). If the lead dies, the slot stays and the player builds general units. | The player fills the 3 slots as they want. Each slot is veteran or general. The squad must hold at least one general slot, or the game adds a free base unit ("militia") when all veterans are dead. |
| **If all veterans die** | The general slots still give new units. The veteran slot is empty. | The slot keeps its type, so the player rebuilds at base level. | The free base unit gives a way to continue. |
| **Choice** | Simple, but the split is fixed. Less choice. | Deep: the lead is a gift, not a risk. The type still matters. | Most choice. Hardest to balance (a squad of 3 veterans is possible). |
| **Weekly challenge** | No veterans exist (nothing is carried over), so the veteran slot is empty or gets a fixed unit. Result: weekly has fewer slots. This is simple and fair. | Same rule for all: no lead, only general builds. Same squad choice, fair. The weekly is a "pure" test of type choice. | The weekly forces "all general". The player still chooses 3 types. |
| **Endless** | Veterans from the endless roster (kept for all time) fill the veteran slot. Many levels: the veteran can be wounded and be out, the general slots keep the run alive. | Veteran leads from the endless roster. A long run has no dead end. | The player takes more risk for more power. Veterans are wounded over many levels, so the player must go general for some levels. |
| **Risk** | The split can feel like a rule, not a choice. | Veterans are weaker than in A because only the first unit is special. | Needs the most measuring. |

**Recommendation: B.** Reason: the slot is always a type, so the squad choice stays simple (3 slots, 3 types) and a dead lead never stops the player. A veteran is a bonus on top of a type. This fits "depth from upgrade paths" and keeps the weekly challenge fair with no special case. A is the safe fallback if B is hard to measure.

**Sub-question for Yaroslav:** in the weekly challenge, is "no veterans" a feature (pure skill, equal for all)? Recommendation: yes.
