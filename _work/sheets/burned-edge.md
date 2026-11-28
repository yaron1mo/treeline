# 15. Burned edge

Category C · Difficulty: Medium · Fits: Campaign: chapter 3, teaches "not all trees are cover". Endless: 7 waves.

**Idea.** Our forest edge is in the south. In front of it is a burned strip that looks like trees but hides nothing and gives no cover. The player learns that a burned belt is a trap for both sides.

## Defender functions

1. The living forest edge: real cover and sight block. Hold here.
2. Dugouts in the forest edge: strong cover against indirect fire.
3. The burned strip: do not place units here. Enemies cross it in the open: it is a kill zone.
4. The road gap in the strip: vehicles use it. Wire in the gap.
5. The trench behind the forest: the last line.
6. Enemy cover is also bad: the burned strip is their problem, not ours. Their spotters in the north grove see us.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Road through the gap: vehicles and main infantry.
2. Across the burned strip: infantry in the open. Fast, deadly.
3. Round the strip on the sides: slow, hidden by the corner groves. Good vs. a weak flank.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
GGGGGGG,,=,,,GGGGGGG
GGGGGGG,,=,,vGGGGGGG
GGGGGGG,,=,,,GGGGGGG
GGGGGGG,,=,c,GGGGGGG
,,,,c,,,,=,,,,,,c,,,
,,,,,,,,,=uuvuuuuuuu
,,,,,,,,,=uuuuuuuuuu
,,,s,,,,,=uuuuuuuuuu
,,,,,W,,,=uuuuuuuuuu
,,,,,,,,,=uuvuuuuuuu
,S,,,,,,,=uuuuuuuuuu
,,,aa,,,,=,,,,,,,,,,
S,,,,,,,,6,,,,,W,,,,
,,,S,,,,,*,,v,,c,,,,
,,,,,,,,M=,,c,,,,,,,
,,,,,,,,,=,,,,O,S,,,
c,,,OS,c,=,,,,,SS,,,
,,,,,,,,,=,,v,,,,,,,
NNNNNNNNN=NNNNNNNNNN
NNNNNNNNN=NNNNNNNNNN
NNNNWNNNN=NNNNNNNNNN
NNNNNNNNN=3NvNNWNNNN
NNNNNNNNN=WNNNNNNNNN
NNNXNNNXN=NNXNNNNNNN
....Sc...4=S........
....S....==.v.......
BB*BBBBBBBBBBBBBBBBB
BBBBzBBBBBBBBBBzBBBB
BBBB1BBBBdBBBBB2BBBB
BBBBBBBBBBBBvBBBBBBB
BBBBBBBBBBBBBBBBBBBB
BBBBBBBBBBBBBBBBBBBB
BBBBBBBBBBBBBBBBBBBB
,,EEEEEEEEEEvEEEEE,,
GGTTTTTTT5TTTTTTTTGG
GG,,,,,c,,*,,,,,,,GG
GGuu,BBBBBBBBBB,i,GG
GGuu,BBBBBBBBBB,,,GG
GGuu,BBBBBBBBBB,,,GG
GG,,,BBBBBBBBBB,,,GG
```

Legend: `,` Crop rows / stubble; `B` Forest belt; `N` Burned belt; `G` Grove / forest; `u` Sunflower field; `.` Empty ground; `=` Road; `T` Trench; `E` Embankment; `S` Bush; `v` Power-line pole; `c` Crater; `W` Wreck / vehicle; `*` Small decor (well, hive, scarecrow, bus stop); `X` Barbed wire; `d` Dugout; `a` Hay / straw stack; `O` Rock; `z` Camouflage net; `s` Shed; `M` Spoil heap; `i` Silo / tower / windmill; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 9 | 236 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `B` | Forest belt | 2 | 173 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `N` | Burned belt | 2 | 107 | Looks like cover, hides almost nothing, gives no cover. | Sim now |
| `G` | Grove / forest | 4 | 80 | Soft sight block and cover inside. | Sim now |
| `u` | Sunflower field | 2 | 64 | Hides who is inside, gives no cover. | Sim now |
| `=` | Road | 2 | 27 | Fast route for vehicles and infantry. | Sim now |
| `T` | Trench | 1 | 16 | Defender cover. The player can build more. | Sim now |
| `E` | Embankment | 2 | 15 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `S` | Bush | 7 | 10 | Hides, gives no cover. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `c` | Crater | 9 | 9 | Decoration. Marks past fights. | Sim now |
| `W` | Wreck / vehicle | 5 | 5 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `X` | Barbed wire | 3 | 3 | Slows infantry a lot. Player builds it. | Sim now |
| `d` | Dugout | 3 | 3 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `a` | Hay / straw stack | 1 | 2 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `O` | Rock | 2 | 2 | Rock-like cover. | Sim now |
| `z` | Camouflage net | 2 | 2 | Hides what is under it. No cover. | NEW Sim row |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |
| `M` | Spoil heap | 1 | 1 | Rock-like cover. | Sim now |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |

**Empty ground:** 31 of 800 cells = 4% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 30% of the map. They are decoration and the open cells of the field between objects.

## Life

- Black smoke rises from the burned stumps (animated). Ash drifts in the wind.
- Crows sit on the burned trees and fly up when units move.
- Wrecks burn on the strip.
- Embers glow at night.

## Winter and night

- **Winter:** Snow on ash: the strip becomes white: very easy to see.
- **Night:** Embers on the strip give light: the enemy is lit while crossing. A good night map.
