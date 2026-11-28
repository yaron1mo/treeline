# 4. Overlook

Category A · Difficulty: Easy · Fits: Campaign: chapter 1, teaches high ground. Endless: 7 waves.

**Idea.** One ford, and our hill stands right above it. The first map where the height decides the fight.

## Defender functions

1. The hill top sees the whole ford and the road behind the river. Units on the hill shoot further.
2. The ford: the single crossing. Wire and hedgehogs here slow vehicles under fire.
3. Left belt: hides riflemen who flank the ford.
4. Right belt: same, and it shelters mortars from the road side.
5. Trench behind the hill: the second line if the top falls.
6. The wreck group north of the ford: enemy cover and a landmark for spotting.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Road to the ford: all vehicles and the main infantry.
2. Wade on the left: infantry, slow, but hidden by the sunflower field.
3. Wade on the right: same on the other side.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
GGGGGG,,,,=,,,GGGGGG
GGGGGG,,,,=,,vGGGGGG
GGGGGG,,,,=c,,GGGGGG
GGGGGG,,,,=,,,GGGGGG
GGGGGG,,,,=,,S,,,,,,
,,,,,,,,,,=c,v,uuuuu
,,,s,,,,,,=,,,,uuuuu
,,,aa,,,c,=,,,,uuuuu
uuuuu,,S,,W,,,,uuuuu
uuuuu,,,,,=,,v,uuuuu
uuuuuc,,,,=,,,,uuuuu
uuuuu,*,,,=c,,,,,,,,
uuuuu,,,,,=,W,,,S,,,
,,,,,,,,,W6,,v,,,,,,
,,,,,,,,,,=,,aa,,,,,
,,,,,,,,,,=,,,,a,,,,
RRRRRRRRRFFFRRRRRRRR
RRRRRRRRRF2FRvRRRRRR
RRRRRRRRRFFFRRRRRRRR
,,,,,,S,,,=,,,,,,,,,
BBBB,EEEE,=,EEEEBBBB
BB3B,,,,,M=M,v,SB4BB
,,,,,HdHHHHHHdH,,,,,
,,,,,HHHHHHHHHH,,,,,
GGGG.HHHH1HHHHH.GGGG
GGGG.HHHOHHHHvH.GGGG
GGGG.HHHHHHOHHH.GGGG
GGGG.HHHHHHHHHH.GGGG
GGGG.HHHHHHHHHH.GGGG
GGGG.HHHHHHHHvH.GGGG
GGGG,,,S,,=,c,,,GGGG
,,,,TTTTTT5TTTTT,,S,
,,,,,,c,,,=,,,,,,,,,
,,,,,,,,*,=,,v,S,,,,
BBBBBS,,,a=,,S,BBBBB
BBBBBSS,,,=,a,SBBBBB
BBBBB,,,ffff,,,BBBBB
BBBBB,h,ffff,h,BBBBB
BBBBB,,affff,,,BBBBB
,,,,,,S,,,=,,,,,,,,,
```

Legend: `,` Crop rows / stubble; `G` Grove / forest; `H` Hill; `B` Forest belt; `u` Sunflower field; `R` River; `=` Road; `S` Bush; `.` Empty ground; `T` Trench; `f` Fence; `v` Power-line pole; `F` Ford; `a` Hay / straw stack; `E` Embankment; `c` Crater; `W` Wreck / vehicle; `*` Small decor (well, hive, scarecrow, bus stop); `M` Spoil heap; `d` Dugout; `O` Rock; `h` House; `s` Shed; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 9 | 316 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `G` | Grove / forest | 4 | 110 | Soft sight block and cover inside. | Sim now |
| `H` | Hill | 1 | 74 | High ground: more range and sight, enemy direct fire less accurate. Cover behind it. | Sim now |
| `B` | Forest belt | 4 | 66 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `u` | Sunflower field | 2 | 55 | Hides who is inside, gives no cover. | Sim now |
| `R` | River | 2 | 50 | Infantry wades slowly. Vehicles cross only at fords or the bridge. | Sim now |
| `=` | Road | 6 | 24 | Fast route for vehicles and infantry. | Sim now |
| `S` | Bush | 11 | 14 | Hides, gives no cover. | Sim now |
| `T` | Trench | 1 | 12 | Defender cover. The player can build more. | Sim now |
| `f` | Fence | 1 | 12 | Hides, gives no cover. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `F` | Ford | 1 | 9 | The only place where vehicles cross. | Sim now |
| `a` | Hay / straw stack | 5 | 8 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `E` | Embankment | 2 | 8 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `c` | Crater | 7 | 7 | Decoration. Marks past fights. | Sim now |
| `W` | Wreck / vehicle | 3 | 3 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 2 | 2 | Decoration. Landmark. | Decoration only |
| `M` | Spoil heap | 2 | 2 | Rock-like cover. | Sim now |
| `d` | Dugout | 2 | 2 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `O` | Rock | 2 | 2 | Rock-like cover. | Sim now |
| `h` | House | 2 | 2 | Blocks sight and movement. One defender can stand inside. | Sim now |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |

**Empty ground:** 12 of 800 cells = 2% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 40% of the map. They are decoration and the open cells of the field between objects.

## Life

- Water flows, small waves at the ford.
- A shepherd-less herd of goats runs away at the start of a wave (decoration, not a target).
- Smoke from a wrecked car north of the river.
- Poles with a fallen wire along the road.

## Winter and night

- **Winter:** Snow on the hill: units on it are visible to enemy drones (white against grey).
- **Night:** The hill is a black shape; only the ford glows with tracers.
