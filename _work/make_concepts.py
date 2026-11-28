import json, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))

# no, file, name, category, sector, subtitle (one line from the sheet index), expect_lanes
MAPS = [
    (1,  "ruined-bridge",       "Ruined bridge",      "A", "Sector 41",       "A river cuts the field in two. The bridge is broken, so vehicles must queue at one ford far on the side.", 3),
    (2,  "river-bend",          "River bend",         "A", "Sector 42",       "The river bends deep into the field. The attack runs along the west bank.", 3),
    (3,  "confluence",          "Confluence",         "A", "Sector 43",       "A tributary meets the main river in a Y. The attack splits into a west lane and an east lane.", 3),
    (4,  "overlook",            "Overlook",           "A", "Sector 44",       "One ford, and our hill stands right above it.", 3),
    (5,  "enemy-heights",       "Enemy heights",      "A", "Sector 45",       "The enemy owns the hill on its own bank and sees our line.", 3),
    (6,  "left-hill",           "Left hill",          "B", "Sector 46",       "A big hill covers the left flank. Whoever holds it sees half the map.", 3),
    (7,  "ridge",               "Ridge",              "B", "Sector 47",       "A ridge crosses the field. The attack must go through two saddles.", 3),
    (8,  "valley",              "Valley",             "B", "Sector 48",       "Two hills with a valley between them. The attack goes through the valley.", 3),
    (9,  "long-slope",          "Long slope",         "B", "Sector 49",       "The ground slopes down to our line. The enemy starts high and sees us.", 3),
    (10, "gully-mouth",         "Gully mouth",        "B", "Sector 50",       "A gully runs along the line of advance and ends in front of our hill.", 3),
    (11, "clearings",           "Clearings",          "C", "Sector 51",       "A big forest with clearings. Sight is short.", 3),
    (12, "forest-road",         "Forest road",        "C", "Sector 52",       "One wide road runs through a dark forest. This is the map of ambushes: one lane by design.", 1),
    (13, "forest-and-steppe",   "Forest and steppe",  "C", "Sector 61",       "Forest on the left, open steppe on the right, a road and a farm between.", 3),
    (14, "grove-islands",       "Grove islands",      "C", "Sector 54",       "Eleven groves of different size stand in the field like islands.", 3),
    (15, "burned-edge",         "Burned edge",        "C", "Sector 55",       "In front of our forest edge is a burned strip that looks like trees but hides nothing and gives no cover.", 3),
    (16, "open-steppe",         "Open steppe",        "D", "Sector 56",       "Almost no trees. Long sight lines; bushes, heaps, wrecks and craters only.", 3),
    (17, "sunflower",           "Sunflower",          "D", "Sector 57",       "A big sunflower field fills the middle of the map: it hides but gives no cover.", 3),
    (18, "old-trenches",        "Old trenches",       "D", "Sector 58",       "Three old trench lines with wire cross the field.", 3),
    (19, "wreck-field",         "Wreck field",        "D", "Sector 59",       "A destroyed column of vehicles lies in a diagonal line across the field.", 3),
    (20, "farmstead",           "Farmstead",          "D", "Sector 60",       "A farm with a silo, sheds and a combine stands north of a thin tree line.", 3),
    (21, "village-street",      "Village street",     "V", "village-street",  "A long village street from the enemy edge to our line.", 3),
    (22, "village-crossroads",  "Crossroads",         "V", "village-crossroads", "Two village streets cross in the middle. Whoever holds the houses at the crossroads controls both routes.", 4),
]

out = {
    "brief": ("Issue #46, brief of 2026-11-27, reworked 2026-11-28: the 22 approved concept sheets of "
              "docs/design/maps/*.md (PR #51) are the target. Each map below points at its sheet; the "
              "generator builds the field straight from the sheet grid (same sheet + same seed -> same "
              "field file). Categories: A river, B hills and slopes, C forest-heavy, D open steppe, "
              "V village (sheets 21-22, added by #51)."),
    "maps": [],
}
for no, file, name, cat, sector, subtitle, lanes in MAPS:
    out["maps"].append({
        "no": no,
        "file": file,
        "name": name,
        "category": cat,
        "sheet": f"docs/design/maps/{file}.md",
        "seed": no,
        "sector": sector,
        "subtitle": subtitle,
        "expect_lanes": lanes,
    })

path = os.path.join(ROOT, "tools", "maps", "concepts.json")
text = json.dumps(out, indent=1, ensure_ascii=False).replace("\n", "\r\n")
with open(path, "w", encoding="utf-8", newline="") as fh:
    fh.write(text + "\r\n")
print("wrote", path, len(out["maps"]), "maps")
