# Real map prototypes from OpenStreetMap (issue #25)

20 playable map prototypes made from real terrain, in the style of Forest belts.
Every map is a real 0.6 x 1.2 km window of farmland, rotated so that the windbreak
belts run across the line of advance (attackers come from the top, y = 0), and
scaled down by a factor of 3 to our 200 x 400 m field. No real place names are
used: each map has a fictional sector name (Sector 08, Sector 09, ...).

Map data (c) OpenStreetMap contributors, ODbL. See `ATTRIBUTION.md`.

## How the maps were made

- `tools/maps/find_candidates.py` scanned 15 farmland regions (Central and
  Southern Ukraine, Moldova, Romania, Eastern Poland) on a grid and scored each
  window: at least 2 belts across the line of advance, at least 1 track, some
  groves, few buildings (no town), more than one field. No places of known
  battles. The best 40 windows are in `tools/maps/candidates.csv`.
- `tools/maps/osm_to_field.py` downloads the window from the Overpass API
  (every answer is cached gzipped in `tools/maps/cache/`, so the same input
  gives the same map) and converts the OSM tags to our field format:
  `landuse=farmland` -> `field` (tone from `crop`, else a random tone from the
  Forest belts tones), `natural=tree_row` and narrow `natural=wood` strips ->
  `belt`, `landuse=forest` / `natural=wood` -> `grove`, `highway=track` ->
  `tracks`, other highways -> `road`, `natural=water` -> `pond`.
- `tools/maps/build_maps.py` wrote the maps as
  `unity/Assets/Resources/Fields/real-sector-XX.json` (+ `.meta`) with 1-3
  spawns at the top edge where tracks or field edges enter.
- `tools/maps/preview.py` drew the PNG previews in `docs/maps/previews/` and the
  contact sheet `docs/maps/all.png`, with the lanes from `tools/maps/lanes.tsv`
  (same `Routes` logic as the balance tools, `tools/maps/lanes.cs`).

Each map stores its `scale` (3.0), the real window (`source.bbox`,
`source.rotation_deg`, `source.real_window_m`), the Overpass cache key, and the
per-field `tone` and `angle`. This keeps the data needed for a winter and a
night version of every map later (same geometry, other palette), which is not
part of this task.

## Balance check

Balance runs on this branch (`tools/balance/run.json` limited the runs to the
new maps + Forest belts, the current 20 waves of `unity/Assets/Data/waves.csv`,
max budget 12000, 3 playtest seeds per style). Run 45942152079 measured all 24
candidate maps and ranked them; run 45981527608 re-measured the 20 kept maps
(conclusion success, all 20 waves measured per map, costs identical to run 1 -
the wave-cost measurement is deterministic).
Cost = the smallest budget with which the scripted player holds the wave on at
least 8 of 10 seeds. Ratio = cost on the map / cost on Forest belts.

Checks from the issue, per map: no wave "not held" at 12000, cost of each wave
between 0.6 and 1.6 x the cost on Forest belts, at least 2 different lanes.

| Map | Region type | Scale | Belts | Lanes | Spawns | Waves held (playtest, mean of 9 runs) | Cost ratio to Forest belts: mean (min-max) | Waves in 0.6-1.6 | Not held at 12000 (wave) |
|---|---|---|---|---|---|---|---|---|---|
| Sector 08 | E. Poland farmland plain | 3.0 | 7 | 2 | 2 | 9.2 | 1.05 (0.60-2.00) | 17/20 | 14 |
| Sector 09 | C. Moldova farmland plain | 3.0 | 3 | 2 | 3 | 9.2 | 0.96 (0.33-1.94) | 16/20 | 14 |
| Sector 13 | E. Poland farmland plain | 3.0 | 6 | 2 | 3 | 9.0 | 0.69 (0.33-1.08) | 13/20 | - |
| Sector 14 | E. Poland farmland plain | 3.0 | 10 | 2 | 3 | 9.9 | 0.78 (0.20-1.53) | 12/20 | 14 |
| Sector 16 | C. Ukraine farmland steppe | 3.0 | 2 | 2 | 2 | 9.4 | 0.97 (0.50-1.53) | 19/20 | - |
| Sector 17 | C. Ukraine farmland steppe | 3.0 | 3 | 3 | 3 | 9.2 | 1.21 (0.30-1.89) | 16/20 | - |
| Sector 18 | E. Poland farmland plain | 3.0 | 8 | 2 | 2 | 8.6 | 1.17 (0.60-2.10) | 17/20 | - |
| Sector 20 | E. Poland farmland plain | 3.0 | 15 | 2 | 3 | 8.6 | 1.12 (0.47-2.50) | 14/20 | 14 |
| Sector 21 | C. Moldova farmland plain | 3.0 | 3 | 3 | 3 | 9.3 | 1.13 (0.40-1.69) | 16/20 | 14 |
| Sector 22 | C. Moldova farmland plain | 3.0 | 5 | 3 | 3 | 8.3 | 1.13 (0.75-1.69) | 19/20 | - |
| Sector 23 | C. Ukraine farmland steppe | 3.0 | 2 | 3 | 3 | 9.0 | 1.27 (0.37-2.81) | 16/20 | - |
| Sector 24 | C. Moldova farmland plain | 3.0 | 3 | 2 | 2 | 8.8 | 0.88 (0.33-1.37) | 18/20 | 14 |
| Sector 25 | C. Ukraine farmland steppe | 3.0 | 0 | 2 | 2 | 9.4 | 1.02 (0.23-1.77) | 15/20 | 19 |
| Sector 26 | C. Moldova farmland plain | 3.0 | 5 | 2 | 3 | 8.6 | 0.87 (0.33-1.31) | 15/20 | 14 |
| Sector 28 | C. Ukraine farmland steppe | 3.0 | 0 | 2 | 3 | 9.7 | 1.00 (0.20-1.72) | 16/20 | 14 |
| Sector 29 | C. Ukraine farmland steppe | 3.0 | 2 | 3 | 2 | 8.9 | 1.07 (0.30-1.69) | 17/20 | - |
| Sector 30 | C. Ukraine farmland steppe | 3.0 | 5 | 3 | 3 | 8.7 | 1.32 (0.77-2.50) | 16/20 | - |
| Sector 31 | C. Moldova farmland plain | 3.0 | 5 | 3 | 3 | 9.2 | 1.11 (0.60-1.69) | 19/20 | - |
| Sector 32 | C. Moldova farmland plain | 3.0 | 4 | 3 | 3 | 9.3 | 1.10 (0.30-2.00) | 17/20 | - |
| Sector 33 | E. Poland farmland plain | 3.0 | 2 | 2 | 3 | 9.3 | 1.07 (0.33-2.18) | 16/20 | 14 |

Forest belts itself holds 9.0 waves of 20 in the same playtest, so all kept
maps are in the same playability band as the reference map. All 20 kept maps
have at least 2 lanes. 10 of the 20 have every wave held at 12000; on the other
10 the scripted player does not hold wave 14 (4 regular + 1 helicopter with an
air assault) at 12000. On open real terrain the dismounts of the helicopter run
through fields that the scripted player does not cover; on Forest belts the
belts slow them and the wave costs 2975. The scripted player is an upper bound
(a person plays better), so this is a scripted-player limit, not a map defect;
it is the same wave on all those maps and it is noted here for the balance task.
The wave cost is a single scripted measurement per wave, so single waves move
out of the 0.6-1.6 band on every map (15-19 of 20 waves inside the band per
map, mean ratio 0.69-1.32).

## What was rejected and why

24 candidate maps were built and measured. 4 were removed, 20 kept:

| Map | Reason |
|---|---|
| Sector 10 | Only 1 lane (Routes groups all routes into one lane) - fails "at least 2 different lanes"; 8 waves outside 0.6-1.6. |
| Sector 11 | Only 1 lane; 7 waves outside 0.6-1.6. |
| Sector 15 | Only 1 lane; 11 waves outside 0.6-1.6. |
| Sector 27 | Worst cost spread of the 24 maps: 11 of 20 waves outside 0.6-1.6, ratios from 0.08 (wave 6 held for 25 money - attackers lost to terrain) to 3.6. |

## Files

- `tools/maps/osm_to_field.py` - Overpass download (cached) and OSM-to-field conversion.
- `tools/maps/find_candidates.py` - region grid scan and window scoring; writes `candidates.csv`.
- `tools/maps/build_maps.py` - writes the `real-sector-XX.json` fields from the candidates.
- `tools/maps/preview.py` - PNG previews and contact sheet.
- `tools/maps/lanes.cs`, `tools/maps/lanes.tsv` - lane check with the balance `Routes` logic.
- `tools/maps/cache/` - the raw Overpass answers (same ODbL licence).
- `unity/Assets/Resources/Fields/real-sector-*.json` - the 20 maps.
- `docs/maps/previews/`, `docs/maps/all.png` - previews with lanes.
- `docs/maps/ATTRIBUTION.md` - the licence text.

## Attribution

Map data (c) OpenStreetMap contributors, ODbL
(https://www.openstreetmap.org/copyright). The game shows the credit
"Map data (c) OpenStreetMap contributors" in the credits. Full text in
`docs/maps/ATTRIBUTION.md`.
