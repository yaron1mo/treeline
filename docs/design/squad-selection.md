# Squad selection and slots

Proposal by the designer agent, 2026-11-27, for issue #19. Write-up status: **Open**. Yaroslav decides. Nothing here changes a "Decided" item in `progression.md`.

## 1. Goal

Before each mission the player takes a small squad. The choice must be hard: the wave needs some unit types, the player wants others. No single squad wins every mission. The choice takes less than one minute, so a session stays 3–5 minutes.

## 2. Player experience

The player opens a mission and sees a short brief: the map, the modifier (for example night) and a forecast of the wave. The player has a few slots and a barracks of units. The player puts one unit in each slot. The forecast says "vehicles from the east, drones late", so the player must give up a favourite unit for a counter. In the battle, the player builds only the types in the squad. After the battle, the units that fought are tired and need a rest, so the next mission needs a different mix.

## 3. Rules

### 3.1 Slots

- One slot holds one **unit type with its upgrade path** (for example "sniper" or "marksman"). The player can build many units of that type in the battle, for money.
- A slot is filled by one named unit from the barracks. That unit is the "lead" of the slot: it keeps its call sign and its upgrades.
- Slots open with the campaign, not with money.

| Slots | Opens when | Proposal |
| --- | --- | --- |
| 3 | Start | Enough for one counter per class: infantry, vehicle, aviation |
| 4 | End of chapter 1 | First free choice |
| 5 | End of chapter 2 | |
| 6 | End of chapter 3 | Maximum. Fewer than the unit types the player owns, so there is always a choice |

The player can own more types than slots from the start of chapter 2. The slot count must stay lower than the number of owned types at all times.

### 3.2 Forecast (the wave brief)

The brief shows the wave as icons by class (infantry, heavy, fast, aviation) and by side of entry. Intelligence improves it. Weather and EW modifiers make it less exact.

| Intelligence level | The brief shows |
| --- | --- |
| 0 (start) | Class icons for the first 3 waves. Number of entry sides. |
| 1 (reconnaissance path) | Class icons for all waves. Entry side per wave. |
| 2 | Exact unit types and counts. Time of each wave. |
| 3 | As level 2. Also shows the modifier effect, and one change of orders by the enemy (route learning). |

The forecast is a promise, not a lie. A level 0 brief can hide a wave, but it must not show a wrong class. Claude checks that the wave generator can give this data.

### 3.3 Fatigue and wounds

- A unit that fought in a mission gets **fatigue** (one step per mission). A tired unit has a weaker start: lower HP in the battle. At the maximum fatigue the unit cannot join the next mission.
- A rested unit loses fatigue after one mission in which it is not used. Money can speed this up (no ads needed).
- A wounded unit (see the open item "Death or wounded") is out for N missions. N is for the balance table.
- Result: the player needs 2 units for a type that it uses often. This makes the barracks matter and supports "depth from upgrade paths": two riflemen with different paths are both useful.

### 3.4 Pairs

A pair is two types that make each other stronger **through existing rules**, with no new bonus number:

| Pair | Why it works |
| --- | --- |
| Recon drone + mortar | Mortar fires at targets that any defender sees (shared vision). The drone sees far. |
| Machine gunner + anti-tank team | Infantry with an IFV: the gun kills the infantry, the missile kills the vehicle. |
| Sniper + recon drone | The sniper has long range but a short sight. The drone gives the targets. |
| Air defence + FPV operator | Air defence stops drones. The FPV operator kills what the air defence cannot reach (vehicles). |

The brief marks a pair with a link line when both units are in the squad. This is only a hint.

### 3.5 Weekly challenge and endless

| Mode | Squad rule | Reason |
| --- | --- | --- |
| Weekly challenge | The challenge gives a fixed pool of 6 types. The player takes 4. Units are "loaned": no fatigue, no upgrades from the story roster. | Same for all players, so the score is fair. Donations stay the main reward. |
| Endless | Own roster, all open slots. The brief shows only the next wave at level 0. The squad can change only after wave 10, 20, 30… (a short "reinforcement" stop). Fatigue is off. | A run has no fixed end, so the player needs a way to react to a new threat. |

## 4. Two examples

**Example A: chapter 1, mission "Forest belts", night, 3 slots.**
Forecast (level 0): first 3 waves: infantry, infantry, fast. One entry side.
Owned: rifleman, machine gunner, mortar, sniper.
Choice: rifleman (cheap base), machine gunner (infantry and fast units), and one of mortar or sniper. At night the sight is short, so the sniper cannot use its range. The player takes the mortar and puts a rifleman in front. The risk: a vehicle is not in the brief. The player has no anti-tank unit and must sell time with the mortar.

**Example B: chapter 3, mission "Open steppe", fog, 5 slots, intelligence level 2.**
Forecast: 4 infantry waves, then 2 IFVs and 1 one-way drone wave. EW makes the FPV unreliable.
Owned: rifleman, marksman, machine gunner, sniper, mortar, anti-tank, air defence, FPV operator, recon drone.
Choice: machine gunner (infantry waves), anti-tank (IFVs), air defence (the drone wave), recon drone (fog hides far targets), mortar (pair with the recon drone). The player leaves the sniper and the FPV operator in the barracks. The sniper has a short sight in fog. The FPV is weak in EW. The squad is good for this mission and weak for a night mission with infantry only.

## 5. Connection to decided items

- Uses "Squad before each mission", "Slots open with progress" and "One squad that wins everything is a failure".
- Uses "Depth from upgrade paths": slot = type + path.
- Uses "Mission modifiers" and the intelligence branch (forecast levels).
- Keeps the weekly challenge decided rules (same map, seed and modifier). It adds a fixed pool.
- Uses the decided "Mortar: starting field unit" and shared vision for pairs.

## 6. What Claude must build or measure

1. **Squads measurement (`mistral/squads`):** for each mission, test all squads of N slots. Pass: no squad wins more than a set share of the missions, and each hard-counter type is in the best squad for at least one mission.
2. Check the wave generator can output the forecast levels (class icons, entry side, time).
3. Run setup: add a squad (list of unit `id` and path) to the run setup. Add fatigue as a data table (steps, HP loss, rest).
4. Measure fatigue effect on win rate, so the 2-unit rule does not make the game too hard.
5. Check the pair table with the sim. Remove a pair that gives no gain.

## 7. Open questions

1. **How many slots at start?**
   a) 3 (proposal). b) 4. c) 2.
   Recommendation: a). Three slots give a counter for each class, and the choice is still hard. Reason: the measure `mistral/squads` can show if 4 is too easy.
2. **What does one slot hold?**
   a) One type, the player builds many (proposal). b) One unit, one body in the battle. c) A type with a fixed count (for example 3).
   Recommendation: a). It keeps the battle simple and the choice before it. Option b) fits "unit = one defender" more, but the battle gets very small.
3. **Fatigue: yes or no?**
   a) Fatigue as in 3.3. b) Only wounds (when the open item is decided). c) Neither; the barracks is only a store.
   Recommendation: a) with b). Reason: it makes the player use more units and keeps names alive. Risk: it can feel like a penalty. Claude measures.
4. **How exact is the forecast at the start?**
   a) Classes for 3 waves (proposal). b) All waves, classes only. c) Nothing; the intelligence branch is the only source.
   Recommendation: a). Reason: the player learns to read the brief, and the intelligence branch has a clear value.
5. **Weekly challenge squad?**
   a) Fixed pool of 6, take 4, loaned units (proposal). b) Own squad. c) Fully fixed squad.
   Recommendation: a). Reason: fair score, and still a choice. Option b) makes a leaderboard unfair.
6. **Endless: can the squad change?**
   a) Every 10 waves (proposal). b) Never. c) Every wave.
   Recommendation: a). Reason: a hard late wave must not be unwinnable because of an old choice, and c) removes the choice.
7. **Pairs: hint only, or a small bonus?**
   a) Hint only (proposal). b) A small bonus in a data row.
   Recommendation: a). Reason: the rules already make the pairs strong, and a bonus is one more number to balance.
