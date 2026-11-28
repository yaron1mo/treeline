# Designed maps — report (issue #46)

Branch `mistral/map-generator`. The 22 approved concept sheets of
`docs/design/maps/*.md` (PR #51, Yaroslav's decisions of 2026-11-28) are the
target: `tools/maps/grammar.py` converts each sheet's 20 x 40 grid (one char =
10 m, row 1 = enemy edge, row 40 = our line) straight into
`unity/Assets/Resources/Fields/<name>.json`, so a field file cannot drift from
its sheet. Same sheet + same seed -> same map (asserted in `generate.py`).
Categories: A river x5, B hills and slopes x5, C forest-heavy x5, D open steppe
x5, V village x2 (sheets 21-22, added by #51). No map is "parallel belts across
the field"; the one single-lane map is Forest road (C12), where the one road
through the forest is the idea.

Previews: one PNG per map + contact sheet (Forest belts first, grouped A-V) in
`docs/design/visual/maps/`.

## Generator parameters that adapt the sheets to the Sim (not Sim rules)

- `resolve_markers`: the digits 1-9 on a sheet mark defender functions; the cell
  keeps the terrain it stands on (ford/river stays water, a digit in a grove
  stays grove, else open ground).
- `heal_roads`: the Sim's vehicle clearance rule blocks any vehicle cell that
  touches an impassable one, and only road/ford cells are exempt. So a road
  cuts through whatever lies on its own corridor (short gaps between road cells
  in a row/column are bridged, road ends extend to the map edge) — a ramp over
  an embankment, a gap in a wreck line, a clearing in a belt. Water, open field,
  buildings, stacks and sunflower are never overwritten.
- `span_fords`: a ford column is extended through the river cells above/below
  it, plus one cleared apron cell at each mouth, so vehicles can actually drive
  across (ford cells are exempt from clearance, river cells are not).
- `bank_roads`: cells next to a river are blocked for vehicles, so on the bank
  rows the road and ford columns are joined by a road; otherwise the ford is a
  dead-end pocket between river and the next obstacle line.
- A ruined bridge (`Q`) stands in the river: for the terrain those cells are
  water (the `bridge_ruins` line still draws the rubble), so vehicles cannot
  drive through the gap under the rubble — only the fords.

## Local checks (`tools/maps/check.py`, port of the Sim's NavGrid/FlowField/Routes)

Lanes = where traced infantry/vehicle routes cross the placing zone (y 240-360),
grouped within 25 m, at most 3 — the same lanes the Balance run's scripted player
sees. Reach: infantry, transports (plain vehicle grid) and armour
(`tree_move_cost` 5 — tanks push through groves/belts) from the entries
(explicit spawns, else the Sim's 15 default entries over the top edge).

| # | Map | Cat | Idea (one line) | Lanes (placing zone) | Reach inf / veh / arm |
|---|---|---|---|---|---|
| A1 | Ruined bridge | A | River in two; broken bridge, one far ford, a pipe for infantry | [91] | 13/15, 7/15, 15/15 |
| A2 | River bend | A | River bends into the field; attack along the west bank | [61, 93] | 13/15, 11/15, 11/15 |
| A3 | Confluence | A | Tributary joins in a Y; attack splits into west and east lanes | [41, 129] | 13/15, 9/15, 14/15 |
| A4 | Overlook | A | One ford; our hill stands right above it | [101] | 13/15, 5/15, 15/15 |
| A5 | Enemy heights | A | Enemy owns the hill on its bank and sees our line | [131, 69] | 13/15, 13/15, 13/15 |
| B6 | Left hill | B | Big hill on the left flank; whoever holds it sees half the map | [121] | 13/15, 13/15, 13/15 |
| B7 | Ridge | B | Ridge across the field; the attack goes through two saddles | [69] | 12/15, 10/15, 13/15 |
| B8 | Valley | B | Two hills with a valley between; crossfire from both | [99] | 13/15, 5/15, 15/15 |
| B9 | Long slope | B | Ground slopes down to our line; the enemy starts high | [99] | 13/15, 13/15, 13/15 |
| B10 | Gully mouth | B | Gully along the advance ends at our hill; hidden infantry route | [151, 43] | 12/15, 10/15, 10/15 |
| C11 | Clearings | C | Big forest with clearings; fights at the clearing edges | [91] | 13/15, 0/15, 15/15 |
| C12 | Forest road | C | One wide road through dark forest; the ambush map, one lane | [111] | 13/15, 1/15, 15/15 |
| C13 | Forest and steppe | C | Forest left, open steppe right, a road and farm between | [91] | 13/15, 7/15, 14/15 |
| C14 | Grove islands | C | Eleven groves of different size stand in the field like islands | [91] | 13/15, 13/15, 13/15 |
| C15 | Burned edge | C | Burned strip in front of our forest edge: looks like cover, is none | [101] | 13/15, 5/15, 15/15 |
| D16 | Open steppe | D | Almost no trees; bushes, heaps, wrecks, craters; long sight lines | [149] | 13/15, 13/15, 13/15 |
| D17 | Sunflower | D | A big sunflower field: hides who is inside, gives no cover | [51, 151] | 13/15, 10/15, 13/15 |
| D18 | Old trenches | D | Three old trench lines with wire cross the field | [101] | 13/15, 13/15, 15/15 |
| D19 | Wreck field | D | A destroyed column of vehicles across the field; the only cover | [101] | 13/15, 7/15, 12/15 |
| D20 | Farmstead | D | Farm with silo, sheds and a combine north of a thin tree line | [99] | 13/15, 6/15, 6/15 |
| V21 | Village street | V | Long village street from the enemy edge to our line | [99] | 13/15, 13/15, 13/15 |
| V22 | Crossroads | V | Two streets cross; the houses at the crossroads control both | [101] | 13/15, 11/15, 11/15 |

Forest belts (reference) gives lanes [45, 159]. Notes: on Clearings the field
tracks are decoration in the Sim (no move-cost row), so transports have no way
in — only infantry and tanks (tree_move_cost 5) attack there, which matches the
sheet's "through the forest: infantry only". Forest road has exactly one vehicle
entry (the road) — the one-lane map by design. With a single road, the plain flow
field funnels every traced route onto that road, so several maps show 1 lane
here; in a real battle attackers also route around known defenders
(`RouteFor` danger fields), so the lanes they take are wider — the playtest
below shows the maps hold as many waves in a row as Forest belts.

## Balance run (step 3)

The branch pushes run the `Balance run` on the maps of `tools/balance/run.json`
(current 20 waves, max budget 12000, 10 playtest seeds x saver/greedy/reactive).
Run 46095181120 (before the bridge fix; the fix only closes a vehicle gap at the
ruined bridge): https://github.com/yaron1mo/treeline/actions/runs/46095181120

Median wave cost vs Forest belts (the brief's 0.6-1.6 band): every map is inside
it — Burned edge 0.80, Clearings 0.93, Confluence 1.00, Crossroads 0.67,
Enemy heights 1.00, Farmstead 1.08, Forest and steppe 0.76, Forest road 0.91,
Grove islands 1.07, Gully mouth 1.30, Left hill 1.00, Long slope 1.00,
Old trenches 1.26, Open steppe 1.00, Overlook 0.96, Ridge 1.13, River bend 1.05,
Ruined bridge 1.00, Sunflower 1.15, Valley 0.70, Village street 0.82,
Wreck field 1.00.

Scripted players, 20 waves in a row (mean / best of 10 seeds): all maps hold
8.2-10.6 waves — the same band as Forest belts (8.8-8.9) and River crossing
(9-10). No map is an outlier.

Waves "not held" at max budget 12000 (cost = NaN):

| Map | Not-held waves | Note |
|---|---|---|
| Clearings, Gully mouth, Open steppe, Overlook, Ridge, Sunflower, Village street | 14 | wave 14 = 4 regular + 1 helicopter (new); the gate field itself is out of target on it (ratio 0.76, "too hard") |
| Confluence | 14, 18, 19, 20 | plus the heaviest mixed waves |
| Valley | 18, 19, 20 | same |
| Enemy heights | 14, 19 | |
| Left hill | 20 | |

The current `waves.csv` is mid-rebalance (the run's gate verdict is FAIL: only
2 of 20 waves in target on Forest belts), and wave 14's helicopter is the common
failure — it is not held even where it is held on other maps at 1.5-3.5x the
Forest belts cost. The maps that fail the heavy waves 18-20 (Confluence, Valley)
are the maps whose idea splits the attack into two lanes plus wading, i.e. the
defence must cover more ground. Fixing these would mean changing the approved
sheets (more covered hold points) or retuning the waves — a design decision, not
a generator parameter; flagged for Yaroslav and Claude.

Second run 46097822807 (after the bridge fix,
https://github.com/yaron1mo/treeline/actions/runs/46097822807) gives the same
picture: median ratios unchanged (Ruined bridge 0.94, now 0 not-held waves),
the same waves not held (14 on nine maps; 18-20 on Confluence and Valley; 19 on
Enemy heights; 20 on Left hill), playtest means 8.2-10.2 waves held per map vs
Forest belts 8.9.

## Sim rules / rows the maps need

Already in `unity/Assets/Data/obstacles.csv` (2026-11-27/28, Claude): `house`,
`house_inside`, `ruin`, `fence`, `sunflower`, `burned_belt`, `stack`, `shed`,
`silo`, `camo_net`, `dugout`. The two rules the original brief asked for are
therefore already rows: **burned_belt** (looks like a belt, hides almost nothing,
no cover — Burned edge) and **sunflower** (hides who is inside, no cover —
Sunflower and others).

Still missing in the Sim (rows exist, behaviour does not):

- `stack` burns when hit (smoke) — all farm maps.
- `silo` spotter on top — Enemy heights and 13 other sheets.
- `camo_net` player-built — later.
- `tracks` have no Sim row (decoration only): the sheets call field tracks
  "light speed bonus", but in the Sim a track is just open ground. Either add a
  `tracks` row with a small move-cost bonus or accept tracks as pure decor.
- The sheets' decoration chars (`v` power-line pole, `*` well/hive/scarecrow/
  bus stop) have no field feature and are not written to the JSON; they are for
  Codex's render layer (with the "Life" animations below).
- `bridge_ruins` is render-only in the Sim: the river itself blocks vehicles and
  infantry wades at cost 4, so a ruined bridge works as drawn, but the rubble
  does not slow infantry extra.

## For Codex: animations from the sheets' "Life" sections

Moving tractor/combine that flees at the first shot (Farmstead, Grove islands,
Forest and steppe, Open steppe, River bend, Sunflower); burning wrecks/stacks
with animated smoke (Ruined bridge, Wreck field, Burned edge, Enemy heights,
Long slope, Valley, Old trenches, Forest road, Crossroads); animated water,
fish/ducks at the fords (all river maps); birds rising from groves, clearings,
sunflowers or the gully on shots or entry; sheep/goats/cows as decor that flee;
chimney smoke and window lamps at night (villages); wind in grass, washing and
flags; mist in the gully and valley (Gully mouth, Valley); dust on the road when
vehicles move (Ridge); crows on wire and wrecks; light shafts through the forest
(Clearings, Forest road); sunflower heads following the sun (Sunflower);
power-line poles with fallen wires.
