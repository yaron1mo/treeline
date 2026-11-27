# Map grammar and 20 designed maps

Proposal by the designer agent, 2026-11-27, for issue #41. Status: **Open**. No "Decided" item in `progression.md` is changed. Units are from [unit-roster.md](unit-roster.md) (PR #28).

## 1. Goal

Each map is **designed** around one gameplay idea. Real data (OpenStreetMap) gives only the look (field shapes, crop tones, the curve of a track), never the layout. Forest belts is the reference. After Yaroslav approves the list, Mistral builds a rule-based generator from the grammar. Every map is checked in the Sim.

## 2. Player experience

The player sees the field and reads it in a few seconds: "the enemy must pass the gaps; I hold here." Each map asks one question ("where do I stop the vehicles?", "who covers the back?"). A session of 7–10 waves lasts 3–5 minutes. The same grammar gives 20 maps that feel different, because the gaps, the road and the one special element change the best squad.

## 3. What Forest belts does (why it works)

| Part | Job in play |
| --- | --- |
| 4 belts across the field | Each belt blocks sight and gives cover. It splits the field into 5 strips and so into a rhythm of "see, hold, fall back". |
| 1–2 gaps per belt, shifted from belt to belt | The route bends. The player chooses where to hold: at a gap, or behind it. |
| A belt along the route (pocket) | A safe place for a mortar or a sniper, and a danger place if the enemy gets in. |
| A road behind a belt | Fast side movement for both sides. A hold point for the runners. |
| Tracks at gaps | Vehicle lanes. They tell the player where vehicles come. |
| Groves, pond, craters | Small cover points and obstacles. They make each strip different. |
| Fields in 3 tones | The look only. No effect. |

## 4. Map grammar

### 4.1 Elements

Existing rows are in `obstacles.csv`. "New" means the Sim needs a small change (Claude checks, section 7).

| Element | In Sim | What it does in play |
| --- | --- | --- |
| Belt | `belt` | Line of soft cover and blocked sight. Infantry cost 2, vehicle cost 3. |
| Gap | no object | Hole in a belt. The only cheap way through. |
| Pocket | `belt` along the route | A strip closed on 3 sides. A place to hold or a trap. |
| Road | `road` | Fast side movement (infantry 0.8, vehicle 0.5). |
| Track | `tracks` | Lane to a gap. Look only for infantry. A hint for vehicles. |
| Grove | `grove` | Small cover and blocked sight. |
| Pond | `pond` | Blocks all ground units. |
| Craters | `craters` | Look and small cover. Never a main element. |
| Field | `field` | Look only. |
| Trench line | `trench` | The player builds it (cost 70). On a map it can exist at the start, as the free hold point. |
| Ruined farm | `rock` and `wreck` group | A block of hard cover with 1–2 doors. A strong hold point for both sides. |
| Ravine with a crossing | `balka` and a gap | Slow crossing (infantry 1.2, vehicle blocked). The crossing is the only vehicle lane. |
| River with a bridge | `river` and `ford` | Infantry slow (4). Vehicles cross only at the bridge or ford. |
| Sunflower field | New (`bush` zone) | Hides what is inside. No protection. Both sides lose sight. |
| Burned belt | New (`belt` with no cover) | Looks like a belt, gives no cover and no sight block. Only slows. |
| Hill | `hill` | Range and sight bonus on top. A hold point for the sniper. |

### 4.2 Rules

| Rule | Value | Reason |
| --- | --- | --- |
| Field size | 200 × 400 m (as Forest belts); portrait; attackers from the top, base at the bottom | Phone screen |
| Belt spacing | 70–100 m | The weapon range of the infantry is about this; each strip is one fight |
| Belts per map | 2–4 (2 on short maps) | Rhythm |
| Gap width | 16–20 m; a "wide gap" is 30 m, only as a weak point on purpose | Narrow enough to hold with 2 units |
| Gaps per belt | 1–2 | One gap = a choke point. Two = a choice. |
| Gap shift | Next belt gap is at least 50 m from the one above | Route bends |
| Lanes (ground routes from spawn to base) | 2–3 | One lane = no choice. More than 3 = no pressure. |
| Vehicle lanes | 1–2 (gap + track) | The vehicle route must be learnable |
| Hold points | 3–5 places with cover and a view on a gap | The player must have a choice, but not a free one |
| Spawns | 1–2 (2 only on "two directions" maps) | Readable |
| Special elements | At most 1 new element per map | Each map teaches one thing |
| Look | Fields in 2–3 tones from OSM-style shapes; tracks from real curves | Look only |

## 5. 20 map concepts

Sketch key: top = attacker side; each row = 40 m; each column = 10 m. `#` belt, `.` gap or open, `=` road, `T` track, `o` grove, `~` water, `X` trench or ruin, `^` hill, `:` ravine, `*` sunflower, `b` burned belt, `B` base. Sketches show only the main idea. Difficulty: 1 (easy) – 5 (hard). "Fits": C1–C4 are campaign chapters (to be set, see question 3); E is endless.

Sketch for map 1 (the reference) — the others follow the same style:

```
#########.##########   <- belt 1, gap right
....................
#######.############   <- belt 2, gap left
==================== road
############.#######   <- belt 3, gap right
....................
#####.##############   <- belt 4, gap left
....................
```

### 5.1 Group A: 12 maps on the Forest belts grammar (no new element)

| # | Name | Idea (what the player learns or solves) | Layout | Favours | Diff | Fits |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Sector Birch | The reference: zig-zag gaps teach "hold at the gap or behind it". | 4 belts, 1 gap each, shifted; 1 road behind belt 2; 2 tracks | Rifleman, machine gunner | 1 | C1, E |
| 2 | Sector Twin Gap | Each belt has 2 gaps. The enemy splits, so the player must pick a side. | 3 belts, 2 gaps each, shifted | Mortar (area damage), recon drone | 2 | C1 |
| 3 | Sector Needle | One gap in each belt, all in one line: a long straight lane. "Stack your fire." | 3 belts, gaps aligned, grove at each side | Machine gunner, mortar | 1 | C1 (tutorial 2) |
| 4 | Sector Pocket | A belt runs along the route: a pocket next to the gap. Holding the pocket gives a flank shot. | 3 belts; 1 long side belt; 1 pocket | Sniper, rifleman | 2 | C1 |
| 5 | Sector Highway | A road runs along the whole front, behind belt 1. Runners go fast. "Cover the width." | 3 belts; road behind belt 1; 3 tracks | Machine gunner (vs. motorbike, buggy) | 2 | C1–C2 |
| 6 | Sector Switchback | 4 belts with the gaps at the far edges, alternating: the longest route, a long time under fire. | 4 belts; gaps at left, right, left, right | Mortar, sniper | 2 | C2, E |
| 7 | Sector Funnel | Belts form a V: the lanes join at one gap near the base. "One strong point." | 3 belts in a V shape; 1 end gap | Anti-tank team (the vehicle must come in) | 3 | C2 |
| 8 | Sector Fork | The route splits at belt 1 into 2 lanes that join again at belt 3. Two ways, same end. | 3 belts; side belts make 2 lanes | Recon drone (shows the lane used) | 3 | C2 |
| 9 | Sector Orchard | Many groves, few belts. Sight is short everywhere. "Who sees first wins." | 2 belts; 6 groves | Recon drone, sniper | 3 | C2–C3 |
| 10 | Sector Long Shadow | Only 2 belts, very wide strips (120 m). Long range matters; cover is thin. | 2 belts; 1 gap each; 1 grove | Sniper, FPV operator | 3 | C3 |
| 11 | Sector Double Front | Two spawns (left and right top). The enemy comes from two sides at once. | 3 belts; 2 spawns; gaps near the middle | Mortar (reach), recon drone | 4 | C3, E |
| 12 | Sector Gauntlet | 4 narrow strips (60 m). The fight never stops; there is no safe place to fall back. | 4 belts, spacing 60 m, 1 gap each | Machine gunner, air defence (waves of drones) | 4 | C3–C4 |

### 5.2 Group B: 8 maps with one new element each

| # | Name | New element | Idea | Layout | Favours | Diff | Fits |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 13 | Sector Trenchline | Trench line | The enemy trench line is the second belt: it exists at the start. The player takes it by holding the gaps. | 2 belts + 1 trench line (free cover); the rest as Forest belts | Rifleman, machine gunner | 2 | C2 |
| 14 | Sector Farmstead | Ruined farm | A ruin in the middle is a strong hold point. Both sides want it. | 2 belts; 1 ruin with 2 doors in the middle strip | Mortar (cover), sniper | 3 | C2–C3 |
| 15 | Sector Gully | Ravine with a crossing | A ravine blocks vehicles except at one crossing. "The crossing is the kill zone." | 2 belts; ravine across the middle; 1 crossing | Anti-tank team, FPV operator | 3 | C3 |
| 16 | Sector Bridge | River with a bridge | One bridge, one ford. Vehicles must use them; infantry swim slowly. | 1 belt; river across; bridge + ford | Anti-tank team, mortar | 3 | C3 |
| 17 | Sector Sunflower | Sunflower field | A big sunflower field hides the lane. The enemy appears late. "Do not wait for the view." | 2 belts; 1 sunflower field in strip 2 | Mortar (area), recon drone | 3 | C3 |
| 18 | Sector Ash | Burned belt | The belts burned: no cover, no sight block. The fight is in the open; range and fast fire matter. | 3 burned belts; 1 real belt at the base | Machine gunner, sniper | 4 | C3–C4, E |
| 19 | Sector Hill 7 | Hill (on top of the route) | A hill gives range and sight. The player holds it, the enemy must take it. | 2 belts; 1 hill left of the lane | Sniper, recon drone | 3 | C2–C3 |
| 20 | Sector Last Line | Wide gap (30 m) | The final map: one wide gap near the base. The base is close; the pressure is high. | 4 belts; wide gap in belt 4 | Whole squad; air defence, anti-tank | 5 | C4 end, E |

Each sketch of the 20 maps is built by the generator from the "Layout" column (see section 7). Mistral does not need a picture per map; the table is the spec.

## 6. Winter and night (later)

| Change | Effect on the grammar | Maps that change the most |
| --- | --- | --- |
| Night | Sight drops. Groves and belts hide more. Special forces see at night. Recon drones matter more. | 9 Orchard, 17 Sunflower, 10 Long Shadow |
| Winter | Belts lose leaves: sight through a belt is half (belt becomes "thin"). Ponds and rivers freeze: they stop blocking infantry and vehicles. | 16 Bridge (the river is open), 4 Pocket, 6 Switchback |
| Fog | Short sight everywhere. Mortar loses its "seen" bonus. | 8 Fork, 12 Gauntlet |

Rule: a modifier must change the best squad, not only the look. Claude measures this per map.

## 7. Connection to decided items, and what Claude must build or measure

- **Squad slots (3), enemy variety from modifiers (night, fog, two directions):** the modifiers above use only this list. No new enemy types.
- **Endless: different maps of different length:** each map has a length range (7, 10, 20 waves). Short maps (2–3 belts) are 7 waves; long ones (4 belts) are 10–20.
- **Forest belts is the reference; Balka and River crossing not a priority (decided):** maps 15 and 16 use these elements only as one element each. The order of work is a question (question 4).
- **No pair hint:** the "Favours" column is for Mistral and for the forecast screen. The player does not see it.
- **Real maps rejected:** OSM is used only for look.

Claude must build or measure:
1. Check the Sim for the new elements: trench at start, ruin group, ravine with a crossing, river with a bridge, sunflower zone (sight only), burned belt (no cover, no sight block), wide gap.
2. Write the grammar as a data table (`maps_grammar.csv`): element, spacing, gap width, lanes. Mistral reads it.
3. Check every generated map: at least 2 ground routes; at least 3 hold points; a vehicle route exists; no route longer than the set limit.
4. Measure per map: the best squad. A map fails if the same squad wins all maps (`mistral/squads`).
5. Check the 12 + 8 split (12 on the Forest belts grammar, 8 with one new element each).

## 8. Open questions

1. **The new elements: how many to build first?**
   a) All 7 (13–20). b) Only the cheap ones (trench, ruin, wide gap, burned belt, hill), and leave the ravine, river and sunflower for later. c) None at first; build the 12 in group A.
   Recommendation: b). Reason: those 5 use existing rows or small changes; the ravine and river were not a priority (decided).
2. **Burned belt and sunflower: a new rule or a new look?**
   a) New rule in the Sim (as in the table). b) Look only (a belt and a grove with another picture).
   Recommendation: a), but only for the burned belt. Reason: "no cover" changes the play; the sunflower can be a grove with a new look.
3. **Campaign chapters (C1–C4).** The chapter list is open in `progression.md`. The "Fits" column is a first guess.
   a) Accept C1–C4 as a working frame. b) Wait for the campaign design.
   Recommendation: a). Reason: it costs nothing to change later.
4. **Order of the build.**
   a) First all of group A, then group B. b) The easiest map of each chapter first.
   Recommendation: b). Reason: a full game slice is playable earlier and tests the squad rule.
5. **Map names.** Fictional "Sector + word". Is that enough, or does each map need a short story name?
   a) Keep the plain names. b) Add a call-sign style name for each map.
   Recommendation: a). Reason: no extra text work now.
