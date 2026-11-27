# 2. River bend

Category A · Difficulty: Medium · Fits: Campaign: mid. Endless: 7 waves.

**Idea.** The river bends deep into the field. The attack runs along the west bank. The defender holds the pocket inside the bend and the ford at its tip.

## Defender functions

1. The ford at the tip of the bend: vehicles that cut the corner cross here, under fire from three sides.
2. The pocket inside the bend (east): a safe place for mortars and drone launch. The river guards it.
3. The hill near our line sees up the whole west bank.
4. The embankment and dugouts hold the line if the ford falls.
5. The belt on the enemy side hides infantry. A drone sees it from above.
6. The pipe-less bank (north) is a long open run: a machine gunner sees the whole approach.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Road along the west bank: all vehicles. Fast, but under fire for 25 cells.
2. Across the ford at the bend: shortcut for those who learn where the defence is weak.
3. Wading the north river arm: slow infantry that comes in from the east, behind the pocket.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
,,,S,=,a,,Suuuuu,RRR
,SvS,=,,,,,uuuuu,RRR
,,,S,=,,,cSuuuuu,RRR
,,,S,=,S,c,uuuus,RRR
,,,,S=S,,a,uuuuuSRRR
,av,,=,,,,,uuuuu,RRR
,BBBB=,,SSc,,,,,,RRR
SBBBB,=,SS,S,,,,cRRR
,BBBB5W,,,,,*,,ccRRR
,BvBB,=aS,S,Sa,M,RRR
SBBBB,=,,,s,,,M,,RRR
,,c,a,=,M,a,,,S,RRR,
,,c,Ma=,,a,SSSM,RRGG
,,v,a,=W,MS,,,,RRRGG
S,,,MM=,,,,,,S,RRRGG
,,,,aa=,,,SS,cRRRcGG
,,,,,,=,,,,,SRRR,,GG
,,v,,,=,SS,,RfR,,,cS
M,,,c,=M,,,,6fR,,,,c
,,,,,MM,,,,RRf,,,,,c
M,S,,=,,,aRRR,S,,S,,
SSv,,=,SSFFFF,,,,,,,
SSM,S=,,,FFFFGGGGGG,
,,,,,=,,,FFFFGGGGGG,
.....WW..F1FFGGGGGG.
..v..=....RRRGG2GGG.
S....=..S.RRRGGGGGG.
S..Mc=...S.RRGGGGGGu
.....=.S...RRGGGGGGu
..vEEEEEES..RRR....u
GGGGGd3d,,,,RRRS,S,u
GGGGG=,,MS,,SRRRc,,u
GGGGG,=S,,HHHHRRR,,,
GGvTTTTTTSHHHHRRR,*,
GGGGG,=,,SH4HH,RRR,,
GGGGG,=,,,HHHH,*RR,,
,,a,a,=,,,c,,,BBBBB,
,,vS,,=,cc,M,,BBBBB,
GGGGGG=SSaa,c,BBBBBR
GGGGGG=,S,,ac,,,,RRR
```

Legend: `,` Crop rows / stubble; `R` River; `G` Grove / forest; `S` Bush; `.` Empty ground; `=` Road; `u` Sunflower field; `B` Forest belt; `c` Crater; `a` Hay / straw stack; `M` Spoil heap; `F` Ford; `H` Hill; `v` Power-line pole; `E` Embankment; `T` Trench; `W` Wreck / vehicle; `*` Small decor (well, hive, scarecrow, bus stop); `f` Fence; `s` Shed; `d` Dugout; `i` Silo / tower / windmill; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 14 | 281 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `R` | River | 3 | 93 | Infantry wades slowly. Vehicles cross only at fords or the bridge. | Sim now |
| `G` | Grove / forest | 4 | 90 | Soft sight block and cover inside. | Sim now |
| `S` | Bush | 36 | 61 | Hides, gives no cover. | Sim now |
| `=` | Road | 6 | 34 | Fast route for vehicles and infantry. | Sim now |
| `u` | Sunflower field | 2 | 34 | Hides who is inside, gives no cover. | Sim now |
| `B` | Forest belt | 2 | 34 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `c` | Crater | 12 | 21 | Decoration. Marks past fights. | Sim now |
| `a` | Hay / straw stack | 12 | 18 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `M` | Spoil heap | 13 | 17 | Rock-like cover. | Sim now |
| `F` | Ford | 1 | 16 | The only place where vehicles cross. | Sim now |
| `H` | Hill | 1 | 16 | High ground: more range and sight, enemy direct fire less accurate. Cover behind it. | Sim now |
| `v` | Power-line pole | 10 | 10 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `E` | Embankment | 1 | 6 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `T` | Trench | 1 | 6 | Defender cover. The player can build more. | Sim now |
| `W` | Wreck / vehicle | 3 | 4 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `f` | Fence | 1 | 3 | Hides, gives no cover. | Sim now |
| `s` | Shed | 2 | 2 | Small house: blocks sight and movement. | NEW Sim row |
| `d` | Dugout | 2 | 2 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |

**Empty ground:** 48 of 800 cells = 6% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 35% of the map. They are decoration and the open cells of the field between objects.

## Life

- Water flows around the bend; fish jump near the ford.
- A tractor drives on the road at the start of each wave and runs away when the fight begins.
- Smoke from a wrecked car near the ford.
- Windmill blades turn in the pocket.

## Winter and night

- **Winter:** The river freezes: the pocket loses its guard. Defender must then hold the line, not the bend.
- **Night:** The pocket is dark. A small campfire shows where the enemy rests.
