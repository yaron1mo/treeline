# Unit progression: upgrade paths, experience, wounds, barracks

Proposal by the designer agent, 2026-11-28, for issue #20. Status: **Open**. This file changes no "Decided" item in `progression.md`.

## 1. Goal

Give each base unit 2–3 upgrade paths. Each path changes how the unit plays, not only its numbers. Give the player a reason to keep a unit alive and a cost when it falls. Keep the 3 meta branches (Assault, Drone forces, Intelligence).

## 2. Player experience

The player has a small barracks of named units. After a battle, the units that fought well have experience. The player picks one unit and sees its tree: two or three paths, three steps each. The player reads one line for each step ("it now needs a spotter", "it now holds a lane"). The player chooses a path, and the unit changes how it plays on the next map. A unit that falls in battle can be wounded and come back, or be killed. The player feels the loss of a unit with a name and a history. A session stays short: upgrade, pick the squad, play 3–5 minutes.

## 3. Rules: upgrade paths

Rule for all trees: a path has 3 steps. Step 1 is a small change. Step 2 is the "this unit plays differently now" step. Step 3 is the sign of the path (a veteran look; the skull gets a badge). A step above step 1 needs the matching meta branch level (the branch gives the right, the unit pays the money). Two units of one type on two paths must play differently (decided).

### 3.1 Rifleman (base, Assault)

| Path | Steps | What changes in play |
| --- | --- | --- |
| **Marksman → Sniper** (Intelligence) | 1 Marksman: longer range, slower fire. 2 Sniper: one shot, picks a target by priority (officer, drone crew, EW team); needs a spotter or sight of the target. 3 Sniper veteran: first shot on a new target never misses. | The unit stops shooting at the crowd. The player must put it where it sees far, and must protect it from the fast units. |
| **Machine gunner** (Assault) | 1 Marksman (shared first step, decided). 2 Machine gunner: fire in a lane, never moves its aim far, strong against infantry and drones (low chance). 3 Veteran: suppression. Enemies in its lane move slower and shoot with lower accuracy. | The unit holds a lane. The player builds the line around it and it needs cover on the sides. |
| **Grenadier** (later, decided) | Not designed now. A free place in the tree. | — |

### 3.2 Mortar (base, Intelligence, fire support path)

| Path | Steps | What changes in play |
| --- | --- | --- |
| **Barrage** | 1 More shells per salvo. 2 Salvo on a marked point: the player taps a point on the route, the mortar fires 3 shells there, then a long reload. 3 Veteran: the marked point stays marked for the next salvo. | Area denial. The player plans a route and a point, and times the salvo for a group. |
| **Precision** | 1 Better accuracy. 2 One shell, small blast, hits a unit that stands in cover (EW station, anti-tank team, carrier that unloads). 3 Veteran: a recon drone in the zone makes the first shell a sure hit. | Sniper of the rear. It needs a recon drone and a good target. It is weak against a crowd. |
| **Smoke and flare** | 1 Reload speed. 2 Smoke shell: enemies inside cannot see our units (long-range shooters lose range). Flare: night sight for our side for a short time. 3 Veteran: smoke also hides our own units from enemy drones. | A support unit, no damage. It answers night, special forces and drone waves. |

### 3.3 Recon drone (base, utility slot; follows the decided drone line)

The decided line is recon → bomber → improvised FPV → factory FPV → fibre-optic FPV. This proposal keeps it as the main path and adds two side paths.

| Path | Steps | What changes in play |
| --- | --- | --- |
| **Observer** (Intelligence, reconnaissance) | 1 Longer sight. 2 Relocation: the player can move it once during a battle. 3 Veteran: its sight shows the forecast item of the level (see the decided forecast by intelligence level). | The drone becomes a tool the player moves. It finds the route and the target for mortars. |
| **Strike line** (Drone forces) | 1 Bomber drone (carries grenades). 2 Improvised FPV (single use, hits vehicles). 3 Factory FPV, then fibre-optic FPV (not stopped by EW). | The unit stops giving sight and starts to kill. Each step changes the target: troops, then vehicles, then vehicles under EW. |
| **EW carrier** (Drone forces) | 1 EW station with a small radius. 2 Two stations (the player can overlap them; two overlapping stations kill one Shahed, decided). 3 Veteran: the station sends a wave that also marks enemy drones (the player sees where they are). | The drone becomes a defence against drones. It must be placed with care, because the enemy shoots the station. |

## 4. Rules: experience

| Item | Proposal |
| --- | --- |
| Who earns | Each unit has its own experience. Experience is not shared. |
| How | A kill gives points by the enemy threat (recruit 1, tank 8 …; the table is data). A unit that is alive at the end of a won battle gets a small bonus. A kill by a support call gives nothing. |
| Levels | 3 veteran levels per unit. A level is a threshold of points. |
| What it does | It does not change the unit's numbers. It makes the next upgrade cheaper: level 1 is 50%, level 2 is 65%, level 3 is 80% (the decided range is 50–80%; exact values: balance). |
| "Spent" | The points are not spent. The discount is used when the player pays for a step. Rule: a unit has one discount at a time, for the next step. After the step, the unit keeps its level, and the next step is cheap again only if the unit has enough points for the new level. |

Why: the player is not asked to "spend experience" (one more currency). The player has one decision: money now at full price, or one more battle with this unit at a lower price.

## 5. Rules: wounded or killed, and evacuation

| Item | Proposal |
| --- | --- |
| At 0 HP | The unit is **wounded** ("300") or **killed** ("200"). The share is 50/50 (open item). The roll is made at the end of the battle, not at the moment of the hit. |
| Killed | The unit leaves the barracks. Its call sign goes to a "wall" (a screen with the names, with black humour). Its money and experience are lost. |
| Wounded | The unit stays in the barracks and rests for a number of missions: 1 mission for veteran level 0–1, 2 for level 2, 3 for level 3. A resting unit cannot go into the squad. This is the cost: with 3 slots, a missing unit makes the squad choice harder. No real-time timers. |
| Fast recovery | The player may end the rest at once for money (a small share of the upgrade value) or for 1 ad. The ad is the choice of the player and is never forced. |
| Evacuation (decided pop-up) | When a valuable unit (level 2 or higher, or with a step 2 upgrade) falls, the pop-up offers: "for N money or 3 ads we retreat and save <call sign>". If the player pays, the unit is **wounded** (not killed) with the normal rest. Without the pop-up, the 50/50 roll applies. |
| Lost battle | All fallen units that were not evacuated are killed. This is the pressure to retreat early (the decided retreat saves all units but gives no earnings). |
| Weekly challenge | Nothing carries over (decided). No wounds and no experience are saved. |

## 6. Rules: barracks

| Item | Proposal |
| --- | --- |
| Size at start | 6 units (5 riflemen and 1 mortar, each with a call sign). |
| Growth | +1 place for each campaign chapter, up to 12. Endless has its own barracks with the same rule. |
| Why more than 3 | The squad has 3 slots plus the drone slot (decided). The barracks is the bench: it holds the wounded, the units of the other paths, and the units for the next map. |
| Why not larger | A big barracks means many upgrades to track. 12 is enough for the whole counter. |
| Full barracks | The player cannot hire. The player may dismiss a unit (it goes to the wall without a loss message). |

## 7. How it connects to decided items

Uses: base unit and paths, upgrades with a 50–80% discount from experience, separate progress for each mode, death and the evacuation pop-up, retreat, three meta branches, infantry fork (sniper = intelligence, marksman = assault), one drone line, mortar in the fire-support path, 3 squad slots plus a drone slot, wounds only (no fatigue), forecast by intelligence level, "depth from paths, not from types", no loot boxes and no forced ads.

## 8. What Claude must build or measure

1. Data: a `unit_paths.csv` (unit id, path, step, requires branch level, cost, `behaviour` change). Special play ("salvo on a marked point", "relocation", "smoke") is a `behaviour` column, not code on a unit name.
2. New behaviours in `Sim`: lane suppression, marked-point salvo, smoke and flare, one relocation in battle. Check cost before we agree the paths.
3. The run result must return, per unit: kills by threat, alive or fallen, so the meta layer can give experience and wounds.
4. Measure: for each pair of paths of one unit, the best map and wave must differ (the decided rule "two units of one type play differently"). A path that wins on all maps is a failure.
5. Measure the 50/50 roll with the 3-slot squads: the average number of units resting at one time.

## 9. Open questions

1. **Wounded or killed: how is it decided?**
   - (a) 50/50 random roll at the end of the battle. Simple. The player can feel the roll as unfair.
   - (b) Depends on the enemy: heavy fire (tank, mortar, FPV) kills, small arms wound.
   - (c) 50/50, but each veteran level moves the share by 10% toward "wounded".
   - Recommendation: (a) for the first test, then (c) if the playtest says that veterans die too often. (a) is the simplest and it is the proposal of the open item; (c) rewards the unit the player took care of.
2. **How long does a wounded unit rest?**
   - (a) 1 mission for all.
   - (b) 1 to 3 missions by veteran level (this file).
   - (c) A real-time timer.
   - Recommendation: (b). The price of a valuable unit is higher, and a mission counter has no waiting and no forced ad. Not (c): it pushes the player to watch ads.
3. **What does the player lose when a unit is killed?**
   - (a) Everything (the unit, the money, the experience). This file.
   - (b) The unit is lost. The next hire of the same type starts with 25% of the experience ("the call sign passes on").
   - Recommendation: (a). (b) weakens the cost of death and the value of the evacuation pop-up.
4. **Barracks size.**
   - (a) 6 at start, +1 per chapter, 12 at most (this file).
   - (b) Fixed 8.
   - (c) Starts at 6; the player buys places with money.
   - Recommendation: (a). It gives a reward for each chapter and needs no new price. (c) would be a monetisation lever and needs a decision about the model first.
5. **Does a unit need the meta branch level for step 2 and 3?**
   - (a) Yes (this file). The branch gives the right, the unit pays.
   - (b) No. Only money and experience.
   - Recommendation: (a). It makes the three branches matter, so that no branch is dead (decided).
6. **Experience: a discount only, or a currency?**
   - (a) A discount only (this file).
   - (b) A currency spent with money.
   - Recommendation: (a). One fewer currency; the decided "50–80% cheaper" works as it is.
7. **Rifleman has only two paths.** Add a third path now?
   - (a) No. Grenadier and ATGM come later (decided).
   - (b) Add a "Fireteam leader" path: no weapon change; the unit holds cover better and the squad around it is less afraid of suppression.
   - Recommendation: (a). (b) is close to a pair bonus, and a pair bonus is decided against.
