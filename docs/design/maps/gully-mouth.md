# 10. Gully mouth

Category B · Difficulty: Medium · Fits: Campaign: chapter 2. Endless: 7 waves.

**Idea.** A gully runs along the line of advance and ends in front of our hill. The gully is a clear, drawn feature: two cells wide, with spoil heaps along both banks. It is a hidden route for infantry. Vehicles go around on the road.

## Defender functions

1. The hill at the gully mouth sees down the whole gully. Mortars on the hill hit the gully floor.
2. Dugout on the hill (right side): protects the hill flank from mortars.
3. The road: vehicles drive past the gully, far to the right. Fire from the trench reaches the road.
4. The mouth apron: infantry leaves the gully here. It is a kill zone. Wire here stops them.
5. The sunflower strip on the left hides our scouts that watch the gully entrance.
6. The gully entrance far north: a drone sees them enter. Time to prepare the mouth.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Gully: infantry only. Hidden from all but the hill top. Ends in the kill zone.
2. Road: vehicles. Fast, in view from the trench line.
3. Left sunflowers: slow infantry flank. Joins the gully.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
,,,,,OO,KKc,,,,=,,,,
,,O,O,,,KK,,,,,=,O,,
,c,,,,M,KK,M,,O=,v,S
uuuu,,M,KK,M,OO=,S,,
uuuu,,M,KKcM,S*=,,O,
uuuu,SM,KK,M,,,=,,aa
uuuu,,M,KK,M,SG=GvGG
uuuuaSM,KK,MSSG=GGGG
uuuu,,M,K6,MS,G=WGGG
uuuu,,M,KK,MaSG=GGGG
u5suS,M,KK,M,cG=GvGG
,,aa,,MSKK,M,SG=GGGG
,,,,,,M,KK,MM,G=GGGG
,,,,,SM,KK,,M,S=,,,,
c,,a,,M,KO,,M,,=,vSS
,,,a,,MSKK,MM,,=,,,,
BBBBB,M,KK,,M,,=,,,,
BBBBB,MaKKS,M,S=Sac,
BBBBBO,M,KK,M,,=Wv,a
,,c,,,,M,KK,M,,=,S,c
S,,S,O,M,KO,M,,=,,,,
aaSa,S,MaKKaM,M=c,S,
S,,,S,,M,KK,M,,=,v,,
,SSS,,,MMKK,,,,=S,S,
...S.SS..KK...S3....
c....M.M.KK....W.c..
GGGG...M.KK....=.v..
GGGG.M.MKKKKS..=S.M.
GGGGHHHHH4HHHH.=..a.
GGGGHHHHHHHHHH.=..a.
,,,,HHdHHHHdHH,=,v,,
S,,,HHHH1HHH2H,=,SS,
,,,,HHHHHHHHHH,=,,S,
,,,,HHHHHHHHHH,=,,,,
,,,,HHHHHHHHHH,*,v,,
,,,SS,S,,,,,,,,=,,S,
,,,TTTTTTTTTTTTTTS,,
BBBBB,,,ffff,,S=GGGG
BBBBB,SSffff,,,=GGGG
BBBBBO,,,,,,,O,=GGGG
```

Legend: `,` Crop rows / stubble; `H` Hill; `G` Grove / forest; `K` Gully (balka); `M` Spoil heap; `S` Bush; `.` Empty ground; `=` Road; `u` Sunflower field; `B` Forest belt; `a` Hay / straw stack; `O` Rock; `T` Trench; `c` Crater; `v` Power-line pole; `f` Fence; `W` Wreck / vehicle; `*` Small decor (well, hive, scarecrow, bus stop); `d` Dugout; `s` Shed; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 17 | 279 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `H` | Hill | 1 | 68 | High ground: more range and sight, enemy direct fire less accurate. Cover behind it. | Sim now |
| `G` | Grove / forest | 4 | 60 | Soft sight block and cover inside. | Sim now |
| `K` | Gully (balka) | 1 | 56 | Fast, hidden path for infantry. Vehicles go around. | Sim now |
| `M` | Spoil heap | 7 | 53 | Rock-like cover. | Sim now |
| `S` | Bush | 34 | 52 | Hides, gives no cover. | Sim now |
| `=` | Road | 4 | 37 | Fast route for vehicles and infantry. | Sim now |
| `u` | Sunflower field | 1 | 31 | Hides who is inside, gives no cover. | Sim now |
| `B` | Forest belt | 2 | 30 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `a` | Hay / straw stack | 13 | 18 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `O` | Rock | 11 | 15 | Rock-like cover. | Sim now |
| `T` | Trench | 1 | 14 | Defender cover. The player can build more. | Sim now |
| `c` | Crater | 11 | 11 | Decoration. Marks past fights. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `f` | Fence | 1 | 8 | Hides, gives no cover. | Sim now |
| `W` | Wreck / vehicle | 3 | 3 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 2 | 2 | Decoration. Landmark. | Decoration only |
| `d` | Dugout | 2 | 2 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |

**Empty ground:** 51 of 800 cells = 6% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 35% of the map. They are decoration and the open cells of the field between objects.

## Life

- Mist or smoke lies in the gully at start (animated).
- Small birds fly up from the gully when infantry enters.
- A rusted pipe drips at the gully bank.
- Sheep trails along the gully edge.

## Winter and night

- **Winter:** Snow in the gully: infantry is slower, easy to see from the hill.
- **Night:** The gully is black. Only the apron is lit by our flares.
