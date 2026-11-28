# 19. Wreck field

Category D · Difficulty: Medium · Fits: Campaign: chapter 4. Endless: 7 waves.

**Idea.** A destroyed column of vehicles lies in a diagonal line across the field. The wrecks are the only cover on the map, for both sides. The wreck line is also a wall: it splits the field in two lanes.

## Defender functions

1. Wreck line, middle: forward cover. A heavy machine gun can hold here, but the wrecks burn.
2. Wreck line, near our side: a safe cover for riflemen. Enemies see the line and shoot at it first.
3. Left dugout: protects from mortars.
4. Right dugout: same.
5. The gap in the wreck line at the road: vehicles must slow here. A kill zone.
6. The silo on the right: a landmark. A spotter on it sees the wreck line from the side.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Along the road: vehicles. They slow at the gap.
2. Behind the wrecks: infantry uses the wrecks as cover. Slow, protected.
3. Far right sunflower strip: slow flank. Goes round the line end.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
GGGGGGc,,=W,,,M,c,,,
GGGGGGW,c=S,S,M,,,,,
GGGGGG,c,=,,vuuuuuuu
GGGGGG,,,=,,uu6uuuuu
,,,,ccSWc=,,uuuuuuuu
W,,sac,c,=,,uuuuuuuu
,,,ac,,,,=,,vuuuuuuu
,,W,,,,,c=c,uuuuuuuu
,aW,S,,,,=,,,SSSSS,,
c,aWa,,SS*W,,caa,SS,
SMSMWc,,,=5cv,,aM,a,
SSSSWW,a,=cc,S,,SS,W
,,M,,OWXa=,c,,,S,,,,
,,,,,,WW,=ccS,,,,S,,
,,,a,,WWW=c,v,S,,,,,
Wcc,,SSWWWc,,M,,Mca,
,c,Mc,,,W=1,cS,,,aSa
aca,,,,,,W,W,a,M,,cS
a,cc,c,,,=WOv,,,c,,S
Sac,c,aaa=W,WWa,,,S,
,Sca,aMSS,=W,W,,c,,,
WSS,a,W,,S=SXXW,,,,,
,,,O,,,WW,=,vW,Wc,S,
MM,,,,,,S,=,,WW,,S,S
......S..M=S..W.S*.S
MM..M.....=.2WMWS...
.....MO...=.v.W.W.Sc
c.S.W.W.W.=.W...W.cM
...SW.ccW.=.cW.W.W.S
.cSS.W....=...S...W.
,cEE3EEEEEEEvEE4EE,S
,,,,,,,Sa,=,*,,,,,,c
Mc,cc,aSaa=,,,,,,aS,
,,,,,,,c,,=,,,GGGGGG
BBBBBTTTTTTTvTGGGGGG
BBBBB,SSSS=,,,GGGGGG
BBBBBSSS,,=,,SGGGGGG
BBBBBB,cffffS,BBBBBB
BBBBBBc,ffffvcBBBBBB
BBBBBBS,,,=S,,BBBBBB
```

Legend: `,` Crop rows / stubble; `S` Bush; `.` Empty ground; `W` Wreck / vehicle; `c` Crater; `B` Forest belt; `G` Grove / forest; `u` Sunflower field; `=` Road; `a` Hay / straw stack; `M` Spoil heap; `E` Embankment; `v` Power-line pole; `T` Trench; `f` Fence; `O` Rock; `*` Small decor (well, hive, scarecrow, bus stop); `X` Barbed wire; `i` Silo / tower / windmill; `s` Shed; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 21 | 262 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `S` | Bush | 33 | 72 | Hides, gives no cover. | Sim now |
| `W` | Wreck / vehicle | 14 | 59 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `c` | Crater | 30 | 56 | Decoration. Marks past fights. | Sim now |
| `B` | Forest belt | 2 | 51 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `G` | Grove / forest | 2 | 48 | Soft sight block and cover inside. | Sim now |
| `u` | Sunflower field | 1 | 45 | Hides who is inside, gives no cover. | Sim now |
| `=` | Road | 7 | 33 | Fast route for vehicles and infantry. | Sim now |
| `a` | Hay / straw stack | 16 | 32 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `M` | Spoil heap | 17 | 21 | Rock-like cover. | Sim now |
| `E` | Embankment | 2 | 15 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `v` | Power-line pole | 10 | 10 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `T` | Trench | 2 | 8 | Defender cover. The player can build more. | Sim now |
| `f` | Fence | 1 | 8 | Hides, gives no cover. | Sim now |
| `O` | Rock | 4 | 4 | Rock-like cover. | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `X` | Barbed wire | 2 | 3 | Slows infantry a lot. Player builds it. | Sim now |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |

**Empty ground:** 68 of 800 cells = 8% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 33% of the map. They are decoration and the open cells of the field between objects.

## Life

- Some wrecks burn and smoke (animated). Fire sparks fly.
- Crows sit on the wrecks and rise at the first shot.
- A dog (decor) runs along the column.
- Shell craters have water in them (decor).

## Winter and night

- **Winter:** Snow on the wrecks. Cold fires are brighter.
- **Night:** Burning wrecks light the field like street lamps. A good night map.
