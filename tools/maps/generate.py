#!/usr/bin/env python3
"""Generate the designed sector maps (issue #46) into unity/Assets/Resources/Fields/.

Reads tools/maps/concepts.json, builds each map with tools/maps/grammar.py (same seed +
same parameters -> same map) and writes sector-<file>.json in the Forest belts field
format, plus the Unity .meta file with a fresh GUID.

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

# Feature types the Sim knows (TerrainTable names + obstacle ids used on fields).
KNOWN_TYPES = {
    "field", "tracks", "craters", "grove", "pond", "belt", "road",
    "rock", "wreck", "bush", "heap", "embankment", "hill", "trench", "balka",
    "river", "ford", "pipe", "wire", "hedgehog",
}


def validate(field, concept):
    w, h = field["size"]
    assert field["name"] == concept["name"]
    assert 0 < w <= 400 and 0 < h <= 800
    assert 1 <= len(field["spawns"]) <= 2, "1-2 spawns"
    for s in field["spawns"]:
        assert grammar.RULES["edge_margin"] <= s <= w - grammar.RULES["edge_margin"]
    belts = [f for f in field["features"] if f["type"] == "belt"]
    assert grammar.RULES["belts_per_map"][0] <= len(belts) <= 12
    for f in field["features"]:
        assert f["type"] in KNOWN_TYPES, f["type"]
        pts = f.get("poly") or f.get("line") or f.get("points")
        assert pts, "feature without geometry"
        for p in (f.get("poly") or f.get("line") or []):
            assert len(p) == 2
    # every belt row keeps its designed gaps: the Sim covers a point within half the
    # belt width of the segment, so the gap centre must stay open and the effective
    # opening (gap width - belt width, as in Forest belts) must stay positive
    lay = field["_layout"]
    for y, gaps, gws, bw in zip(lay["belt_ys"], lay["gaps"], lay["gap_widths"], lay["belt_ws"]):
        for gx, gw in zip(gaps, gws):
            assert gw - bw >= 3.5, f"gap at x={gx} y={y} is narrower than the belt: {gw} - {bw}"
            for f in belts:
                line = f.get("line")
                if line is None or not any(abs(p[1] - y) < 20 for p in line):
                    continue
                half = max(f.get("width", 12) / 2, 1.5)
                for a, b in zip(line, line[1:]):
                    if not (min(a[0], b[0]) - 0.01 <= gx <= max(a[0], b[0]) + 0.01):
                        continue
                    t = 0.0 if b[0] == a[0] else (gx - a[0]) / (b[0] - a[0])
                    sy = a[1] + t * (b[1] - a[1])
                    if abs(sy - y) <= half:
                        raise AssertionError(f"belt covers the gap centre at x={gx} y={y}")
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
            with open(meta, "w", encoding="utf-8", newline="") as fh:
                fh.write("fileFormatVersion: 2\r\nguid: %s\r\nTextScriptImporter:\r\n  externalObjects: {}\r\n  userData: \r\n  assetBundleName: \r\n  assetBundleVariant: \r\n" % uuid.uuid4().hex)
        # determinism: generating twice gives the same file
        again = json.dumps(grammar.strip_layout(grammar.generate(concept)), indent=1)
        assert again.replace("\r\n", "\n") == json.dumps(data, indent=1).replace("\r\n", "\n"), concept["name"]
        written.append((concept["file"], concept["name"], len(data["features"]), data["spawns"]))
        print(f"{concept['file']}.json  {concept['name']:22s} features={len(data['features']):3d} spawns={data['spawns']}")
    print(f"{len(written)} maps OK" + (" (check only)" if check_only else ""))


if __name__ == "__main__":
    main()
