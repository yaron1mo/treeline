# 7. Ridge

Category B · Difficulty: Medium · Fits: Campaign: chapter 2. Endless: 10 waves.

**Idea.** A ridge crosses the field. The attack must go through two saddles. Our units on the ridge shoot into the saddles.

## Defender functions

1. Left ridge top: sees the left saddle from the side.
2. Centre ridge top: sees both saddles. The key point.
3. Right ridge top: sees the right saddle and the track.
4. Left saddle (road): vehicles and infantry. Wire and hedgehogs make a kill zone.
5. Embankment and trench: second line behind the ridge, out of sight of the enemy.
6. Right saddle (track): lighter traffic, slow, hidden by the belt.

The numbers 1–6 on the layout show where each function is.

## Attacker routes

1. Left saddle with road: main route. Fast.
2. Right saddle with track: flank. Slower. The belt hides it from the ridge.
3. Over the ridge: infantry only, slow, under fire from 3 hills.

## Layout

20 columns × 40 rows, one character = 10 m (map 200 m × 400 m). Row 1 (top) is the enemy edge. Row 40 (bottom) is our line.

```
GGGG,,=S,c,Sc:,OS,c,
GGGG,,=Sc,SSO:,,,,,c
GGGG,,=S,,v,S:,,,,,,
GGGG,,=M,,,,,:,,S,,O
GGGGOS=,,,,,,:,,,M,,
GGGG,S=,SS,,,:,,,OM,
,,,,,,=,S,v,BBBBBBBB
,aa,,,=c,S,,BBBBBBBB
,,,O,,=,,M,,BBBBBBBB
aOaaS,=M,,,,BBBBBBBB
,,,,,W=a,,v,,:,c,,,,
O,,,,a=a,,c,c:M,sa,,
,,,,,,=,S,,,,:MWa,,,
,OS,,,=,,,,S,:,,,,,,
c,S,,,=,,*vSS:,S,,,,
,,S,S,=SSM,S,:S,,O,,
,c,S,M=M,,,SM:MS,,,,
HHHHHa=SHHHH,:,HHHHH
HHHHH,=,HHvH,:,HHHHH
HHd1Ha=,HH2HO:,Hd3HH
HHHHH,4,HHHH,:,HHHHH
HHHHH,=,HHHH,:,HHHHH
HHHHHS=,HHvH,:,HHHHH
,,,,,,O,,c,,,O,M,cO,
uuuu.S=....S.6S.ccO.
uuuuS.=c.cS.S:.S....
uuuuc.WSS.v.S:.GGGGG
uuuu..=..a.SS:.GGGGG
uuuu.S=SM..c.:.GGGGG
uuuu..=......:.GGGGG
uuuEEEEEE5vEEEEGGGGG
,,,,,,=,d,,,,:,Oc,,,
,c,,S,,=,SS,,,:,,O,,
,,,,,,,=,,,,S,:,O,,,
,*STTTTTTTvTTTTTT,,,
,,,,,SS=,aac,S:,S,,,
BBBBB,S=S,c,c,BBBBBB
BBBBBS,=ffff,,BBBBBB
BBBBB,c=ffff,,BBBBBB
,,,,,,,=,O,,,,:,,,,,
```

Legend: `,` Crop rows / stubble; `H` Hill; `B` Forest belt; `S` Bush; `G` Grove / forest; `.` Empty ground; `=` Road; `:` Field track; `u` Sunflower field; `c` Crater; `O` Rock; `a` Hay / straw stack; `M` Spoil heap; `T` Trench; `E` Embankment; `v` Power-line pole; `f` Fence; `W` Wreck / vehicle; `d` Dugout; `*` Small decor (well, hive, scarecrow, bus stop); `s` Shed; `1`–`9` defender function. Full legend: `README.md`.

## Fill list

| Char | Object | Count | Cells | Game role | Sim |
|---|---|---|---|---|---|
| `,` | Crop rows / stubble | 22 | 290 | Decoration. Crop rows show the field direction. No cover. | Sim now |
| `H` | Hill | 3 | 80 | High ground: more range and sight, enemy direct fire less accurate. Cover behind it. | Sim now |
| `B` | Forest belt | 3 | 65 | Soft sight block and cover inside. Slows infantry. | Sim now |
| `S` | Bush | 28 | 60 | Hides, gives no cover. | Sim now |
| `G` | Grove / forest | 2 | 49 | Soft sight block and cover inside. | Sim now |
| `=` | Road | 5 | 36 | Fast route for vehicles and infantry. | Sim now |
| `:` | Field track | 6 | 30 | Field track. Light speed bonus. | Sim now |
| `u` | Sunflower field | 1 | 27 | Hides who is inside, gives no cover. | Sim now |
| `c` | Crater | 19 | 25 | Decoration. Marks past fights. | Sim now |
| `O` | Rock | 17 | 19 | Rock-like cover. | Sim now |
| `a` | Hay / straw stack | 10 | 15 | Cover (like a heap). Burns when hit: smoke. | NEW Sim row |
| `M` | Spoil heap | 12 | 14 | Rock-like cover. | Sim now |
| `T` | Trench | 2 | 13 | Defender cover. The player can build more. | Sim now |
| `E` | Embankment | 2 | 11 | Cover line. Blocks vehicles. Infantry climb slowly. | Sim now |
| `v` | Power-line pole | 9 | 9 | Decoration. Landmark. Fallen wires near it. | Decoration only |
| `f` | Fence | 1 | 8 | Hides, gives no cover. | Sim now |
| `W` | Wreck / vehicle | 3 | 3 | Rock-like cover. Some wrecks burn (smoke). | Sim now |
| `d` | Dugout | 3 | 3 | Roofed trench: strong cover against indirect fire and aircraft. | NEW Sim row |
| `*` | Small decor (well, hive, scarecrow, bus stop) | 2 | 2 | Decoration. Landmark. | Decoration only |
| `s` | Shed | 1 | 1 | Small house: blocks sight and movement. | NEW Sim row |

**Empty ground:** 40 of 800 cells = 5% (limit 20%). It is deliberate: a clear kill zone in front of our line, where the defender needs a clear line of sight. Crop rows (`,`) fill 36% of the map. They are decoration and the open cells of the field between objects.

## Life

- Dust rises on the road when vehicles move.
- A tractor with a plough stops in the right saddle and burns.
- Birds fly along the ridge.
- Poles with a fallen wire in the saddle.

## Winter and night

- **Winter:** Snow on the ridge: the saddles get icy and slow vehicles.
- **Night:** The ridge is a black line; the saddles show as lighter gaps.
