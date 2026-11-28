# 14. Grove islands

Category C · Difficulty: Medium · Fits: Campaign: chapter 3. Endless: 10 waves.

**Idea.** Eleven groves of different size stand in the field like islands. The ground between them is a sea of crop rows with bushes. Who sees first from an island wins. Each island has its own object: a ruin, a shed, a pond, a silo.

## Defender functions

1. North grove with a ruin: first island. A spotter here warns the line.
2. Centre island with a shed: the biggest. It sees all other islands. The key point.
3. Pond island (right): safe from vehicles. Mortars here reach the road.
4. South-left grove: the last cover before our trench.
5. Embankment and dugout: the second line.
6. West grove (small): a trap. Enemies hide here and surprise the flank.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Track through the islands: vehicles and infantry. Long, bends.
2. Island hopping: infantry goes from grove to grove, using bushes. Slow.
3. Right edge: sunflowers hide infantry. It joins the pond island.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
,,c,,,,c,,:,S,,,,,,,
,,,,,,M,,,:,,S,,,G,,
,,,,G,S,,,:,S,,,GGGv
,,GGGGG,,c:,,,cGGGGG
,GGGGGGG,,:,,,,,GGG,
,GG:GzGG,W:,,,G,,G,,
GGG:GGGGG,:GGGrGGG,v
,GG:G1GG,,:GGGGGGGG,
,GG:GGGG,G:aaGGGGGGG
,,G:GGGc,,:GGGGuuuuu
,,G:G,,,,M:GGGGuuuuu
,GG:,,,,,,:,,,Guuuuu
GGG:G,S,G,:,S,,uuuuu
,G:G,,,GGG:,,,*,,,,,
,,:,,,GGGG:,:,,,,P,v
S,:,,,GG*G:,:,,2PPP,
,,:,,GGGGG:G:,,,PPP,
,,:,,,GGGG:,,:,PPPPP
S,:,,,GG6G:,,:,GPPPv
,G:G,,,GGG:M,:GGPPP,
GG:GGS,,a:,,GG:GGPG,
,GGG,,c,,:O,GG:GGGG,
,,G,,,,S,:,GGG:GGGGv
,,c,,,S,,:,,GGz3GGG,
..S.GcS.W:..GG:rGGG.
..GOGGGS.:...GG:GG..
..GGGGG..:M.M..:...v
.GGGGGGG.:.SG.S:....
..GGG*G.c:GGGGGG:...
..GGGGG.G:GGGGGG:...
,,,,G,,GG:GG4GGG:G,v
,,,,,,,SG:GGGGGGG,,,
uuu,,,,,,:GGGGGGG,,,
uuu,,G,,EEEEE,GGGGG,
uuuGGGGG,:5,,,GGGGGv
uuuGGGGG,:,,,GGGGGGG
,,GGGGGGG:,,c,GGGGG,
,,,GGGTTTTTTTTTGGGG,
,,,GGGGG,:,,,,S,G,,v
,,,S,G,,,:,,,,,,,,,,
```

Legend: `G` Grove / forest; `,` Crop rows / stubble; `:` Field track; `.` Empty ground; `u` Sunflower field; `S` Bush; `P` Pond; `c` Crater; `v` Power-line pole; `T` Trench; `M` Spoil heap; `E` Embankment; `r` Ruined house; `a` Hay / straw stack; `*` Small decor (well, hive, scarecrow, bus stop); `z` Camouflage net; `W` Wreck / vehicle; `O` Rock; `s` Shed; `d` Dugout; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `G` | Grove / forest | 10 | 289 | Soft sight block and cover inside. | Sim now |
| `,` | Crop rows / stubble | 16 | 271 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `:` | Field track | 6 | 70 | Field track. Light speed bonus. | Sim now |
| `u` | Sunflower field | 2 | 32 | Hides who is inside, gives no cover. | Sim now |
| `S` | Bush | 14 | 19 | Hides, gives no cover. | Sim now |
| `P` | Pond | 1 | 19 | Blocks all. Forces routes around it. | Sim now |
| `c` | Crater | 10 | 10 | Decoration. Marks past fights. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `T` | Trench | 1 | 9 | Defender cover. The player can build more. | Sim now |
| `M` | Spoil heap | 5 | 5 | Rock-like cover. | Sim now |
| `E` | Embankment | 1 | 5 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `r` | Ruined house | 3 | 3 | Cover. Infantry climb through slowly. | Sim now |
| `a` | Hay / straw stack | 2 | 3 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `z` | Camouflage net | 2 | 2 | Hides what is under it. No cover. | NEW Sim row |
| `W` | Wreck / vehicle | 2 | 2 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `O` | Rock | 2 | 2 | Rock-like cover. | Sim now |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |
| `d` | Dugout | 1 | 1 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |

**Empty ground:** 45 of 800 cells = 6% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 34% of the map. They are decoration and the open cells of the field between objects.

## Life

- Each island has its own birds. They rise when a unit enters.
- A tractor works between the islands and flees at the first shot.
- Smoke from a ruin on the north island.
- Silo shows a flag that moves in the wind.

## Winter and night

- **Winter:** Snow on the sea of crop rows. Islands are dark: easy to see from far.
- **Night:** Groves are black. The player cannot see inside them: the attackers hide too.
