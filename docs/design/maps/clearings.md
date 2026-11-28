# 11. Clearings

Category C · Difficulty: Medium · Fits: Campaign: chapter 2-3. Endless: 10 waves.

**Idea.** A big forest with clearings. Sight is short. The fights happen at the clearing edges. A clearing is a room: the player chooses which rooms to hold.

## Defender functions

1. The centre clearing (sunflowers): the biggest kill zone. Units on its edge shoot into it.
2. North clearing with a farm: the first room. A mortar here sees the forest road.
3. South clearing: the last room before our line.
4. The embankment between the clearings: cover for the second line.
5. Forest paths: the only fast routes. Wire on a path blocks infantry.
6. Left net hides a scout team on the forest edge.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Track chain through the three clearings: fast, all unit types, predictable.
2. Through the forest: infantry only. Slow, hidden, short sight.
3. Around the clearings on the right: slow flank. The drone sees it.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
GGGGGGGGG:GGGGGGGGGG
GGGGGGOG:GGGGGGGGGGG
GGGGGGGG:GGGGSSSGvGG
GGGGG,GG:GGGGSSSGGGG
GGG,,,,:GGGGGGGGGGGG
GG,,,,:*,GGGGGGGGGGG
GG,,,s:,,GGGGGGGGvGG
G,c,a,a,,,GGGGGGGGGG
GGc,ia,,,GGGGGGGGGGG
GG,,,,2,,GGGGGGGGGGG
GGGc,,,:GGGGGGGGGvGG
GGGGG,GS:GGGGGGGNGGG
GGGGGGGGG:GGGGGNNNGG
GGGGGGGGG:GGGGNNNNNG
GGGGGGGGGG:G5,GNNvGG
GGGGGGGGGGc:,,,,NGGG
GGGGGGGGG,,c:,,,,,GG
GGGGGGGGG,,,,uS,S,GG
GGGGOGGGG,,Wuuuu,vGG
GGGGGG:::,*uu1uuu,,G
GGz6zGGGG,,uuuuu,,GG
GGzzzGGGG,,,,ucW,,GG
GGGGGOGGG,,,:,,,,vGG
GGGGGGGGGG,:,,,,cGGG
GGGGGGGGGGG:G.GGGGGG
GGGGGGGGGG:GGGGGGSGG
GGGGGSGGGG:GGGGGGvGG
SGGEEE4EEEEGGGGGGGGG
GGG.d...d:GGGGGGGGGG
GG.c....:..GGGGGGGGG
GG,,,,c,*,,GGGGGGvGG
G,,,,,3,,,,,GGGGGGGG
GG,,,,,,M,,GGGGGGGGG
GG,,,M,,:,,GGGGGGGGG
GGG,S,,,:,GGGGGGGvGG
GGGGGG,G:GGGGGGGGGGG
GG,,,,,,,,,,,,,,,GGS
GG,,,,,,,,,,,,,,,ffG
GG,,,,,,,,,,,,,,,ffG
GGGGGGG:GGSGGGGGGGGG
```

Legend: `G` Grove / forest; `,` Crop rows / stubble; `:` Field track; `u` Sunflower field; `S` Bush; `N` Burned belt; `.` Empty ground; `v` Power-line pole; `c` Crater; `E` Embankment; `z` Camouflage net; `f` Fence; `O` Rock; `*` Small decor (well, hive, scarecrow, bus stop); `a` Hay / straw stack; `W` Wreck / vehicle; `d` Dugout; `M` Spoil heap; `s` Shed; `i` Silo / tower / windmill; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `G` | Grove / forest | 3 | 497 | Soft sight block and cover inside. | Sim now |
| `,` | Crop rows / stubble | 3 | 164 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `:` | Field track | 7 | 29 | Field track. Light speed bonus. | Sim now |
| `u` | Sunflower field | 1 | 17 | Hides who is inside, gives no cover. | Sim now |
| `S` | Bush | 10 | 15 | Hides, gives no cover. | Sim now |
| `N` | Burned belt | 1 | 12 | Looks like cover, hides almost nothing, gives no cover. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `c` | Crater | 7 | 9 | Decoration. Marks past fights. | Sim now |
| `E` | Embankment | 1 | 8 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `z` | Camouflage net | 1 | 6 | Hides what is under it. No cover. | NEW Sim row |
| `f` | Fence | 1 | 4 | Hides, gives no cover. | Sim now |
| `O` | Rock | 3 | 3 | Rock-like cover. | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `a` | Hay / straw stack | 1 | 3 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `W` | Wreck / vehicle | 3 | 3 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `d` | Dugout | 2 | 2 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `M` | Spoil heap | 2 | 2 | Rock-like cover. | Sim now |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |

**Empty ground:** 12 of 800 cells = 2% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 20% of the map. They are decoration and the open cells of the field between objects.

## Life

- Birds fly up from the clearings when a fight starts.
- A deer (decor) runs across a clearing.
- Smoke from the farm shed after a hit.
- Light falls through the leaves (animated light shafts).

## Winter and night

- **Winter:** Snow in the clearings. The forest stays dark: the contrast is stronger.
- **Night:** Clearings are lit by moon. The forest is black. A good map for night waves.
