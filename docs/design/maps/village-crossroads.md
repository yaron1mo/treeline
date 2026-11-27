# 22. Crossroads

Category V (village) · Difficulty: Hard · Fits: Campaign: chapter 5 (second village). Endless: 20 waves. Exists as a Sim field (Claude, 2026-11-27).

**Idea.** Two village streets cross in the middle. Whoever holds the houses at the crossroads controls both routes. The attack can come down the north street, then split: west street, east street or straight on.

## Defender functions

1. The ruined corner at the crossroads: cover on all sides. A trap for the first unit to take it.
2. The north-west house (row 17): sees the north street and the west street.
3. The north-east house: crossfire with the north-west house.
4. The south houses: the second ring. They see the crossroads from behind.
5. The line trench: the last line.
6. The pond south-west: blocks vehicles and protects the flank.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. North street: vehicles and infantry. The main route.
2. West street: infantry and light vehicles. Branches at the crossroads.
3. East street: same on the other side.
4. Across gardens: slow infantry between houses.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
c,,,:,M,,W=Mc,,,:,,,
c,,,:M,M,,=,c,,,:,,,
,,,,:M,S,,=MM,,,:GGG
,M,,:,cc,a=,,,,,:GGG
GGGG:,,,c,=,,c,,:GGG
GGGa:acS,c=vcc,,:,,,
GGGa:,,,,,=,,,,:,S,,
,S,S:GGfh,=hfGG:SW,,
,M,,:GGf,,=,fGG:MMM,
,Mcc:GGfh,=hfGG:,,S,
Maa,:GGfh,=hfGG:SMc,
,,W,:GGf,,=,fGG:Wa,c
,aS,:GGfv,=,fGG:Waac
,,S:,GGfh,=hfGG,:,S,
W,W:,GGfr,=hfGGs:,,,
,,,:MGGf,W=,fGG,:M,,
,,,:,,Mf2a=3fGG,,:W,
,,:,,,,f,,=*fGG,W:,W
,,:,,,,S,,=,,,,,W,:,
,,:,,S,,,,=1i=====:=
===========,,M,,,cS,
S,,Ms,cfh,=hfGG,cMS,
,a,,cGGf,,=,fGG,,SSS
,,,a,GGfh*=4fGG,,,,,
..a..GGfh.=.fGG..S.c
..6P.GGf..=hfGG....c
.MPPMGGfWS=.fW......
W..c.GGfh.=hf.....S.
...MMGGfhW=.fGG.....
.c.SMSSfc.=rfGG.....
,,c,,GGfh,=vfSS,,,a,
,a,cSGGf,,=,f,c,,S,a
,c,c,cc,,,=,M,M,c,W,
S,,cSSSSS,=,M,,,cMW,
S,S,,,S,z,=,zS,,,a,,
,S,,dc,,v,=,,,Sd,a,,
,,,,TTTTTT5TTTTTT,a,
BBBBBM,,ffff,c,BBBBB
BBBBBM,,ffff,,,BBBBB
BBBBB,,,S,=,,,,BBBBB
```

Legend: `,` Crop rows / stubble; `G` Grove / forest; `=` Road; `.` Empty ground; `f` Fence; `S` Bush; `:` Field track; `c` Crater; `M` Spoil heap; `B` Forest belt; `h` House; `a` Hay / straw stack; `W` Wreck / vehicle; `T` Trench; `v` Power-line pole; `P` Pond; `r` Ruined house; `s` Shed; `*` Small decor (well, hive, scarecrow, bus stop); `z` Camouflage net; `d` Dugout; `i` Silo / tower / windmill; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 30 | 277 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `G` | Grove / forest | 8 | 91 | Soft sight block and cover inside. | Sim now |
| `=` | Road | 4 | 53 | Fast route for vehicles and infantry. | Sim now |
| `f` | Fence | 5 | 52 | Hides, gives no cover. | Sim now |
| `S` | Bush | 25 | 42 | Hides, gives no cover. | Sim now |
| `:` | Field track | 2 | 40 | Field track. Light speed bonus. | Sim now |
| `c` | Crater | 20 | 38 | Decoration. Marks past fights. | Sim now |
| `M` | Spoil heap | 20 | 32 | Rock-like cover. | Sim now |
| `B` | Forest belt | 2 | 30 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `h` | House | 16 | 21 | Blocks sight and movement. One defender can stand inside. | Sim now |
| `a` | Hay / straw stack | 12 | 20 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `W` | Wreck / vehicle | 14 | 18 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `T` | Trench | 1 | 13 | Defender cover. The player can build more. | Sim now |
| `v` | Power-line pole | 4 | 4 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `P` | Pond | 1 | 4 | Blocks all. Forces routes around it. | Sim now |
| `r` | Ruined house | 3 | 3 | Cover. Infantry climb through slowly. | Sim now |
| `s` | Shed | 2 | 2 | Small house: blocks sight and movement. | NEW Sim row |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 2 | 2 | Decoration. Landmark. | Decoration only |
| `z` | Camouflage net | 2 | 2 | Hides what is under it. No cover. | NEW Sim row |
| `d` | Dugout | 2 | 2 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |

**Empty ground:** 53 of 800 cells = 7% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 35% of the map. They are decoration and the open cells of the field between objects.

## Life

- A well and a bus stop at the crossroads (decor). A truck burns on the west street.
- A dog and a cat run at the first shot.
- Lamps and window light at night.
- Smoke from a stove chimney. Pond ducks fly up when a shell lands.

## Winter and night

- **Winter:** Snow on streets: vehicles slide at the crossroads. Footprints in the gardens.
- **Night:** A burning building at the crossroads lights the centre. The edges are dark.
