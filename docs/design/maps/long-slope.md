# 9. Long slope

Category B · Difficulty: Hard · Fits: Campaign: late. Endless: 20 waves.

**Idea.** The ground slopes down to our line. The enemy starts high and sees us. A small hill near our line changes who has the height on the way.

## Defender functions

1. The small hill near our line: the only high ground we have. Everything else is below the enemy start.
2. The embankment hides units on the slope from long-range fire.
3. Dugouts behind the embankment: strong cover from indirect fire.
4. The left grove: close-range ambush point on the road.
5. The right sunflower strip: hides a flank squad.
6. The enemy hill (north): enemy spotters and mortars start here. A drone strike hits them first.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Road down the slope: vehicles and main infantry. Fast, long, in full view.
2. Left grove path: infantry hidden by the trees.
3. Right strip: slow infantry flank through the sunflowers.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
,,,HHHHHHH=HHHHHHH,,
,,aHHHHHHH=HHHHHHH,,
,,,HHHHHHH=HvHHHHH,,
,,SHHHHHHH=HHHHHHHc,
,S,HHHHHHH=HHHHHHH,S
,S,HHHHHHH6HHHHHHH,,
,,,HHHHHHH=HvHHHHH,c
,,,HHHHHHH=HHHHHHH,,
,S,HHHHHHH=HHHHHHH,,
,,,HHHHHHH=HHHHHHH,,
GGGGG,,,,,=,vSSGGGGG
GGGGG,,,,,=,,,,GGGGG
GG4GG,,S,,=,,,,GGGGG
GGGGG,,,,*=,,,,GGGGG
GGGGG,,S,,=,v,,GGGGG
,,,Sc,,,,W=,,,,,,O,,
,,,,,,S,,,=,,,SOO,aa
BBBBBBBBBB=BB,,,,c,,
BBBBBBBBBB=BvS,S,a,,
,,,,,,,,,,=S,,Muuuuu
,,,,M,,,,=,,,,,uuuuu
,ccS,,SSO=,,,,,u5uuu
,,,,S,,S,=,,v,,uuuuu
,,,SS,,SS=,a,,,uuuuu
EEEEEEEE.=2aEEEEEEEE
...3.S..c=..a..dS.O.
uuuu..S..=..v.....s.
uuuu.W.ca=.a.....a..
uuuu...S.=...S..SMa.
uuuuc.S..=......c.c.
uuuu,,,,,=,,v,,S,,c,
,,,,,,HHH=HHHH,,,,,,
,,,,,,HHHdHHHH,,,,,,
,,,,c,HHH1HHHH,,,,,,
,,*,,,HHH=HHvH,,,,,,
,a,,,,HHH=HHHH,,,,,,
c,,,,,,,,=c,Sc,,,,,,
,,,,TTTTTTTTTTTT,ff,
BBBBBB,,,=,,vSBBBffB
BBBBBB,,,=S,,,BBBBBB
```

Legend: `,` Crop rows / stubble; `H` Hill; `.` Empty ground; `G` Grove / forest; `B` Forest belt; `u` Sunflower field; `=` Road; `S` Bush; `c` Crater; `E` Embankment; `a` Hay / straw stack; `T` Trench; `v` Power-line pole; `O` Rock; `f` Fence; `M` Spoil heap; `d` Dugout; `*` Small decor (well, hive, scarecrow, bus stop); `W` Wreck / vehicle; `s` Shed; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 11 | 272 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `H` | Hill | 4 | 172 | High ground: more range and sight, enemy direct fire less accurate. Cover behind it. | Sim now |
| `G` | Grove / forest | 2 | 50 | Soft sight block and cover inside. | Sim now |
| `B` | Forest belt | 4 | 45 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `u` | Sunflower field | 2 | 45 | Hides who is inside, gives no cover. | Sim now |
| `=` | Road | 3 | 38 | Fast route for vehicles and infantry. | Sim now |
| `S` | Bush | 23 | 35 | Hides, gives no cover. | Sim now |
| `c` | Crater | 14 | 16 | Decoration. Marks past fights. | Sim now |
| `E` | Embankment | 2 | 16 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `a` | Hay / straw stack | 8 | 12 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `T` | Trench | 1 | 12 | Defender cover. The player can build more. | Sim now |
| `v` | Power-line pole | 10 | 10 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `O` | Rock | 3 | 5 | Rock-like cover. | Sim now |
| `f` | Fence | 1 | 4 | Hides, gives no cover. | Sim now |
| `M` | Spoil heap | 3 | 3 | Rock-like cover. | Sim now |
| `d` | Dugout | 3 | 3 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 2 | 2 | Decoration. Landmark. | Decoration only |
| `W` | Wreck / vehicle | 2 | 2 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |

**Empty ground:** 57 of 800 cells = 7% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 34% of the map. They are decoration and the open cells of the field between objects.

## Life

- Wind blows dust along the slope. Poles with wire on the right.
- A burned truck smokes halfway down.
- Birds scatter when enemies start the run.
- Sheep on the north hill run away.

## Winter and night

- **Winter:** Snow on the slope: enemies are easy to see against it.
- **Night:** Enemy flares light the hill from behind: silhouettes.
