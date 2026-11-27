#!/usr/bin/env python3
"""Two village prototype maps (Claude, 2026-11-27, Yaroslav: Ukrainian rural streets, one-storey houses).

Houses block sight and movement; one defender can stand inside a house (Sim: house, house_inside). Gardens (grove)
and fences make the side routes slow for infantry and closed for vehicles, so vehicles keep to the streets: fewer
routes than an open field, closer to a classic tower defence.
Usage: make_villages.py <repo root>
"""
import json, os, random, sys, uuid

root = sys.argv[1]
out_dir = os.path.join(root, "unity/Assets/Resources/Fields")


def rect(x, y, w, h):
    return [[round(x, 1), round(y, 1)], [round(x + w, 1), round(y, 1)], [round(x + w, 1), round(y + h, 1)], [round(x, 1), round(y + h, 1)]]


def field(x, y, w, h, tone, angle):
    return {"type": "field", "poly": rect(x, y, w, h), "tone": tone, "angle": angle}


def houses_along(feats, rng, x_street, side, y0, y1, gap_every=3):
    """A row of plots along one side of a street: house near the street, fence, garden behind."""
    y = y0
    n = 0
    while y + 14 <= y1:
        n += 1
        if n % gap_every == 0:      # a lane between plots: a way for infantry
            y += 8
            continue
        w, h = rng.uniform(8, 10), rng.uniform(7, 9)
        hx = x_street + 9 if side > 0 else x_street - 9 - w
        feats.append({"type": "ruin" if rng.random() < 0.12 else "house", "poly": rect(hx, y + 2, w, h)})
        # fence along the back of the plot and the garden behind it
        fx = x_street + 22 if side > 0 else x_street - 22
        feats.append({"type": "fence", "line": [[fx, y], [fx, y + 14]], "width": 1.5})
        gx = x_street + 24 if side > 0 else x_street - 24 - 22
        if rng.random() < 0.7:
            feats.append({"type": "grove", "poly": rect(gx, y + 1, 22, 12)})
        y += 16
    return feats


def write(name, title, subtitle, feats, spawns):
    data = {"name": title, "sector": name, "size": [200, 400], "spawns": spawns, "subtitle": subtitle, "features": feats}
    path = os.path.join(out_dir, name + ".json")
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(data, fh, ensure_ascii=False, indent=1)
    meta = path + ".meta"
    if not os.path.exists(meta):
        with open(meta, "w") as fh:
            fh.write(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    print("wrote", path, len(feats), "features")


# Map 1: Village street — one long street from the enemy edge to our line, plots on both sides.
rng = random.Random(11)
f = [field(0, 0, 200, 400, "stubble", 90), field(0, 0, 60, 400, "plowed", 0), field(140, 0, 60, 400, "sunflower", 0)]
f.append({"type": "road", "line": [[100, -5], [102, 150], [98, 260], [100, 405]], "width": 8})
houses_along(f, rng, 100, +1, 40, 330)
houses_along(f, rng, 100, -1, 48, 330)
f.append({"type": "tracks", "line": [[20, -5], [25, 120], [40, 220], [70, 345]]})
f.append({"type": "tracks", "line": [[180, -5], [175, 140], [160, 250], [130, 345]]})
f.append({"type": "sunflower", "poly": rect(150, 60, 50, 150)})
f.append({"type": "belt", "line": [[-6, 30], [70, 28]], "width": 12})
f.append({"type": "belt", "line": [[130, 30], [206, 32]], "width": 12})
f.append({"type": "wreck", "poly": rect(96, 205, 7, 4)})
f.append({"type": "craters", "points": [[round(rng.uniform(10, 190), 1), round(rng.uniform(20, 350), 1), round(rng.uniform(1.0, 2.0), 1)] for _ in range(25)]})
write("village-street", "Village street",
      "A Ukrainian village street from the enemy edge to our line. Vehicles keep to the street; infantry goes through the lanes and gardens. One defender per house.",
      f, [100, 25, 175])

# Map 2: Crossroads — a main street and a cross street; the centre is the key.
rng = random.Random(23)
f = [field(0, 0, 200, 400, "stubble", 80), field(0, 230, 200, 170, "plowed", 0)]
f.append({"type": "road", "line": [[100, -5], [100, 405]], "width": 8})
f.append({"type": "road", "line": [[-5, 200], [205, 196]], "width": 7})
houses_along(f, rng, 100, +1, 70, 185, gap_every=4)
houses_along(f, rng, 100, -1, 70, 185, gap_every=4)
houses_along(f, rng, 100, +1, 212, 320, gap_every=4)
houses_along(f, rng, 100, -1, 212, 320, gap_every=4)
f.append({"type": "ruin", "poly": rect(112, 186, 12, 8)})
f.append({"type": "grove", "poly": rect(5, 40, 40, 25)})
f.append({"type": "grove", "poly": rect(160, 20, 35, 30)})
f.append({"type": "pond", "poly": rect(20, 250, 18, 12)})
f.append({"type": "tracks", "line": [[40, -5], [45, 100], [20, 196]]})
f.append({"type": "tracks", "line": [[160, -5], [150, 110], [180, 197]]})
f.append({"type": "craters", "points": [[round(rng.uniform(10, 190), 1), round(rng.uniform(20, 360), 1), round(rng.uniform(1.0, 2.0), 1)] for _ in range(30)]})
write("village-crossroads", "Crossroads",
      "Two village streets cross in the middle. Whoever holds the houses at the crossroads controls both routes.",
      f, [100, 40, 160])
