# Treeline

A maze-building tower defence on the open steppe. Attackers take the cheapest route across real terrain and learn your kill zones, two waves late.

## Play the test build

Open `index.html` in any browser. Pick a field, drag on it to draw terrain, and send a wave.

## Fields

`fields/` holds the field files. The three test fields (200 × 400 m) are hand-built from typical eastern Ukrainian terrain:

| Field | Terrain |
| --- | --- |
| Forest belts | Farmland cut by four windbreak belts across the line of advance |
| Balka and railway | A wooded ravine and a rail embankment with one level crossing |
| River crossing | A meandering river, floodplain woods, one ford, a slag heap |

## Make a field from a real place

```
python3 tools/osm_to_field.py 48.2915 37.1720 --enemy-from east --name "Belts near X"
```

This downloads OpenStreetMap data for a 200 × 400 m box around the point (change it with `--size`) and writes `fields/<name>.json`. Load it in the game with **Load field file**. Standard-library Python 3.8+, no installs. Map data © OpenStreetMap contributors (ODbL).

## Rules

`rules/attackers.json` holds every attacker number (HP, armour, damage, accuracy, range, speed, adaptation, special traits) plus the armour and cover tables. Check it and print the roster table with:

```
python3 tools/check_rules.py --table
```

## Engine notes

- Terrain is vector shapes (polygons and lines with width) in metres.
- Pathfinding runs on a hidden 2 m grid (50 × 100 cells), one flow field per unit class, rebuilt when terrain changes.
- Units steer freely along the flow field, keep their own lane and spread out.

## Plan

1. Terrain and pathfinding on free structure (done)
2. Obstacles and lagged danger memory
3. Weapons and the full attacker roster
4. Waves, budget, end screens
5. Python balance simulator, then playtests

Everything is tested in the browser first; the finished game ships to iOS, Google Play and web together.
