#!/usr/bin/env python3
"""Generate the designed maps (issue #46) into unity/Assets/Resources/Fields/.

Reads tools/maps/concepts.json, builds each map with tools/maps/grammar.py and
writes <file>.json in the Forest belts field format, plus the Unity .meta file
with a new GUID only for a new map (an existing .meta is kept, so re-running
the generator does not change any GUID). Maps with a "sheet" key are built
from their approved concept sheet (docs/design/maps/*.md, PR #51); same seed
+ same parameters -> same map.

Usage: python3 tools/maps/generate.py [--check-only]
"""

import json
import os
import sys
import uuid

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
FIELDS_DIR = os.path.join(ROOT, "unity", "Assets", "Resources", "Fields")

sys.path.insert(0, HERE)
import grammar  # noqa: E402

# Feature types the Sim/render know: TerrainTable names, obstacle ids, and the
# render-only decorations (tracks, craters, bridge_ruins, field).
KNOWN_TYPES = {
    "field", "tracks", "craters", "grove", "pond", "belt", "road",
    "rock", "wreck", "bush", "heap", "embankment", "hill", "trench", "balka",
    "river", "ford", "pipe", "wire", "hedgehog", "bridge_ruins",
    "house", "ruin", "fence", "sunflower", "burned_belt",
    "stack", "shed", "silo", "camo_net", "dugout",
}


def validate(field, concept):
    w, h = field["size"]
    assert field["name"] == concept["name"]
    assert 0 < w <= 400 and 0 < h <= 800
    spawns = field.get("spawns", [])
    assert len(spawns) <= 3, "at most 3 spawns"
    for s in spawns:
        assert grammar.RULES["edge_margin"] <= s <= w - grammar.RULES["edge_margin"], s
    for f in field["features"]:
        assert f["type"] in KNOWN_TYPES, f["type"]
        pts = f.get("poly") or f.get("line") or f.get("points")
        assert pts, "feature without geometry"
        for p in (f.get("poly") or f.get("line") or []):
            assert len(p) == 2
            assert 0 <= p[0] <= w + 1e-6 and 0 <= p[1] <= h + 1e-6, (f["type"], p)
    if "sheet" in concept:
        # the sheet grid is the source: every non-decor cell must appear as a feature
        grid = field["_layout"]["grid"]
        cell = grammar.SHEET_CELL
        placed = set()
        for f in field["features"]:
            if f.get("poly"):
                xs = [p[0] for p in f["poly"]]
                ys = [p[1] for p in f["poly"]]
                for r in range(int(min(ys) // cell), int(max(ys) // cell) + 1):
                    for c in range(int(min(xs) // cell), int(max(xs) // cell) + 1):
                        placed.add((r, c))
            for p in (f.get("line") or []):
                placed.add((int(p[1] // cell), int(p[0] // cell)))
            for q in (f.get("points") or []):
                placed.add((int(q[1] // cell), int(q[0] // cell)))
        missing = []
        for r, row in enumerate(grid):
            for c, ch in enumerate(row):
                if ch in grammar.SHEET_DECOR:
                    continue
                if (r, c) not in placed:
                    missing.append((ch, r, c))
        assert not missing, f"sheet cells without a feature: {missing[:10]}"
    return True


def main():
    check_only = "--check-only" in sys.argv
    with open(os.path.join(HERE, "concepts.json"), encoding="utf-8") as fh:
        concepts = json.load(fh)["maps"]
    os.makedirs(FIELDS_DIR, exist_ok=True)
    written = []
    for concept in concepts:
        field = grammar.generate(concept)
        validate(field, concept)
        data = grammar.strip_layout(field)
        # same format as forest-belts.json: 1-space indent, CRLF
        text = json.dumps(data, indent=1).replace("\r\n", "\n")
        text = text.replace("\n", "\r\n")
        path = os.path.join(FIELDS_DIR, concept["file"] + ".json")
        meta = os.path.join(FIELDS_DIR, concept["file"] + ".json.meta")
        if not check_only:
            with open(path, "w", encoding="utf-8", newline="") as fh:
                fh.write(text)
            if not os.path.exists(meta):
                with open(meta, "w", encoding="utf-8", newline="") as fh:
                    fh.write("fileFormatVersion: 2\r\nguid: %s\r\nTextScriptImporter:\r\n  externalObjects: {}\r\n  userData: \r\n  assetBundleName: \r\n  assetBundleVariant: \r\n" % uuid.uuid4().hex)
        # determinism: generating twice gives the same file
        again = json.dumps(grammar.strip_layout(grammar.generate(concept)), indent=1)
        assert again.replace("\r\n", "\n") == json.dumps(data, indent=1).replace("\r\n", "\n"), concept["name"]
        written.append((concept["file"], concept["name"], len(data["features"]), data.get("spawns")))
        print(f"{concept['file']}.json  {concept['name']:20s} features={len(data['features']):3d} spawns={data.get('spawns')}")
    print(f"{len(written)} maps OK" + (" (check only)" if check_only else ""))


if __name__ == "__main__":
    main()
