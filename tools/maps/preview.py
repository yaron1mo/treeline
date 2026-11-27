#!/usr/bin/env python3
"""Preview PNGs for the designed sector maps (issue #46).

One PNG per map plus a contact sheet with Forest belts first, in
docs/design/visual/maps/ (that folder is not in Git LFS, so Claude and Telegram can
show the images). Flat colours: belts dark green, tracks brown, spawns red.

Usage: python3 tools/maps/preview.py [--out DIR] [--scale 3]
"""

import json
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
FIELDS = os.path.join(ROOT, "unity", "Assets", "Resources", "Fields")
DEFAULT_OUT = os.path.join(ROOT, "docs", "design", "visual", "maps")

TONES = {
    "stubble": (201, 176, 131),
    "plowed": (138, 111, 82),
    "sunflower": (216, 181, 69),
    "rape": (183, 176, 78),
    "fallow": (169, 144, 107),
}
OPEN = (179, 160, 119)
BELT = (47, 93, 47)        # dark green
TRACK = (122, 90, 52)      # brown
ROAD = (141, 141, 141)
GROVE = (74, 122, 58)
POND = (74, 111, 165)
CRATER = (107, 91, 69)
SPAWN = (208, 32, 32)      # red
BASE = (160, 30, 30)


def draw_field(field, scale):
    w, h = field["size"]
    img = Image.new("RGB", (int(w * scale), int(h * scale)), OPEN)
    d = ImageDraw.Draw(img)

    def px(p):
        return (p[0] * scale, p[1] * scale)

    for f in field["features"]:
        t = f["type"]
        if t == "field":
            d.polygon([px(p) for p in f["poly"]], fill=TONES.get(f.get("tone"), OPEN))
    for f in field["features"]:
        if f["type"] == "craters":
            for x, y, r in f["points"]:
                d.ellipse([(x - r) * scale, (y - r) * scale, (x + r) * scale, (y + r) * scale], fill=CRATER)
    for f in field["features"]:
        t = f["type"]
        if t == "pond":
            d.polygon([px(p) for p in f["poly"]], fill=POND)
        elif t == "grove":
            d.polygon([px(p) for p in f["poly"]], fill=GROVE)
    for f in field["features"]:
        if f["type"] == "tracks":
            d.line([px(p) for p in f["line"]], fill=TRACK, width=max(2, int(scale * 0.8)))
        elif f["type"] == "road":
            d.line([px(p) for p in f["line"]], fill=ROAD, width=max(2, int(f.get("width", 5) * scale)))
    for f in field["features"]:
        if f["type"] == "belt":
            d.line([px(p) for p in f["line"]], fill=BELT, width=max(3, int(f.get("width", 12) * scale)),
                   joint="curve")
    # base line at the bottom edge, spawns as red dots on the top edge
    d.line([(0, (h - 0.5) * scale), (w * scale, (h - 0.5) * scale)], fill=BASE, width=max(2, int(scale)))
    for s in field.get("spawns", []):
        r = 5 * scale / 3
        d.ellipse([s * scale - r, -r, s * scale + r, r], fill=SPAWN)
    return img


def main():
    out = DEFAULT_OUT
    scale = 3
    args = sys.argv[1:]
    if "--out" in args:
        out = args[args.index("--out") + 1]
    if "--scale" in args:
        scale = int(args[args.index("--scale") + 1])
    os.makedirs(out, exist_ok=True)

    with open(os.path.join(HERE, "concepts.json"), encoding="utf-8") as fh:
        concepts = json.load(fh)["maps"]

    ref_path = os.path.join(FIELDS, "forest-belts.json")
    with open(ref_path, encoding="utf-8") as fh:
        ref = json.load(fh)

    entries = [("forest-belts", ref)] + [(c["file"], json.load(open(os.path.join(FIELDS, c["file"] + ".json"), encoding="utf-8"))) for c in concepts]
    for fname, field in entries:
        img = draw_field(field, scale)
        path = os.path.join(out, fname + ".png")
        img.save(path)
        print(path, img.size)

    # contact sheet: Forest belts first, then the 12 sector maps
    tw, th = 200, 400          # thumbnail size (scale 1)
    label = 18
    cols = 5
    rows = (len(entries) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (tw + 8) + 8, rows * (th + label + 8) + 8), (245, 245, 245))
    d = ImageDraw.Draw(sheet)
    for i, (fname, field) in enumerate(entries):
        img = draw_field(field, 1)
        x = 8 + (i % cols) * (tw + 8)
        y = 8 + (i // cols) * (th + label + 8)
        sheet.paste(img, (x, y + label))
        d.text((x + 4, y + 3), f"{i}: {field['name']}", fill=(20, 20, 20))
    sheet_path = os.path.join(out, "contact-sheet.png")
    sheet.save(sheet_path)
    print(sheet_path, sheet.size)


if __name__ == "__main__":
    main()
