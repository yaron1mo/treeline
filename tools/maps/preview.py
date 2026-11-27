#!/usr/bin/env python3
"""Preview PNGs for the designed maps (issue #46).

One PNG per map plus a contact sheet in docs/design/visual/maps/: Forest belts
first, then the designed maps grouped by category (A river, B hills, C forest-heavy,
D open steppe). That folder is not in Git LFS, so Claude and Telegram can show the
images. Flat colours: belts dark green, river blue, spawns red.

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

sys.path.insert(0, HERE)
import grammar  # noqa: E402

TONES = {
    "stubble": (201, 176, 131),
    "plowed": (138, 111, 82),
    "sunflower": (216, 181, 69),
    "rape": (183, 176, 78),
    "fallow": (169, 144, 107),
}
OPEN = (179, 160, 119)
BELT = (47, 93, 47)        # dark green
BURNED = (120, 104, 82)    # the burned strip (map 15): looks like cover, gives none
TRACK = (122, 90, 52)      # brown
ROAD = (141, 141, 141)
GROVE = (74, 122, 58)
POND = (74, 111, 165)
RIVER = (58, 98, 180)
FORD = (130, 170, 220)
PIPE = (35, 65, 130)
BRIDGE = (100, 88, 70)
HILL = (172, 148, 105)
BALKA = (90, 70, 48)
EMBANK = (125, 104, 72)
HEAP = (135, 110, 78)
BUSH = (110, 150, 70)
ROCK = (125, 125, 125)
WRECK = (70, 62, 58)
TRENCH = (60, 45, 35)
WIRE = (200, 60, 60)
HEDGEHOG = (150, 40, 40)
CRATER = (107, 91, 69)
SPAWN = (208, 32, 32)      # red
BASE = (160, 30, 30)

# draw order per feature type
ORDER = ["field", "craters", "river", "hill", "balka", "embankment", "pond", "ford",
         "pipe", "bridge_ruins", "heap", "grove", "bush", "rock", "wreck", "hedgehog",
         "belt", "trench", "wire", "road", "tracks"]

FILLS = {
    "pond": POND, "ford": FORD, "hill": HILL, "heap": HEAP, "grove": GROVE, "bush": BUSH,
    "rock": ROCK, "wreck": WRECK, "hedgehog": HEDGEHOG,
}
LINES = {
    "river": RIVER, "pipe": PIPE, "bridge_ruins": BRIDGE, "balka": BALKA, "embankment": EMBANK,
    "belt": BELT, "trench": TRENCH, "wire": WIRE, "road": ROAD, "tracks": TRACK,
}


def draw_field(field, scale, burned_y=None):
    w, h = field["size"]
    img = Image.new("RGB", (int(w * scale), int(h * scale)), OPEN)
    d = ImageDraw.Draw(img)

    def px(p):
        return (p[0] * scale, p[1] * scale)

    def poly(f):
        d.polygon([px(p) for p in f["poly"]], fill=fill)

    def line(f, color, width=None):
        d.line([px(p) for p in f["line"]], fill=color,
               width=max(1, int((width if width is not None else f.get("width", 4)) * scale)),
               joint="curve")

    feats = sorted(field["features"], key=lambda f: ORDER.index(f["type"]) if f["type"] in ORDER else 99)
    for f in feats:
        t = f["type"]
        if t == "field":
            d.polygon([px(p) for p in f["poly"]], fill=TONES.get(f.get("tone"), OPEN))
        elif t == "craters":
            for x, y, r in f["points"]:
                d.ellipse([(x - r) * scale, (y - r) * scale, (x + r) * scale, (y + r) * scale], fill=CRATER)
        elif t in FILLS:
            fill = FILLS[t]
            poly({"poly": f["poly"]})
        elif t == "belt" and burned_y is not None:
            ys = [p[1] for p in f["line"]]
            if abs(sum(ys) / len(ys) - burned_y) < 12:
                line(f, BURNED)
            else:
                line(f, BELT)
        elif t in LINES:
            line(f, LINES[t])
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

    with open(os.path.join(FIELDS, "forest-belts.json"), encoding="utf-8") as fh:
        ref = json.load(fh)

    entries = [("forest-belts", ref, None, "0")] + [
        (c["file"], json.load(open(os.path.join(FIELDS, c["file"] + ".json"), encoding="utf-8")), c,
         f"{c['category']}{c['no']}")
        for c in concepts
    ]
    for fname, field, concept, tag in entries:
        burned_y = None
        if concept and concept.get("burned_belt") is not None:
            lay = grammar.generate(concept)["_layout"]
            burned_y = lay["belt_ys"][concept["burned_belt"]]
        img = draw_field(field, scale, burned_y)
        path = os.path.join(out, fname + ".png")
        img.save(path)
        print(path, img.size)

    # contact sheet: Forest belts first, then the maps grouped by category A-D
    tw, th = 200, 400          # thumbnail size (scale 1)
    label = 18
    cols = 5
    rows = (len(entries) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (tw + 8) + 8, rows * (th + label + 8) + 8), (245, 245, 245))
    d = ImageDraw.Draw(sheet)
    for i, (fname, field, concept, tag) in enumerate(entries):
        burned_y = None
        if concept and concept.get("burned_belt") is not None:
            lay = grammar.generate(concept)["_layout"]
            burned_y = lay["belt_ys"][concept["burned_belt"]]
        img = draw_field(field, 1, burned_y)
        x = 8 + (i % cols) * (tw + 8)
        y = 8 + (i // cols) * (th + label + 8)
        sheet.paste(img, (x, y + label))
        d.text((x + 4, y + 3), f"{tag}: {field['name']}", fill=(20, 20, 20))
    sheet_path = os.path.join(out, "contact-sheet.png")
    sheet.save(sheet_path)
    print(sheet_path, sheet.size)


if __name__ == "__main__":
    main()
