# 13. Forest and steppe (was Half forest)

Category C · Difficulty: Medium · Fits: Campaign: chapter 3. Endless: 10 waves.

**Idea.** Rework of Half forest. Forest on the left, open steppe on the right, a road between. The forest gives cover but short sight. The steppe gives range but no cover. A small farm at our line joins both halves, so one squad can swing. The player must pick where to hold, and the enemy learns it.

## Defender functions

1. Forest nest (dugout and net): short range, high safety. Infantry and machine guns.
2. Stack line on the steppe: the only cover on the right. Snipers and ATGM-type units shoot over long range.
3. The farm (houses, shed): the hinge. Units inside see both halves of the line.
4. Embankment in the forest: cover for a second line.
5. The road wire: stops vehicles in the middle, in range of both halves.
6. The south trench: the last line for both halves.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Forest lane (left): infantry, slow, hidden. Good against a weak left.
2. Road (centre): vehicles. Fast, the defender sees them from both halves.
3. Steppe lane (right): fast infantry and vehicles in the open. Good against a weak right.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
GGGGGGGGG=,,,,,,,,,,
GGGGGGGGGSS,,,,,,,,,
GGGGGGGGG=,v,,,Ouuuu
GGGGGGGSG=,,a,c,uuuu
GGGGGGGGG=,,a,S,uuuu
GGGdzGGGG=,,,,,,uuuu
GGGGGGGGG=,v,W,,uuuu
GGGGGGGGG=,,,,,,,S,,
GGGGGGGGG=,,,,M,,,,,
GGG1GGGGG=,,,,,,,,O,
GGGGSGGGS=,v,,,aS,,,
GGGGGGGGGX,,,,caS,,S
GGGGGGGGGWc,,,,,,,,,
GGGGGGGGG=,,,,,,,,,c
GGdzGGGGG=,v,,*c,,,,
GGGGGGGGG=,,,,,,,M,,
GGGGGSGGG=,W,2,,,,,S
GGGGGGGGG=,,,c,,,,,,
GGGGGGGGG=,v,a,,,,,,
GGGGGGGGG=,,,a,,,,,,
GGGGGGGGG=,,M,S,,,,S
GGGGGGGGG=,O,,,,,,,,
GGGGGdzGG=5v,c,,,a,,
GGGGGGGGG=,,,,,,,a,,
.......GGX..........
.......GG=.........c
....*..SG=.v...W....
EEEE4EEEG=EEEEEEEEEE
GGGdGGGGG=..........
GGGGGGsGG=..sc...ff.
GGGGGGGGG=,v,3,hhff,
GGGGGGGGG=cS,i,hhff,
GGGGSGGGG=*,,,,,,,,,
GGGGGGGGG=,,,,,,,,,,
GGGGGGGGG=,vc,,,,,,,
GGTTTTTTT6TTTTTTTT,,
BBBBBBBBB=,,,,GGGGGG
BBBBBBBBB=ffffGGGGGG
BBBBBBBBB=ffffGGGGGG
BBBBBBBBB=,,,,GGGGGG
```

Legend: `G` Grove / forest; `,` Crop rows / stubble; `.` Empty ground; `B` Forest belt; `=` Road; `u` Sunflower field; `E` Embankment; `S` Bush; `T` Trench; `f` Fence; `c` Crater; `v` Power-line pole; `a` Hay / straw stack; `d` Dugout; `W` Wreck / vehicle; `h` House; `O` Rock; `z` Camouflage net; `M` Spoil heap; `*` Small decor (well, hive, scarecrow, bus stop); `X` Barbed wire; `s` Shed; `i` Silo / tower / windmill; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `G` | Grove / forest | 2 | 298 | Soft sight block and cover inside. | Sim now |
| `,` | Crop rows / stubble | 5 | 226 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `B` | Forest belt | 1 | 36 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `=` | Road | 5 | 35 | Fast route for vehicles and infantry. | Sim now |
| `u` | Sunflower field | 1 | 20 | Hides who is inside, gives no cover. | Sim now |
| `E` | Embankment | 2 | 18 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `S` | Bush | 15 | 17 | Hides, gives no cover. | Sim now |
| `T` | Trench | 1 | 16 | Defender cover. The player can build more. | Sim now |
| `f` | Fence | 2 | 14 | Hides, gives no cover. | Sim now |
| `c` | Crater | 11 | 11 | Decoration. Marks past fights. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `a` | Hay / straw stack | 4 | 8 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `d` | Dugout | 4 | 4 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `W` | Wreck / vehicle | 4 | 4 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `h` | House | 1 | 4 | Blocks sight and movement. One defender can stand inside. | Sim now |
| `O` | Rock | 3 | 3 | Rock-like cover. | Sim now |
| `z` | Camouflage net | 3 | 3 | Hides what is under it. No cover. | NEW Sim row |
| `M` | Spoil heap | 3 | 3 | Rock-like cover. | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `X` | Barbed wire | 2 | 2 | Slows infantry a lot. Player builds it. | Sim now |
| `s` | Shed | 2 | 2 | Small house: blocks sight and movement. | NEW Sim row |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |

**Empty ground:** 63 of 800 cells = 8% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 28% of the map. They are decoration and the open cells of the field between objects.

## Life

- Combine harvester works at the steppe edge and stops when the fight starts.
- Birds rise from the forest at the first shot.
- Smoke from the farm chimney.
- Wind moves the grass on the steppe.

## Winter and night

- **Winter:** Snow on the steppe: sight becomes shorter in snowfall. The forest stays the same.
- **Night:** The forest is black, the steppe is lit by moon: the enemy is visible on the right only.
