# 5. Enemy heights

Category A · Difficulty: Hard · Fits: Campaign: mid-late. Endless: 10 waves.

**Idea.** The enemy owns the hill on its own bank and sees our line. The player must hide, not shoot first. Rework: camouflage nets, dead ground behind the embankment and a small counter-hill on our left flank.

## Defender functions

1. The small left hill on our bank: the only point that sees the back slope of the enemy hill.
2. Camouflage nets on the embankment hide the centre of our line from the enemy hill.
3. The embankment: cover and dead ground. Units behind it are not seen from the hill.
4. The right grove is dead ground for mortars. It is behind a shoulder of the embankment.
5. The ford (right): the only vehicle crossing. It is away from the hill, so the enemy sees less of it.
6. The enemy hill (not ours): drones that fly over it see our line. Keep air defence ready.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Road down the hill to the river: infantry and light vehicles. The hill sees it all.
2. Ford on the right: all heavy vehicles. Long walk along the river under our fire.
3. Belt on the right: hidden infantry route to the ford.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
uuuuu=,,,,,,,,,,c,,,
uuuuu=a,,,,S,,ccv,,,
uuuuu=,,,,,,,,,,,,,,
uuuuu=,,sa,S,,,,,,,,
uuuuu=,S,a,,,,GGGGGG
uuuuuS=,,,s,,,GGvGGG
uuuuu,=,,a,,,,GGGGGG
,aSS,,=,,,,S,,GG*GGG
,S,,,,=S,,,,,,GGGGGG
,,,S,,=SS,,,i,,,v,,a
,,HHHHHHHHHH,S,,,,,,
,,HHHHHHHHHH,,,,,,,,
,,HHHHHHHHHH,BWBBBBB
,cHHHHHHHHHH,BBBvBBB
,,HHHH6HHHHH,BBBBBBB
,,HHHHHHHHHHc,,,c,S,
,,HHHHHHHHHH,,,,,,,,
S,HHHHHHHHHH,,,,v,,c
,SHHHHHHHHHH,,,,,,,,
,,*,,,=S,,,,,S,,,,,S
,c,,,,=W,,,,,,,,,,,,
,,,,,,=,,,,,,,WSv,S,
RRRRRRRRRRRRRRRFFFRR
RRRRRRRRRRRRRRRF5FRR
RRRRRRRRRRRRRRRFFFRR
...O.M=....SM...v...
EEEEEEEEEEE..EEE3EEE
c.d...z2zz......d..c
HHHH..zzzz....GGGGGG
H1HH..=....a..GGvGGG
HHHH,,=,,M,a,,GG4GGG
HHHH,c=,,,,,,,GGGGGG
HHHH,,=,,,,,,,GGGGGG
G,,TTTTTTTTTTTGGvGGG
G,M,,,=,,,,,,,,,,,,,
G,S,,,=,,,,,,,,,*,,,
G,S,,,BBBBBBB,GGGGGG
,,fff,BBBBBBB,GGGGGG
S,fff,BBBBBBBcGGGGGG
,,,,,,=,,,,M,,GGGGGG
```

Legend: `,` Crop rows / stubble; `H` Hill; `G` Grove / forest; `R` River; `.` Empty ground; `B` Forest belt; `u` Sunflower field; `S` Bush; `=` Road; `E` Embankment; `c` Crater; `T` Trench; `v` Power-line pole; `F` Ford; `a` Hay / straw stack; `z` Camouflage net; `f` Fence; `M` Spoil heap; `*` Small decor (well, hive, scarecrow, bus stop); `W` Wreck / vehicle; `s` Shed; `d` Dugout; `i` Silo / tower / windmill; `O` Rock; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 8 | 288 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `H` | Hill | 2 | 110 | High ground: more range and sight, enemy direct fire less accurate. Cover behind it. | Sim now |
| `G` | Grove / forest | 4 | 90 | Soft sight block and cover inside. | Sim now |
| `R` | River | 2 | 51 | Infantry wades slowly. Vehicles cross only at fords or the bridge. | Sim now |
| `B` | Forest belt | 2 | 40 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `u` | Sunflower field | 1 | 35 | Hides who is inside, gives no cover. | Sim now |
| `S` | Bush | 19 | 25 | Hides, gives no cover. | Sim now |
| `=` | Road | 6 | 21 | Fast route for vehicles and infantry. | Sim now |
| `E` | Embankment | 2 | 18 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `c` | Crater | 10 | 12 | Decoration. Marks past fights. | Sim now |
| `T` | Trench | 1 | 11 | Defender cover. The player can build more. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `F` | Ford | 1 | 9 | The only place where vehicles cross. | Sim now |
| `a` | Hay / straw stack | 6 | 8 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `z` | Camouflage net | 1 | 8 | Hides what is under it. No cover. | NEW Sim row |
| `f` | Fence | 1 | 6 | Hides, gives no cover. | Sim now |
| `M` | Spoil heap | 5 | 5 | Rock-like cover. | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `W` | Wreck / vehicle | 3 | 3 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `s` | Shed | 2 | 2 | Small house: blocks sight and movement. | NEW Sim row |
| `d` | Dugout | 2 | 2 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |
| `O` | Rock | 1 | 1 | Rock-like cover. | Sim now |

**Empty ground:** 42 of 800 cells = 5% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 36% of the map. They are decoration and the open cells of the field between objects.

## Life

- Enemy spotter drone circles over the hill (animated).
- Smoke from a burned vehicle on the north slope.
- Birds nest on the hill and fly up when infantry climbs it.
- Shells hit the hill sometimes: dust clouds.

## Winter and night

- **Winter:** Snow: camouflage nets show white. The player must use the "winter net" version.
- **Night:** The enemy hill glows with its own fires. Our line is invisible to it without light.
