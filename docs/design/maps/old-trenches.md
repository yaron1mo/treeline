# 18. Old trenches

Category D · Difficulty: Medium · Fits: Campaign: chapter 4. Endless: 10 waves.

**Idea.** Three old trench lines with wire cross the field. They are free cover for both sides. The player can reuse the trenches, and the enemy uses them too. Each line is a fall-back position: hold, retreat, hold again.

## Defender functions

1. Line 3 (nearest): the strongest. Dugouts and wire. The last stand.
2. Line 2, left: a flank position that covers the road gap in the wire.
3. Line 2, right: same on the right. Crossfire with the left.
4. Line 3, right dugout: protects from mortars.
5. Line 1 (far): the first line. Hold it only with a few units; then retreat to line 2.
6. The road gap: wire is open here. All vehicles use it. A kill zone in the middle.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Road gap: vehicles and infantry. Fast, predictable.
2. Along the communication trench: infantry. Slow, protected from fire, ends inside our line.
3. Cut the wire: infantry slowly cuts through. Good for the player to see the cut early.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
BBBBBBB,,M=,,SBBBBBB
BBBBBBB,,,=,,,BBBBBB
BBBBBBBS,,=,v,BBBBiB
,a,a,,,M,,=,,c,csccc
a,,,,,,M,,=,,,,,W,MM
,,,,c,,M,,=,,,c,,Mc,
XXXXXXXXX:=XvXXXXXXX
,c,,,,W,,,=cMMSS,,,,
TTTTdTTTTT=TTTT5TTTT
W,,z,c,,cT=,c,,c,,,,
WW,M,,,,,T=cv,SSc,,,
,,,,SW,,,T=,,,,Sa,,,
,,,,,WcSST6,,,S,a,,,
,,,,*c,,,T=,M,S,S,,,
,,,,,,,,,T=av,S,,,M,
XXXXXXXXX:=XXXXXXXXX
,,,,,S,,,T=,,,,,,,,,
,,TTTT2TTT=TT3TTTT,c
S,cc,,c,,,z,vT,,,,S,
ccc,SSS,,,=c,TM,S,,c
c,,,SOSc,,=S,T,,,S,c
,,W,,,S,,,=,MTa,,,MM
,c,S,,,MS,=SvT,,*,,,
,,,MMMM,SS=,,T,aac,M
XXXXXXXX::=XXXXXXXXX
.c.cS...c:=..Tc..aac
TTT1TTT..T=TvT4TTTTT
....W.....=....z.ca.
uuuuMWc...=...cSa...
uuuu...cc.=....M.O..
uuuu,,,c,,=Sv,,Mc,,,
uuuu,,,,cc=W,M,,,a,,
c,aSEEEEEEEEEEEE,,,S
BBBB,,,,,,=,S,W,GGGG
BBBBS,,,,,=,v,WcGGGG
BBBB,,,a,,*,,,,,GGGG
,,,TTTTTTTTTTTTTTGGG
BBBBBB,,ffff,,BBBBBB
BBBBBB,,ffffvcBBBBBB
BBBBBB,,S,=,S,BBBBBB
```

Legend: `,` Crop rows / stubble; `B` Forest belt; `T` Trench; `X` Barbed wire; `c` Crater; `.` Empty ground; `S` Bush; `=` Road; `M` Spoil heap; `a` Hay / straw stack; `u` Sunflower field; `G` Grove / forest; `W` Wreck / vehicle; `E` Embankment; `v` Power-line pole; `f` Fence; `d` Dugout; `:` Field track; `z` Camouflage net; `*` Small decor (well, hive, scarecrow, bus stop); `O` Rock; `i` Silo / tower / windmill; `s` Shed; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 28 | 284 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `B` | Forest belt | 5 | 86 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `T` | Trench | 14 | 72 | Defender cover. The player can build more. | Sim now |
| `X` | Barbed wire | 7 | 52 | Slows infantry a lot. Player builds it. | Sim now |
| `c` | Crater | 33 | 49 | Decoration. Marks past fights. | Sim now |
| `S` | Bush | 25 | 40 | Hides, gives no cover. | Sim now |
| `=` | Road | 5 | 33 | Fast route for vehicles and infantry. | Sim now |
| `M` | Spoil heap | 15 | 26 | Rock-like cover. | Sim now |
| `a` | Hay / straw stack | 12 | 16 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `u` | Sunflower field | 1 | 16 | Hides who is inside, gives no cover. | Sim now |
| `G` | Grove / forest | 1 | 15 | Soft sight block and cover inside. | Sim now |
| `W` | Wreck / vehicle | 9 | 14 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `E` | Embankment | 1 | 12 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `v` | Power-line pole | 10 | 10 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `f` | Fence | 1 | 8 | Hides, gives no cover. | Sim now |
| `d` | Dugout | 6 | 6 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `:` | Field track | 3 | 5 | Field track. Light speed bonus. | Sim now |
| `z` | Camouflage net | 3 | 3 | Hides what is under it. No cover. | NEW Sim row |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `O` | Rock | 2 | 2 | Rock-like cover. | Sim now |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |

**Empty ground:** 46 of 800 cells = 6% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 36% of the map. They are decoration and the open cells of the field between objects.

## Life

- Old sandbags and a broken ladder in the trenches (decor).
- A burned truck smokes on the road.
- Crows sit on the wire and rise at the first shot.
- Wind moves a torn flag on a pole.

## Winter and night

- **Winter:** Snow fills the trenches: the cover is lower. Footprints show infantry use.
- **Night:** Flares reveal the trench lines. The wire glints.
