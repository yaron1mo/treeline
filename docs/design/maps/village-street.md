# 21. Village street

Category V (village) · Difficulty: Medium · Fits: Campaign: chapter 5 (first village). Endless: 10 waves. Exists as a Sim field (Claude, 2026-11-27).

**Idea.** A long village street from the enemy edge to our line. Houses stand on both sides. Vehicles keep to the street. Infantry goes through gardens and lanes. One defender per house. Closest to a classic tower defence: few fixed routes.

## Defender functions

1. The ruin in the middle (burned): cover for both sides. It is the half-way point of the street.
2. The house in the middle of the east row: a defender inside sees the whole south half of the street.
3. Houses in the south: the last line of houses. A defender inside is strongly protected.
4. The line trench and dugouts: the last position.
5. The east fence line: infantry walks behind it, hidden from the street. It gives no cover, so do not trust it.
6. The silo and the shed at the north end: a landmark and the enemy start point.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. The street: vehicles and infantry. Fast, a kill zone between the houses.
2. West gardens: infantry through the fences and groves. Slow, hidden.
3. East sunflower field: slow flank, hides infantry, ends at the east houses.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
,,:,,,,,,,=,,,,,,,:,
,,:,,,,,,,=,,,,,c,:,
BBBBBBBB,c=,,BBBBBBB
BBBBBBBB,,=,,BBBBBBB
,,:,cGGf,,=hfGG,,,:,
,,6,,GGfh,=vfGG,c,:,
,,as,GGfh,=hfGGuuu:u
,,:a,S,f,,=,f,,uu:uu
,,:SSGGf,,=rfGGuu:uu
,,:cWGGfr,=,fGGuu:uu
,,:,,GGfh,=hfGGuu:uu
,,:,,GGf,,=,f,Wuu:uu
,S:,,GGf,,=2fGGuu:uu
,,:,SGGfh,=,fGGuu:uu
,S:,,GGfh,=hfGGuu:uu
SW,:,GGfvS=,5,,uu:uu
S,,:,,cf,,=1fGGuu:uu
,,S:,c,fh,=,fGGuu:uu
,,,:,GGfh,=*fGGuu:uu
,,,:,GGf,,=,fc,uu:uu
,,,,:GGf,W=hfGGu:uuu
,,,,:GGf*=,,fGG,:,,,
,,,c:GGfh=Whf,,,:,,,
,,,,:GGf,=SSf,,W:,,,
....:GGf.=chfGG.:..S
.....:Gfh=.vfGG.:...
S...W:Gf3=.hfGGM:...
.....:GfS=..f..:...M
.....G:f.=.rfGG:....
.....G:fh=..fGG:a...
,,,,,G:f,=,*fG:,,,M,
,,,,,G:fh=,,f,:,,,,,
,,,c,W:,,=cS,,:,,,,,
,,SS,,,:,,=,,:,,,,,,
,,,S,,,:z,=,z:,,,S,,
,,S,d,,,v,=,,,,dSa,,
,,,,TTTTTT4TTTTTTa,,
BBBBB,W,ffff,,,BBBBB
BBBBB,,,ffff,,cBBBBB
BBBBB,,c,,=,,,,BBBBB
```

Legend: `,` Crop rows / stubble; `G` Grove / forest; `:` Field track; `f` Fence; `B` Forest belt; `u` Sunflower field; `.` Empty ground; `=` Road; `S` Bush; `h` House; `c` Crater; `T` Trench; `W` Wreck / vehicle; `a` Hay / straw stack; `v` Power-line pole; `r` Ruined house; `*` Small decor (well, hive, scarecrow, bus stop); `M` Spoil heap; `z` Camouflage net; `d` Dugout; `i` Silo / tower / windmill; `s` Shed; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 31 | 271 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `G` | Grove / forest | 10 | 82 | Soft sight block and cover inside. | Sim now |
| `:` | Field track | 5 | 64 | Field track. Light speed bonus. | Sim now |
| `f` | Fence | 3 | 64 | Hides, gives no cover. | Sim now |
| `B` | Forest belt | 4 | 60 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `u` | Sunflower field | 1 | 60 | Hides who is inside, gives no cover. | Sim now |
| `=` | Road | 2 | 37 | Fast route for vehicles and infantry. | Sim now |
| `S` | Bush | 13 | 22 | Hides, gives no cover. | Sim now |
| `h` | House | 17 | 21 | Blocks sight and movement. One defender can stand inside. | Sim now |
| `c` | Crater | 13 | 14 | Decoration. Marks past fights. | Sim now |
| `T` | Trench | 1 | 13 | Defender cover. The player can build more. | Sim now |
| `W` | Wreck / vehicle | 9 | 9 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `a` | Hay / straw stack | 3 | 5 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `v` | Power-line pole | 4 | 4 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `r` | Ruined house | 4 | 4 | Cover. Infantry climb through slowly. | Sim now |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 3 | 3 | Decoration. Landmark. | Decoration only |
| `M` | Spoil heap | 3 | 3 | Rock-like cover. | Sim now |
| `z` | Camouflage net | 2 | 2 | Hides what is under it. No cover. | NEW Sim row |
| `d` | Dugout | 2 | 2 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `i` | Silo / tower / windmill | 1 | 1 | Landmark. Blocks sight in a thin line. Can hold a spotter. | NEW Sim row |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |

**Empty ground:** 58 of 800 cells = 7% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 34% of the map. They are decoration and the open cells of the field between objects.

## Life

- Chimney smoke from some houses. Lamps in windows at night.
- A cow or goat (decor) in a garden; a dog runs away at the first shot.
- A burning house (ruin) smokes.
- Washing on a line moves in the wind. Birds rise from the groves.

## Winter and night

- **Winter:** Snow on the roofs and in the gardens. Footprints show infantry routes.
- **Night:** Windows glow in some houses. A burning ruin lights the street.
