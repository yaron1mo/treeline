# 12. Forest road

Category C · Difficulty: Hard · Fits: Campaign: chapter 3. Endless: 7 waves.

**Idea.** One wide road runs through a dark forest. This is the map of ambushes: the defender hides on both edges and shoots across the road. Only one lane, but it is a deadly lane.

## Defender functions

1. North-left ambush nest (dugout and net): first shot, then drop back.
2. North-right ambush nest: crossfire with the left nest.
3. Middle clearing on the left: a place for mortars. It sees the road with a gap in the trees.
4. Middle clearing on the right: same, with logs for cover.
5. South trench: the last line before the forest ends.
6. Wire and hedgehog on the road: it makes a queue in the kill zone.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. The road: all vehicles and infantry. Predictable, one lane.
2. Forest left: infantry, slow, short sight. They want to flank the nests.
3. Forest right: same, plus a track to the clearing.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
GGG:GGGGG==GGGGG:GGG
GGG:GGGGG==GGGvG:GGG
GGG:GGGGG=*GGGGG:GGG
GGGG:GGGG==GGGGG:GGG
GGGO:GGGG==GGGG:GSGG
GGGG:GGGM==MGGG:GGSG
GGGG:GGGG==GGGv:GGSG
GGGS:GSGG==GGGG:GSGG
GOGG:GGGGW=GGGG:GGGG
GGGG:GGGG==GGGG:GGGS
GGGGG:GzG==GGGG:GGGG
GGGGG:1dG==GGd2GGGGG
GGGGG:GGS==GGG:GGGGG
GGGGGGGGGXXGGG:GGGGG
GGGGGG,,,,,,s,:GGGGG
GGGGGGa,,c,,,aGGGGGG
GGGGGGaf,,,,,,vGGGGG
GGGGGG,*,6cc,aGGGGGG
GGGGGG,,,W,,,,GGGGGS
GGGGGGc,f,Wc,,GGGGGG
GGGGGG,,,,,,c,GGGGGG
GGGGGGGGGG==GGvGSGGG
GGGGGGGGMG==MGGGGGGG
GGGGGGzGGG==GGOGGGGG
GGGGGG3GGG==Gd4GGGGG
GGGGGGGGGG==GzGGGGGG
GGGGGGGGGG==GGvGGGGG
GGGGGGEEEEEEEEGGGGGG
GGGGGGGGGGW=GGSGGGGG
GGGGGGGGGXX=GGGGGGGG
GGGGGG,,c,,,c,GGGGGG
GGGGGGia,,,,,,vGGGGG
GGGGGG,,f,,,,aGGGGGG
GGGGGG,,,,,,*,GGGGGG
GGGGGG,Sc,,,,,GGGGGG
GGGGGdGGGG5=GGGGGGGG
GGGGTTTTTTTTTTvTGGGG
GGGGGGGGGGGGGGGGGGGG
GGGGGGGGGGGGGGGGGGGG
GGGGGGGGGGGGGGGGGGGG
```

Legend: `G` Grove / forest; `,` Crop rows / stubble; `=` Road; `:` Field track; `S` Bush; `T` Trench; `c` Crater; `E` Embankment; `v` Power-line pole; `a` Hay / straw stack; `d` Dugout; `M` Spoil heap; `W` Wreck / vehicle; `X` Barbed wire; `*` Small decor (well, hive, scarecrow, bus stop); `O` Rock; `z` Camouflage net; `f` Fence; `s` Shed; `i` Silo / tower / windmill; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `G` | Grove / forest | 1 | 577 | Soft sight block and cover inside. | Sim now |
| `,` | Crop rows / stubble | 2 | 70 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `=` | Road | 4 | 40 | Fast route for vehicles and infantry. | Sim now |
| `:` | Field track | 2 | 28 | Field track. Light speed bonus. | Sim now |
| `S` | Bush | 9 | 12 | Hides, gives no cover. | Sim now |
| `T` | Trench | 2 | 11 | Defender cover. The player can build more. | Sim now |
| `c` | Crater | 7 | 10 | Decoration. Marks past fights. | Sim now |
| `E` | Embankment | 1 | 8 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `v` | Power-line pole | 7 | 7 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `a` | Hay / straw stack | 5 | 6 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `d` | Dugout | 5 | 5 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `M` | Spoil heap | 4 | 4 | Rock-like cover. | Sim now |
| `W` | Wreck / vehicle | 3 | 4 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `X` | Barbed wire | 2 | 4 | Slows infantry a lot. Player builds it. | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `O` | Rock | 3 | 3 | Rock-like cover. | Sim now |
| `z` | Camouflage net | 3 | 3 | Hides what is under it. No cover. | NEW Sim row |
| `f` | Fence | 3 | 3 | Hides, gives no cover. | Sim now |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |

**Empty ground:** 0 of 800 cells = 0% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 9% of the map. They are decoration and the open cells of the field between objects.

## Life

- Log piles and a sawmill shed at the clearing.
- Birds fly up in a line along the road when vehicles pass.
- A burned truck on the road smokes.
- Rays of light through trees (animated).

## Winter and night

- **Winter:** Snow covers the road: vehicles slip, speed lower. Footprints show where infantry walked.
- **Night:** Pitch dark. Headlights of vehicles are the only light: the player sees them first.
