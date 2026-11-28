# 8. Valley

Category B · Difficulty: Medium · Fits: Campaign: chapter 2-3. Endless: 10 waves.

**Idea.** Two hills with a valley between them. The attack goes through the valley. Both hills see the valley floor: a crossfire.

## Defender functions

1. Left hill (north): first shot at the valley mouth.
2. Right hill (north): crossfire with the left hill.
3. Left hill (south): last cover over the valley exit.
4. Right hill (south): same on the right.
5. The embankment at the valley exit: units here see enemies when they leave the valley.
6. The central belt breaks the valley in two. Vehicles use the road, infantry can hide in the belt.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Valley road: all vehicles. Narrow, so they queue.
2. Valley floor left: infantry along the foot of the left hill.
3. Over a hill: slow, very exposed, but out of the crossfire for a moment.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
GGGGGGG,c,=,,GGGGGGG
GGGGGGG,Sc=,,GGGGGGG
GGGGGGG,s,av,GGGGGGG
GGGGGGG,a,=a,GGGGGGG
GGGGGGG,,,=,,GGGGGGG
GGGGGGG,,W=,,GGGGGGG
GGGGGGG,,,=v,GGGGGGG
,,,S,,,,,,=,,S,,,,,,
HHHHHH,,,,=,,SHHHHHH
HHHHHH,,,,=,,,HHHHHH
HHHHHH,,O,=v,,HHHHHH
HHHHHH,*,,=,,,HHHHHH
HHH1HH,BBB6BB,HH2HHH
HHHHHH,BBBBBB,HHHHHH
HHdHHH,,,S=vc,HHHdHH
HHHHHH,c,,W,,,HHHHHH
HHHHHH,,,,=,,,HHHHHH
HHHHHH,,,,=,,,HHHHHH
HHHHHH,,M,=v,,HHHHHH
HHHHHH,,,,=,,,HHHHHH
HHHHHH,,,=,,O,HHHHHH
HHHHHHc,,=,,,,HHHHHH
HHHHHH,,W=,v,,HHHHHH
HHHHHH,,,=,,,,HHHHHH
HHH3HH..S=.M..HH4HHH
HHHHHH...=.c..HHHHHH
HHHdHH...=.v.SHHdHHH
HHHHHH...=..S.HHHHHH
HHHHHH.M.=.S..HHHHHH
HHHHHH...=...SHHHHHH
HHHHHH,,,=,v,,HHHHHH
,,,,,EEEE5EEEEES,,,,
uuuuu,,,,=,,,,,uuuuu
uuuuu,,,,*,,,,,uuuuu
uuuuu,TTTTTvTT,uuuuu
uuuuu,,,,=,,,,,uuuuu
BBBBBB,,S=,,c,BBBBBB
BBBBBB,,ffff,,BBBBBB
BBBBBBc,ffff,,BBBBBB
,,,,c,,,,=,,,,,,,S,,
```

Legend: `H` Hill; `,` Crop rows / stubble; `G` Grove / forest; `B` Forest belt; `u` Sunflower field; `.` Empty ground; `=` Road; `S` Bush; `E` Embankment; `c` Crater; `v` Power-line pole; `f` Fence; `T` Trench; `d` Dugout; `a` Hay / straw stack; `W` Wreck / vehicle; `M` Spoil heap; `O` Rock; `*` Small decor (well, hive, scarecrow, bus stop); `s` Shed; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `H` | Hill | 2 | 272 | High ground: more range and sight, enemy direct fire less accurate. Cover behind it. | Sim now |
| `,` | Crop rows / stubble | 12 | 204 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `G` | Grove / forest | 2 | 98 | Soft sight block and cover inside. | Sim now |
| `B` | Forest belt | 3 | 48 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `u` | Sunflower field | 2 | 40 | Hides who is inside, gives no cover. | Sim now |
| `=` | Road | 7 | 31 | Fast route for vehicles and infantry. | Sim now |
| `S` | Bush | 10 | 13 | Hides, gives no cover. | Sim now |
| `E` | Embankment | 1 | 10 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `c` | Crater | 8 | 9 | Decoration. Marks past fights. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `f` | Fence | 1 | 8 | Hides, gives no cover. | Sim now |
| `T` | Trench | 2 | 7 | Defender cover. The player can build more. | Sim now |
| `d` | Dugout | 4 | 4 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `a` | Hay / straw stack | 2 | 3 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `W` | Wreck / vehicle | 3 | 3 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `M` | Spoil heap | 3 | 3 | Rock-like cover. | Sim now |
| `O` | Rock | 2 | 2 | Rock-like cover. | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 2 | 2 | Decoration. Landmark. | Decoration only |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |

**Empty ground:** 33 of 800 cells = 4% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 26% of the map. They are decoration and the open cells of the field between objects.

## Life

- Mist rises in the valley in the morning (animated).
- Sheep flee from the road at the first shot.
- A burned truck smokes in the middle of the road.
- Poles along the road.

## Winter and night

- **Winter:** Snow fills the valley: speed is lower. Hills stay clear.
- **Night:** Fire and flares light the valley like a stage.
