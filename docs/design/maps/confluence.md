# 3. Confluence

Category A · Difficulty: Hard · Fits: Campaign: late river map. Endless: 20 waves.

**Idea.** A tributary meets the main river in a Y. The attack splits into a west lane and an east lane. Each lane has its own ford. The defender cannot hold both fords equally.

## Defender functions

1. The hill (left, south bank) sees the west ford and half of the west lane.
2. The belt in the middle covers the point where the joined river is narrow. It sees both lanes.
3. The embankment and trench line is the last line for both lanes.
4. The grove (right) hides a mortar. It reaches the east ford.
5. The west ford (main river): infantry and vehicles.
6. The north ford (tributary): the crossing of the east lane. Vehicles cross here first.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. West lane: road to the west ford. Good for vehicles.
2. East lane: track to the tributary ford and then to the east ford. Long, but hidden by sunflowers.
3. Wade: infantry wades the joined river between the fords. Slow, deadly in a kill zone.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
,,S,=,,,,RR,,GGGGGGG
,,,S=,,,,RR,,GGGGGvG
GGGG=aa,,RR,,GGGGGGG
GGGG=as,,RR,,GGGGGGG
GGGG=,a,,FF,,,,:,,,,
GGGG=W,,,6F,,,,:,,v,
GGGG=,,,,FFS,uuuuuuu
GGGG=,,c,RR,,uuuuuuu
GGGG=,,,,RRS,uuuuuuu
,,,c=,,c,RR,,uuuuuvu
,,M,=S,,SRR,,uuuuuuu
,,,,=,,S,RR,Muuuuuuu
,S,,=,S,,RR,,,,:,,,,
RRRFFFRRRRR,,,,,:Mv,
RRRF5FRRRRR,,,S,:,M,
RRRFFFRRRRRR,,,,:,M,
,,,,=,,,,,RRR,,c:,,,
,,,,=uuuuu,RRR*,c:v,
HHHH=uuuuu,cRRR,,:,c
HH1H=uuuuu,,,RRRM:c,
HHHH=uuuuuM,,,FFF,c,
HHHH=uuuuu,,,2FFF,v*
HHHH=c,,,cS,,,FFFR,,
,,,,=c,c,,,,,,,:RRR,
....=cBBBBBB..a:.RRR
....=.BB3BBB..:...vR
....=*BBBBBB..:....R
....=.........:.4...
S...=.....S..GGGGGGG
....=..S.S.S.GGGGGvG
,,,,EEEEEEE,,GGGGGGG
,c,,=d,,,d,,,GGGGGGG
,,,,=,,,S,,,,GGGGGGG
,,,TTTTTTTTS:GGGGGvG
,,,,=,,,,,S,:GGGGGGG
,,,,=S,,,,,,:,,,,,,,
BBBB=,,,M,,,:,,GGGGG
BBBB=,,,,,,,:,,GGGGG
BBBB=,,,,,,:,,,GGGGG
,,aa=S,,,,,:,aaGGGGG
```

Legend: `,` Crop rows / stubble; `G` Grove / forest; `R` River; `u` Sunflower field; `.` Empty ground; `=` Road; `B` Forest belt; `F` Ford; `:` Field track; `S` Bush; `H` Hill; `c` Crater; `v` Power-line pole; `a` Hay / straw stack; `M` Spoil heap; `T` Trench; `E` Embankment; `*` Small decor (well, hive, scarecrow, bus stop); `d` Dugout; `s` Shed; `W` Wreck / vehicle; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 13 | 267 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `G` | Grove / forest | 4 | 122 | Soft sight block and cover inside. | Sim now |
| `R` | River | 4 | 66 | Infantry wades slowly. Vehicles cross only at fords or the bridge. | Sim now |
| `u` | Sunflower field | 2 | 66 | Hides who is inside, gives no cover. | Sim now |
| `=` | Road | 4 | 35 | Fast route for vehicles and infantry. | Sim now |
| `B` | Forest belt | 2 | 30 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `F` | Ford | 3 | 24 | The only place where vehicles cross. | Sim now |
| `:` | Field track | 4 | 22 | Field track. Light speed bonus. | Sim now |
| `S` | Bush | 15 | 21 | Hides, gives no cover. | Sim now |
| `H` | Hill | 1 | 20 | High ground: more range and sight, enemy direct fire less accurate. Cover behind it. | Sim now |
| `c` | Crater | 10 | 15 | Decoration. Marks past fights. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `a` | Hay / straw stack | 4 | 9 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `M` | Spoil heap | 7 | 9 | Rock-like cover. | Sim now |
| `T` | Trench | 1 | 8 | Defender cover. The player can build more. | Sim now |
| `E` | Embankment | 1 | 7 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `d` | Dugout | 2 | 2 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |
| `W` | Wreck / vehicle | 1 | 1 | Rock-like cover. Some wrecks burn (smoke). | Sim now |

**Empty ground:** 63 of 800 cells = 8% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 33% of the map. They are decoration and the open cells of the field between objects.

## Life

- Two rivers: the water of the tributary is a different colour (clear / brown).
- A herd of birds sits on the sandbar at the Y and flies up at the first shell.
- A hay stack burns at the north farm after a hit.
- Power-line poles lean along the track.

## Winter and night

- **Winter:** Ice: the tributary becomes a road. The Y no longer splits the attack.
- **Night:** Water reflects fire light: the fords are easy to see.
