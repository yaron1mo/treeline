# 16. Open steppe

Category D · Difficulty: Hard · Fits: Campaign: chapter 4. Endless: 20 waves.

**Idea.** Almost no trees. Long sight lines. The only cover is what the war left: heaps, wrecks, rocks, craters, hay stacks. The kurgan (a burial mound) is the only hill. The map is open, but the defender still has tools: the kurgan, the windmill and the heap lines.

## Defender functions

1. The kurgan: the only high ground. A sniper or a spotter here sees the whole steppe.
2. The windmill: a landmark and a spotter perch. It blocks a thin line of sight.
3. The embankment and trench: the line. Wire in front makes a kill zone.
4. Heaps at mid-field: forward cover for a machine gun. A risk, but it forces the enemy to slow down.
5. The road: vehicles are fast but visible for 40 cells. A long kill zone for ATGM-type units.
6. Hay stacks in the south: cover and decoy. They burn when hit.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Road: vehicles and infantry. Fast, long, open.
2. Left field: infantry in the sunflowers. Slow, hidden for a short part.
3. Right field: infantry behind the heaps. Medium speed.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
a,,c,cSS,,S,SS,=SSSc
c,Sa,v,,WW,,,,,=,S,,
uuuca,O,c,,ScSS=cS,W
uuuWWaM,SScSc,S=,a,W
uuuW,,SS,S,,WS,=,SS,
uuua,vMaSS,O,S,=cW,W
uuua,,,S,,,,,,,=,,,W
,,,,M,aSc,,M,c,=aW,c
O,cM,,,a,S*,c,,W,,c,
SMMS2vMS,cSccc,=,,,,
,,S,S,,a,a,,a,a=,S,,
WSOc,caSa,,cMa,=WSSM
S,cS,,,,SS,,c,,=s,,,
c,,,WvcM,,,ccc,=,,,,
c,,,W,,SW,,,,cS5aa,S
,,a,WW,SSaM,,M,=,,S,
,cMa,S,,SS,aSW,=,,,,
,a,,Sv,,,,,MSSM=,,S,
,acWWSSS,M,a,S,=ca,,
,ccccaS,ccSaW,,=,a,,
c,*caaSc,,c,Wc=c,SS,
,,,MMvSSHHHHW,=,,,,,
S,S,M,M,H1HHc,=,OSc,
S,S,M,SSHHHH,,=,SS,a
.S.MS.S.S.cW..=.cMS.
S....vc.SW.cO.=.MSSS
S.Oc.a.cc...S.=cMSc.
a...c..SWSSS.S=aSS.c
.a..M...WSSS.W=..M.c
aa...v.WM...W.=.c.S.
,,EE3EEEE4E,zS=,W,c,
,S,MdaS,,d,,,,=,Oc,M
,,,,cSXXXXXXX,=,,,cW
W,W,av,,,c,S,S=,M,W,
,,WTTTTT6TTTTTTTT,,,
,Mc,,,a,*,cSS,=,,,,c
cc,,,,,a,c,SSS=Suuuu
,cMBBBBBBSffff=,uuuu
,a,BBBBBB,ffff=,uuuu
a,SBBBBBB,,,c,=,,M,,
```

Legend: `,` Crop rows / stubble; `S` Bush; `c` Crater; `.` Empty ground; `a` Hay / straw stack; `W` Wreck / vehicle; `=` Road; `M` Spoil heap; `u` Sunflower field; `B` Forest belt; `T` Trench; `H` Hill; `v` Power-line pole; `E` Embankment; `O` Rock; `f` Fence; `X` Barbed wire; `*` Small decor (well, hive, scarecrow, bus stop); `d` Dugout; `i` Silo / tower / windmill; `s` Shed; `z` Camouflage net; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 28 | 276 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `S` | Bush | 40 | 117 | Hides, gives no cover. | Sim now |
| `c` | Crater | 42 | 76 | Decoration. Marks past fights. | Sim now |
| `a` | Hay / straw stack | 25 | 46 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `W` | Wreck / vehicle | 23 | 39 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `=` | Road | 3 | 38 | Fast route for vehicles and infantry. | Sim now |
| `M` | Spoil heap | 26 | 35 | Rock-like cover. | Sim now |
| `u` | Sunflower field | 2 | 27 | Hides who is inside, gives no cover. | Sim now |
| `B` | Forest belt | 1 | 18 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `T` | Trench | 1 | 14 | Defender cover. The player can build more. | Sim now |
| `H` | Hill | 1 | 12 | High ground: more range and sight, enemy direct fire less accurate. Cover behind it. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `E` | Embankment | 1 | 9 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `O` | Rock | 8 | 8 | Rock-like cover. | Sim now |
| `f` | Fence | 1 | 8 | Hides, gives no cover. | Sim now |
| `X` | Barbed wire | 1 | 7 | Slows infantry a lot. Player builds it. | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `d` | Dugout | 2 | 2 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |
| `z` | Camouflage net | 1 | 1 | Hides what is under it. No cover. | NEW Sim row |

**Empty ground:** 53 of 800 cells = 7% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 34% of the map. They are decoration and the open cells of the field between objects.

## Life

- Wind moves the grass in waves (animated).
- A combine works on the left and stops at the first shot.
- Dust devils (decor) cross the steppe.
- Larks fly up from craters when units step near.

## Winter and night

- **Winter:** Snow: heaps and stacks are white. Tracks of enemy show in the snow.
- **Night:** Open and bright under moon. A flare shows everything. The kurgan is a black shape.
