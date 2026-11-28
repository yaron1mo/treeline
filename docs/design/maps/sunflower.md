# 17. Sunflower

Category D · Difficulty: Hard · Fits: Campaign: chapter 4. Endless: 10 waves.

**Idea.** A big sunflower field fills the middle of the map. It hides everything inside, for both sides, and gives no cover. The player cannot see the enemy inside the field. A drone sees them from above. The defender hides at the edge and shoots what comes out.

## Defender functions

1. The small hill behind the line sees over the sunflowers (more sight). Spotters here find enemies in the field.
2. Left dugout: protects the left flank from mortars.
3. Right dugout: same on the right.
4. The cross track in the middle: a road for the enemy to leave the field. It is a kill zone.
5. The sunflower centre: hide a scout team here. They see, and the enemy does not know.
6. The silo north: a landmark and an enemy start point. A drone strike here hits their start.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Left track: vehicles. Hidden by sunflowers on one side.
2. Right track: vehicles. Same.
3. Through the field: infantry. Slow, hidden, not covered. A mortar kills a group in a ball.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
,,,,,,,,GGGG,,,,,,,,
,v,,,,,,GGGG,c,,,,,,
,,sa,,S,,,,,,,,,,a,,
,,,a,,,,,,,,,,,,a,,,
,,,,,,,,,6i,,,,,,S,,
,v,S,,,,,,,,,,,,,,,,
uuuu:uuuuuuuuu:uuuuu
uuuu:uuuuuOuuu:uuuuu
uuuc:uuuuuuuuu:uuWuu
uvuu:uuuucuuuu:uuuuu
uuuu:uWuuu5uuu:uuuuu
ucuu:uuuuuuuuu:uuuuu
uuuu:uuuuuuuuu:uuuuu
uuuu:uuuuuuuuu:uuuuu
uuuu:cuuuuuuuuWuuuuu
uuuu:uuuuu*uuu:uuuuu
uuuu:uuuuuuuuu:uuuuu
4:::::::::::::::::::
uucu:uuuuMuuuu:uuuuu
uuuu:uuuuuuuuu:uuuuc
uuuWu:uuuuuuuuu:uuuu
cuuuu*uuuuuuuuu:uuuu
uuuuu:uuuuuuuuu:uuuu
uuuuu:Muuuuuuuu:uuuu
uuuuu:uuuuuuucu:Wuuu
uuuuu:uuuuuuucu:uuuu
cuuuu:uuuuuuOuu:uuuc
uuuuu:uuuuuuuuu:uuuu
uuuuu:uuuWuuuuu:uuuu
uuuuu:uuuuuuuuu:ucuu
uuuuu:uuuuuuuuu:uuuu
,,,,,:,z,**,S,,:,,S,
z,EEEEEEEEEEEEEEEE,z
,,c2,:,,HHHH,,,:3,,,
,,,,,:,,H1HH,*,:,,,,
,,,,,:,,HHHH,,,:,,,,
,,TTTTTTTTTTTTTTTT,,
BBBBB:,,,,,,S,SBBBBB
BBBBB:,,S,,,,,,BBBBB
BBBBB:,,,,,,,,,BBBBB
```

Legend: `u` Sunflower field; `,` Crop rows / stubble; `:` Field track; `B` Forest belt; `E` Embankment; `T` Trench; `c` Crater; `H` Hill; `G` Grove / forest; `S` Bush; `W` Wreck / vehicle; `*` Small decor (well, hive, scarecrow, bus stop); `a` Hay / straw stack; `v` Power-line pole; `z` Camouflage net; `O` Rock; `M` Spoil heap; `d` Dugout; `s` Shed; `i` Silo / tower / windmill; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `u` | Sunflower field | 4 | 409 | Hides who is inside, gives no cover. | Sim now |
| `,` | Crop rows / stubble | 10 | 181 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `:` | Field track | 6 | 77 | Field track. Light speed bonus. | Sim now |
| `B` | Forest belt | 2 | 30 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `E` | Embankment | 1 | 16 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `T` | Trench | 1 | 16 | Defender cover. The player can build more. | Sim now |
| `c` | Crater | 13 | 14 | Decoration. Marks past fights. | Sim now |
| `H` | Hill | 1 | 12 | High ground: more range and sight, enemy direct fire less accurate. Cover behind it. | Sim now |
| `G` | Grove / forest | 1 | 8 | Soft sight block and cover inside. | Sim now |
| `S` | Bush | 8 | 8 | Hides, gives no cover. | Sim now |
| `W` | Wreck / vehicle | 6 | 6 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 4 | 5 | Decoration. Landmark. | Decoration only |
| `a` | Hay / straw stack | 2 | 4 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `v` | Power-line pole | 3 | 3 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `z` | Camouflage net | 3 | 3 | Hides what is under it. No cover. | NEW Sim row |
| `O` | Rock | 2 | 2 | Rock-like cover. | Sim now |
| `M` | Spoil heap | 2 | 2 | Rock-like cover. | Sim now |
| `d` | Dugout | 2 | 2 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |

**Empty ground:** 0 of 800 cells = 0% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 23% of the map. They are decoration and the open cells of the field between objects.

## Life

- Sunflower heads follow the sun (animated). They bend when units pass through.
- Birds rise in a cloud at a shell hit.
- A combine harvests a strip and stops.
- Bees (decor) at the hives at the edge.

## Winter and night

- **Winter:** Dry brown stalks, snow: they hide less. The field is a bad hiding place in winter.
- **Night:** Dark field. Only tracers show the enemy. Flares make the field a golden sea.
