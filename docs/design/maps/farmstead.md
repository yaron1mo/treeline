# 20. Farmstead (replaces Tree line)

Category D · Difficulty: Easy · Fits: Campaign: chapter 1 (teaches cover and kill zones). Endless: 7 waves.

**Idea.** Replaces Tree line. A farm with a silo, sheds and a combine stands north of a thin tree line. A second small farm stands on our side. The farm is the first objective: the enemy comes to the farm, takes cover in it and uses it as a base. The tree line is the one belt on the map. Spoil heaps give small cover.

## Defender functions

1. The south silo: a tower. A spotter on it sees over the tree line and the farm.
2. The tree line: the only belt. It hides a flank squad.
3. The wire and the embankment line: the kill zone at the road.
4. Dugouts on the embankment: protect from mortars.
5. The sunflower strip: hides enemy flank and our scouts.
6. The enemy farm (north): the first fight happens there. A drone shows enemy units inside the yard.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Road through the farm: vehicles and infantry. The farm blocks sight.
2. Left field: infantry behind the sunflowers.
3. Right of the tree line: slow flank. Belt gap.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
,,,SWWS,Sc=,cSa,a,W,
S,WcW,c,,,=,c,,Sa,S,
MW,W,cvcc,=,,,SS,,,c
,M,,,c,,,,=SffffcSS,
cS,i1c,,,,=Sffff,,c,
,,cii,ss,,=,ffff*c,c
,cccc,vSS,aaffff,cc,
,M,sscS,,,=,a,,S,,,,
,ca,,aa,M,=,acacS,,S
,McW,,,S,a=,W,ca,,cc
cc,c,,v,Wc6,S,aSS,,a
,c,M,,,,,,*,c,,,SSaS
BBBBBBBBB==SS2,,,,,,
,WW,,,,,,==BBBBBBBBB
S,,M,,v,c,=,S,,,c,,,
,,,c,c,Sac=,S,M,,,,c
uuuu,cS,S,=,uuuuuuuu
uuuu,O,Sac=,uuuuuuuu
uuuu,cvSc,=,uuuuuuuu
uuuu,,S,,,=auuuuuuuu
uuuuW,,S,=,auuuuuuuu
,,SSccS,,=,,uuuuuuuu
,S,,S,v,M=,,uuuuuuuu
,SS,,S,SS=,,,a,,,,,,
cas.ccS..=..M..c.3.S
..aaO.Sc.=...ac.....
c..ccWv..=.......a..
.c...c...=c..cWc.S..
..ScS.XXXX5XXX...Sa.
MS.c.S.SS=..a....Wa.
,S,EEEvEEEEEEEEEESaa
,,,,Wd,,Sz,,,,d,,,,,
S,,,,,W,4=,,,,S,ScWc
W,,,,S,,*=,,S,S,,,W,
WW,,,,v,,=S,,,S,,,,c
,,MTTTTTTTTTTTTTT,,,
,c,,,,,,,=,,S,GGGGGG
BBBBBBB,,ffffSGGGGGG
BBBBBBB,,ffffaGGGGGG
BBBBBBB,,=,,,cGGGGGG
```

Legend: `,` Crop rows / stubble; `u` Sunflower field; `S` Bush; `c` Crater; `.` Empty ground; `B` Forest belt; `=` Road; `a` Hay / straw stack; `f` Fence; `G` Grove / forest; `W` Wreck / vehicle; `T` Trench; `E` Embankment; `M` Spoil heap; `v` Power-line pole; `X` Barbed wire; `i` Silo / tower / windmill; `s` Shed; `*` Small decor (well, hive, scarecrow, bus stop); `O` Rock; `d` Dugout; `z` Camouflage net; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 22 | 267 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `u` | Sunflower field | 2 | 76 | Hides who is inside, gives no cover. | Sim now |
| `S` | Bush | 36 | 74 | Hides, gives no cover. | Sim now |
| `c` | Crater | 37 | 70 | Decoration. Marks past fights. | Sim now |
| `B` | Forest belt | 3 | 39 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `=` | Road | 7 | 34 | Fast route for vehicles and infantry. | Sim now |
| `a` | Hay / straw stack | 18 | 32 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `f` | Fence | 2 | 24 | Hides, gives no cover. | Sim now |
| `G` | Grove / forest | 1 | 24 | Soft sight block and cover inside. | Sim now |
| `W` | Wreck / vehicle | 14 | 23 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `T` | Trench | 1 | 14 | Defender cover. The player can build more. | Sim now |
| `E` | Embankment | 2 | 13 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `M` | Spoil heap | 11 | 12 | Rock-like cover. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `X` | Barbed wire | 1 | 8 | Slows infantry a lot. Player builds it. | Sim now |
| `i` | Silo / tower / windmill | 2 | 5 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |
| `s` | Shed | 3 | 5 | Small house: blocks sight and movement. | NEW Sim row |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `O` | Rock | 2 | 2 | Rock-like cover. | Sim now |
| `d` | Dugout | 2 | 2 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `z` | Camouflage net | 1 | 1 | Hides what is under it. No cover. | NEW Sim row |

**Empty ground:** 63 of 800 cells = 8% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 33% of the map. They are decoration and the open cells of the field between objects.

## Life

- A combine works in the yard and flees at the first shot.
- Chickens (decor) run in the yard. A dog barks.
- Smoke from the farm chimney. A scarecrow falls over when hit.
- Silo flag in the wind.

## Winter and night

- **Winter:** Snow on the roofs. The farm is easy to spot from far.
- **Night:** The farm lights a lamp. The enemy at the yard is visible: a target at night.
