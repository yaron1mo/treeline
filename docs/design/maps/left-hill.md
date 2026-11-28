# 6. Left hill

Category B · Difficulty: Medium · Fits: Campaign: chapter 1-2. Endless: 7 waves.

**Idea.** A big hill covers the left flank. Whoever holds it sees half the map. The enemy wants it too.

## Defender functions

1. The hill top sees the road, the field and the right belt. Best place for a sniper and a spotter.
2. Dugouts on the hill slope protect from mortars.
3. The belt in the centre field hides a mobile squad.
4. Embankment: cover for the centre line against the road.
5. Trench behind the embankment: the last line.
6. The sunflower field (north-right) hides the enemy flank and cannot be seen from the hill.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Road: vehicles and main infantry. Under the hill guns the whole way.
2. Over the hill: infantry that wants the high ground. Slow, but a big prize.
3. Right flank through the sunflowers: hidden, ends at the right belt.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
,,,,,,,,,,,,=,S,,,,,
,,,,,,,,,,,,=,,,,,,,
GGGGGG,,,,,,=,,v,,,,
GGGGGG,,S,,u=uuuuuuu
GGGGGG,,,S,u=uuuuuuu
GGGGGG,,,,,u=uuuuuuu
GGGGGG,,,,,u=u6vuuuu
GGGGGG,,,,,u=uuuuuuu
GGGGGG,,,*,u=uuuuuuu
,,,,,,,,O,,u=uuuuuuu
HHHHHHHH,,,S=,,v,,,,
HHHHHHHH,,S,=,,,,S,,
HHH1HHHH,,BB=BBBBBBB
HHHHHHHH,,BB=BBBBBBB
HHHdHHHH,,BB=BBvBBBB
HHHHHHHHc*c,=,,,,,,,
HHHHHHHH,S,,=W,,,,S,
HHHHHHHH,,,,=,,,,,,c
HHHHdHHH,,,,=Scvc,,,
HHHHHHHH,,,,=,,,,,,,
HHHH2HHH,M,,=,,,GGGG
HHHHHHHH,cc,=,,,GGGG
HHdHHHHH,S,,=,,vGGGG
HHHHHHHH,,,,=,,,GGGG
HHHHHHHH....3.M.GGGG
HHHHHHHH....=...GGGG
HHHHHHHH..M.=..vGGGG
HHHHHHHH.c.c=.......
HHHHHHHHO...=.......
GGGGGGG.....=.......
GGGGGGG,,EEE4EEvEEEE
GGGaGGG,,,S,=,,i,,,,
GGGGGGG,,,,,=,,,,S,,
GGGGGGG,,,,,=,,,,,,,
GGGGG,TTTT5TTTTvTT,,
GGGGG,,,,,S,=,,,,,,,
GGGGG,BBBBBBB,,,,,,,
GGGGG,BBBBBBB,ffff,,
GGGGG,BBBBBBB,ffff,,
,,,,,,,,,,,,=S,c,,,,
```

Legend: `,` Crop rows / stubble; `H` Hill; `G` Grove / forest; `u` Sunflower field; `.` Empty ground; `B` Forest belt; `=` Road; `S` Bush; `T` Trench; `c` Crater; `E` Embankment; `v` Power-line pole; `f` Fence; `d` Dugout; `M` Spoil heap; `W` Wreck / vehicle; `*` Small decor (well, hive, scarecrow, bus stop); `O` Rock; `a` Hay / straw stack; `i` Silo / tower / windmill; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 9 | 261 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `H` | Hill | 1 | 149 | High ground: more range and sight, enemy direct fire less accurate. Cover behind it. | Sim now |
| `G` | Grove / forest | 3 | 129 | Soft sight block and cover inside. | Sim now |
| `u` | Sunflower field | 2 | 54 | Hides who is inside, gives no cover. | Sim now |
| `B` | Forest belt | 3 | 47 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `=` | Road | 4 | 35 | Fast route for vehicles and infantry. | Sim now |
| `S` | Bush | 12 | 14 | Hides, gives no cover. | Sim now |
| `T` | Trench | 2 | 11 | Defender cover. The player can build more. | Sim now |
| `c` | Crater | 9 | 10 | Decoration. Marks past fights. | Sim now |
| `E` | Embankment | 2 | 10 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `f` | Fence | 1 | 8 | Hides, gives no cover. | Sim now |
| `d` | Dugout | 3 | 3 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `M` | Spoil heap | 3 | 3 | Rock-like cover. | Sim now |
| `W` | Wreck / vehicle | 2 | 2 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 2 | 2 | Decoration. Landmark. | Decoration only |
| `O` | Rock | 2 | 2 | Rock-like cover. | Sim now |
| `a` | Hay / straw stack | 1 | 1 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |

**Empty ground:** 49 of 800 cells = 6% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 33% of the map. They are decoration and the open cells of the field between objects.

## Life

- Wind moves the grass on the slope. Sheep (decor) graze at the top and run away at shots.
- A hay-stack line burns after a hit.
- Birds fly up from the grove.
- A flag on a pole at the hill top (our colours, fictional).

## Winter and night

- **Winter:** Snow on the slope: infantry slides and is slower going up.
- **Night:** The hill top shows the enemy flare at the first contact.
