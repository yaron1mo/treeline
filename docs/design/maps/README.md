# Map concept sheets (22 maps)

Designer agent, 2026-11-27, for issue #48. Proposal. Yaroslav decides which maps stay.
Write all text in ASD-STE100. One sheet per map. Each sheet has: idea, defender functions, attacker routes, a 20 × 40 layout, a fill list, life, winter and night.

## How to read a layout

- One character = 10 m. The map is 200 m × 400 m (20 columns, 40 rows). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.
- The digits `1`–`9` mark the defender functions in the numbered list of the sheet.
- The grids were drawn with a script and checked for the 20% rule. The script is not part of the repo (the grids are the source).

| Char | Object | Sim |
|---|---|---|
| `.` | Empty ground | Decoration only |
| `,` | Crop rows / stubble | Sim now |
| `H` | Hill | Sim now |
| `B` | Forest belt | Sim now |
| `G` | Grove / forest | Sim now |
| `N` | Burned belt | Sim now |
| `R` | River | Sim now |
| `F` | Ford | Sim now |
| `Q` | Ruined bridge | Sim now |
| `K` | Gully (balka) | Sim now |
| `E` | Embankment | Sim now |
| `P` | Pond | Sim now |
| `M` | Spoil heap | Sim now |
| `O` | Rock | Sim now |
| `W` | Wreck / vehicle | Sim now |
| `S` | Bush | Sim now |
| `T` | Trench | Sim now |
| `X` | Barbed wire | Sim now |
| `Y` | Hedgehog | Sim now |
| `=` | Road | Sim now |
| `:` | Field track | Sim now |
| `c` | Crater | Sim now |
| `h` | House | Sim now |
| `r` | Ruined house | Sim now |
| `f` | Fence | Sim now |
| `u` | Sunflower field | Sim now |
| `p` | Pipe | Sim now |
| `a` | Hay / straw stack | NEW Sim row |
| `s` | Shed | NEW Sim row |
| `i` | Silo / tower / windmill | NEW Sim row |
| `z` | Camouflage net | NEW Sim row |
| `d` | Dugout | NEW Sim row |
| `v` | Power-line pole | Decoration only |
| `*` | Small decor (well, hive, scarecrow, bus stop) | Decoration only |

## Rules check

| Rule | How the sheets keep it |
|---|---|
| Defender functions | Each sheet lists 6–7 functions (hold points, choke points, flanks, high ground, cover lines, kill zones, places for drones and mortars) and shows them on the layout. |
| At most 20% empty ground | The empty ground (`.`) is only the kill zone in front of our line. See the table below (all maps are under 20%). |
| Alive, not empty | Each sheet has a fill list with a game role for each object and a "Life" list. |
| Sim elements | Existing rows are used first. Five new rows are proposed (next section). |

## New Sim rows that the maps need (Claude adds them)

| Id | Object | Proposed behaviour (numbers: Claude) | Used in |
|---|---|---|---|
| `stack` | Hay / straw stack | Like a heap: cover behind it. Burns when hit (smoke). | all maps |
| `shed` | Field shed | Like a small house: blocks sight and movement. | farm maps, villages |
| `silo` | Silo, water tower, windmill | Rock-like block with a thin sight line. Landmark. A spotter can stand on it. | 14 maps |
| `camo_net` | Camouflage net | Like a bush: hides what is under it, no cover. The player can build it later. | Enemy heights, Forest road, others |
| `dugout` | Dugout | A trench with a roof: strong cover against indirect fire and aircraft. | most maps |

Decoration only (no Sim row): power-line poles with fallen wires, wells, beehives, scarecrows, bus stop. Cars, tractors and combines use the `wreck` row.
Sim rules that exist since 2026-11-27 and are used: `house`, `ruin`, `fence`, `sunflower`, `burned_belt`.

## Index

| No. | Map | Cat. | Difficulty | Empty | One line | Sheet |
|---|---|---|---|---|---|---|
| 1 | Ruined bridge | A | Medium | 6% | A river cuts the field in two. The bridge is broken, so vehicles must queue at one ford far on the side. | [ruined-bridge](ruined-bridge.md) |
| 2 | River bend | A | Medium | 6% | The river bends deep into the field. The attack runs along the west bank. | [river-bend](river-bend.md) |
| 3 | Confluence | A | Hard | 8% | A tributary meets the main river in a Y. The attack splits into a west lane and an east lane. | [confluence](confluence.md) |
| 4 | Overlook | A | Easy | 2% | One ford, and our hill stands right above it. | [overlook](overlook.md) |
| 5 | Enemy heights | A | Hard | 5% | The enemy owns the hill on its own bank and sees our line. | [enemy-heights](enemy-heights.md) |
| 6 | Left hill | B | Medium | 6% | A big hill covers the left flank. Whoever holds it sees half the map. | [left-hill](left-hill.md) |
| 7 | Ridge | B | Medium | 5% | A ridge crosses the field. The attack must go through two saddles. | [ridge](ridge.md) |
| 8 | Valley | B | Medium | 4% | Two hills with a valley between them. The attack goes through the valley. | [valley](valley.md) |
| 9 | Long slope | B | Hard | 7% | The ground slopes down to our line. The enemy starts high and sees us. | [long-slope](long-slope.md) |
| 10 | Gully mouth | B | Medium | 6% | A gully runs along the line of advance and ends in front of our hill. | [gully-mouth](gully-mouth.md) |
| 11 | Clearings | C | Medium | 2% | A big forest with clearings. Sight is short. | [clearings](clearings.md) |
| 12 | Forest road | C | Hard | 0% | One wide road runs through a dark forest. This is the map of ambushes: the defender hides on both edges and shoots across the road. | [forest-road](forest-road.md) |
| 13 | Forest and steppe (was Half forest) | C | Medium | 8% | Rework of Half forest. Forest on the left, open steppe on the right, a road between. | [forest-and-steppe](forest-and-steppe.md) |
| 14 | Grove islands | C | Medium | 6% | Eleven groves of different size stand in the field like islands. | [grove-islands](grove-islands.md) |
| 15 | Burned edge | C | Medium | 4% | Our forest edge is in the south. In front of it is a burned strip that looks like trees but hides nothing and gives no cover. | [burned-edge](burned-edge.md) |
| 16 | Open steppe | D | Hard | 7% | Almost no trees. Long sight lines. | [open-steppe](open-steppe.md) |
| 17 | Sunflower | D | Hard | 0% | A big sunflower field fills the middle of the map. | [sunflower](sunflower.md) |
| 18 | Old trenches | D | Medium | 6% | Three old trench lines with wire cross the field. | [old-trenches](old-trenches.md) |
| 19 | Wreck field | D | Medium | 8% | A destroyed column of vehicles lies in a diagonal line across the field. | [wreck-field](wreck-field.md) |
| 20 | Farmstead (replaces Tree line) | D | Easy | 8% | Replaces Tree line. A farm with a silo, sheds and a combine stands north of a thin tree line. | [farmstead](farmstead.md) |
| 21 | Village street | V | Medium | 7% | A long village street from the enemy edge to our line. | [village-street](village-street.md) |
| 22 | Crossroads | V | Hard | 7% | Two village streets cross in the middle. Whoever holds the houses at the crossroads controls both routes. | [village-crossroads](village-crossroads.md) |

Categories: A river, B hills, C forest, D open steppe, V village.

## Changes against the 20 concepts of issue #46

| Map | Change | Why |
|---|---|---|
| Enemy heights | Kept, reworked | Added camouflage nets, dead ground behind the embankment and a counter-hill. Now it is a game about hiding, not shooting first. |
| Forest road | Kept | One lane, but it is a lane of ambushes. Nests on both edges. |
| Grove islands | Kept, more drawing | 11 groves of different size, each with its own object (ruin, shed, pond, silo). |
| Half forest | Reworked, now "Forest and steppe" | Added a farm that joins both halves, so the choice "forest or steppe" is a real choice. |
| Gully mouth | Kept, gully made clear | The gully is 2 cells wide, runs 25 cells and has spoil heaps on both banks. It ends in a kill zone. |
| Tree line | Replaced by "Farmstead" | A single tree line is weak by itself. The farm gives the map a goal. The one tree line stays as a belt. |
| Open steppe | Kept, with the kurgan | A burial mound is the one hill. Without it the steppe gives the defender no tools. |
| Village street, Crossroads | Added (Claude's prototypes) | The layout is taken from the Sim field files. The sheets add functions, fill and life. |

## Decisions of Yaroslav (2026-11-28)

1. **Maps:** keep all 22. Cut later, when the Balance run shows which maps play the same.
2. **Crop rows:** they count as filled (decoration, 20–40% of a map). If a map looks empty on screen, add objects.
3. **New Sim rows:** add all five (`stack`, `shed`, `silo`, `camo_net`, `dugout`).
4. **Designed maps and real terrain:** the sheets are the target. Real terrain gives only the style.
5. **Campaign order:** decide with the campaign structure (issue #21).

## What Claude must build or measure

1. Add the five Sim rows (if question 3 is approved).
2. Build each approved sheet as a field file in `unity/Assets/Resources/Fields/`. Check each one in the Balance run: no wave "not held", and the lanes in the sheet are the lanes the attackers use.
3. Check that a hill is visible on screen (Yaroslav's note on the rejected maps) and that the sight rules agree with the "Defender functions" (for example: the Overlook hill sees the ford).
4. Check that no map has a dominant hold point: the best hold point must not win every wave with the same squad.
5. List the animations for Codex: moving tractor and combine, smoke, birds, water, mist, flags (from the "Life" sections).
