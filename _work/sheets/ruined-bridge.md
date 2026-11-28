# 1. Ruined bridge

Category A · Difficulty: Medium · Fits: Campaign: first river map, teaches choke points. Endless: 10 waves.

**Idea.** A river cuts the field in two. The bridge is broken, so vehicles must queue at one ford far on the side. Infantry crosses a pipe or wades.

## Defender functions

1. The pipe (left) is the infantry shortcut. A trench behind it makes a small kill zone for pipe crossers.
2. The broken bridge is a choke point: vehicles stop at its rubble and turn to the far ford.
3. The embankment line on our bank gives cover to the main squad and covers the road end.
4. Dugouts behind the embankment protect from mortars and drones.
5. The grove on the right hides the mortar position. It sees the ford over the river.
6. The ford (right): every vehicle comes here. Hedgehogs and wire at the ford exit give a kill zone.
7. The shed on the enemy bank is a landmark and a spotter perch for drones.

The numbers 1–7 on the layout show where each function is.

## Attacker routes

1. Road to bridge: fast infantry, then the rubble slows them. Bad for vehicles.
2. Field track to the far ford: all vehicles. Long, so they are in view of the whole line.
3. Pipe: small infantry groups, hidden by the left belt. Cheap flank.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
GGGGG,,,,=,,,,,GGGGG
GGGGG,,,,=,,,,,GGGGG
GGGGG,,,,=,,v,,GGGGG
GGGGGS,,,=,S,,,:S,,,
,SaSSc,,,=S,SM,:uuuu
cc,,,c,,,=,SMS,*uuuu
c,accW,Sa=,,v,,:uuuu
,c,SSW,,,=,,,,,:uuuu
,,,,,,,a,=,a7s,:uuuu
S,,,S,,,,=,,aaa:uuuu
S,,c,,,SM=,,va,:,S,,
,,caa,,S,=,,c,,:S,S,
,,,,,,,SW=,,c,,:M*S,
,,,,,S,,,=,,ccM:SaS,
,S,a,,,c,=W,vc,:,,,,
S,a,,,,M,=,,,Sc:,,,,
RRpRRRRRRQOR2RRF6FRR
RRpRRRRRRQRRRRRFFFRR
RRpRRRRRRQORvRRFFFRR
,,1S,Sa,,=,,,,,,:,,,
BBBB,,,SS=,S,,a,:,,,
BBBBS,,,,=WSca,:SaS,
,,S,S,,,W=,,va,:,,S,
,,SEEEEEE=EEEEEE,,c,
uuu..4...=...d:.SMc.
uuu.MMS..=3McS:.....
uuu....c.=..v:GG5GG.
uuuM.McSS=.M.:GGGGGa
uuu......=M..:GGGGGa
uuuS.c...=..:.GGGGG.
,,,,c,,*,=,,v,GGGGG,
,c,a,,,,,=,,,,GGGGG,
,,SacTTTTTTTTTM,,S,,
,,MacS,,,=,cSM,BBBBB
a,Ma,,,,,=,,vMMBBBBB
GGGGGS,,S=a,,,BBBBBS
GGGGG,,SS=,ff,BBBBB,
GGGGG,a,,=,ff,BBBBB,
GGGGG,,aa=,ff,BBBBB,
,,,,,,,,,=,,,,cc,S,,
```

Legend: `,` Crop rows / stubble; `G` Grove / forest; `S` Bush; `.` Empty ground; `u` Sunflower field; `R` River; `B` Forest belt; `=` Road; `c` Crater; `a` Hay / straw stack; `:` Field track; `M` Spoil heap; `E` Embankment; `v` Power-line pole; `F` Ford; `T` Trench; `W` Wreck / vehicle; `f` Fence; `*` Small decor (well, hive, scarecrow, bus stop); `p` Pipe; `Q` Ruined bridge; `O` Rock; `d` Dugout; `s` Shed; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 24 | 285 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `G` | Grove / forest | 4 | 85 | Soft sight block and cover inside. | Sim now |
| `S` | Bush | 35 | 58 | Hides, gives no cover. | Sim now |
| `u` | Sunflower field | 2 | 42 | Hides who is inside, gives no cover. | Sim now |
| `R` | River | 4 | 42 | Infantry wades slowly. Vehicles cross only at fords or the bridge. | Sim now |
| `B` | Forest belt | 2 | 38 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `=` | Road | 3 | 36 | Fast route for vehicles and infantry. | Sim now |
| `c` | Crater | 14 | 31 | Decoration. Marks past fights. | Sim now |
| `a` | Hay / straw stack | 16 | 30 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `:` | Field track | 4 | 22 | Field track. Light speed bonus. | Sim now |
| `M` | Spoil heap | 13 | 21 | Rock-like cover. | Sim now |
| `E` | Embankment | 2 | 12 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `F` | Ford | 1 | 9 | The only place where vehicles cross. | Sim now |
| `T` | Trench | 1 | 9 | Defender cover. The player can build more. | Sim now |
| `W` | Wreck / vehicle | 5 | 6 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `f` | Fence | 1 | 6 | Hides, gives no cover. | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `p` | Pipe | 1 | 3 | Infantry crossing. Vehicles cannot use it. | Sim now |
| `Q` | Ruined bridge | 1 | 3 | Broken bridge. Infantry cross slowly, vehicles cannot. | Sim now |
| `O` | Rock | 2 | 2 | Rock-like cover. | Sim now |
| `d` | Dugout | 2 | 2 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |

**Empty ground:** 45 of 800 cells = 6% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 36% of the map. They are decoration and the open cells of the field between objects.

## Life

- River flows (animated water). Ducks fly up from the reeds when a unit wades.
- A burned combine smokes at the north bank.
- Power-line poles along the road; a wire hangs into the river near the bridge.
- Birds fly up from the sunflowers when a shell lands.

## Winter and night

- **Winter:** Ice on the river: infantry cross anywhere slowly; the ford is no longer special.
- **Night:** Only the burning combine and tracer fire give light; the bridge rubble is a dark shape.
