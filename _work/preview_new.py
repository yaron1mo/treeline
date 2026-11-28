#!/usr/bin/env python3
"""Preview PNGs for the designed maps (issue #46).

One PNG per map plus a contact sheet in docs/design/visual/maps/: Forest belts
first, then the designed maps grouped by category (A river, B hills, C forest,
D steppe, V village). That folder is not in Git LFS, so Claude and Telegram can
show the images. Flat colours, hills drawn with a shaded rim so they read on
screen; spawns red, our line at the bottom.

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
    "fallow": (172, 150, 112),
}
OPEN = (179, 160, 119)

# fills for closed polygons
FILLS = {
    "hill": (196, 172, 122), "grove": (63, 108, 50), "belt": (47, 93, 47),
    "burned_belt": (88, 74, 60), "pond": (74, 111, 165), "balka": (96, 76, 52),
    "embankment": (150, 122, 84), "heap": (135, 110, 78), "ford": (130, 170, 220),
    "pipe": (120, 120, 112), "rock": (125, 125, 125), "wreck": (66, 58, 54),
    "bush": (110, 150, 70), "trench": (60, 45, 35), "wire": (200, 60, 60),
    "hedgehog": (150, 40, 40), "house": (188, 84, 60), "ruin": (150, 110, 90),
    "fence": (170, 140, 90), "sunflower": (216, 181, 69), "stack": (210, 180, 90),
    "shed": (170, 110, 70), "silo": (150, 150, 145), "camo_net": (120, 140, 90),
    "dugout": (90, 70, 50),
}
# lines drawn from "line" + width
LINES = {
    "river": (58, 98, 180), "road": (150, 138, 120), "tracks": (122, 96, 60),
    "bridge_ruins": (100, 88, 70),
}
CRATER = (107, 91, 69)
SPAWN = (208, 32, 32)
BASE = (160, 30, 30)

# draw order: ground first, water and paths, then cover, then small objects
ORDER = ["field", "craters", "river", "balka", "pond", "ford", "pipe", "hill",
         "embankment", "heap", "bridge_ruins", "grove", "belt", "burned_belt",
         "sunflower", "bush", "camo_net", "fence", "rock", "wreck", "stack",
         "shed", "silo", "house", "ruin", "dugout", "trench", "wire", "hedgehog",
         "road", "tracks"]


def draw_field(field, scale):
    w, h = field["size"]
    img = Image.new("RGB", (int(w * scale), int(h * scale)), OPEN)
    d = ImageDraw.Draw(img)

    def px(p):
        return (p[0] * scale, p[1] * scale)

    feats = sorted(field["features"],
                   key=lambda f: ORDER.index(f["type"]) if f["type"] in ORDER else 99)
    for f in feats:
        t = f["type"]
        if t == "field":
            d.polygon([px(p) for p in f["poly"]], fill=TONES.get(f.get("tone"), OPEN))
        elif t == "craters":
            for x, y, r in f["points"]:
                d.ellipse([(x - r) * scale, (y - r) * scale, (x + r) * scale, (y + r) * scale],
                          fill=CRATER)
        elif t in LINES:
            width = max(1, int(f.get("width", 4) * scale))
            d.line([px(p) for p in f["line"]], fill=LINES[t], width=width, joint="curve")
        elif t in FILLS:
            poly = [px(p) for p in f["poly"]]
            d.polygon(poly, fill=FILLS[t])
            if t == "hill":  # shaded rim so the hill reads on screen
                d.line(poly + [poly[0]], fill=(120, 100, 62), width=max(1, int(scale)))
    # base line at the bottom edge, spawns as red dots on the top edge
    d.line([(0, (h - 0.5) * scale), (w * scale, (h - 0.5) * scale)], fill=BASE,
           width=max(2, int(scale)))
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

    entries = [("forest-belts", ref, None)] + [
        (c["file"], json.load(open(os.path.join(FIELDS, c["file"] + ".json"), encoding="utf-8")), c)
        for c in concepts
    ]

    for fname, field, concept in entries:
        img = draw_field(field, scale)
        path = os.path.join(out, fname + ".png")
        img.save(path)
        print(path, img.size)

    # contact sheet: Forest belts first, then the maps grouped by category
    tw, th = 200, 400
    label = 18
    cols = 5
    rows = (len(entries) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (tw + 8) + 8, rows * (th + label + 8) + 8), (245, 245, 245))
    d = ImageDraw.Draw(sheet)
    for i, (fname, field, concept) in enumerate(entries):
        img = draw_field(field, 1)
        x = 8 + (i % cols) * (tw + 8)
        y = 8 + (i // cols) * (th + label + 8)
        sheet.paste(img, (x, y + label))
        tag = "0" if concept is None else f"{concept['category']}{concept['no']}"
        d.text((x + 4, y + 3), f"{tag}: {field['name']}", fill=(20, 20, 20))
    sheet_path = os.path.join(out, "contact-sheet.png")
    sheet.save(sheet_path)
    print(sheet_path, sheet.size)


if __name__ == "__main__":
    main()
