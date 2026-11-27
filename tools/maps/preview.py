#!/usr/bin/env python3
"""Draw the real maps as PNG previews (flat colours, like BattlefieldPreview) and one
contact sheet. Optional: draw the lanes from tools/maps/lanes.cs output.

    python3 tools/maps/preview.py                       # all real-*.json -> docs/maps/previews/ + docs/maps/all.png
    python3 tools/maps/preview.py --lanes tools/maps/lanes.tsv

No extra packages: standard-library Python 3.8+ (writes PNG with zlib).
Map data (c) OpenStreetMap contributors, ODbL.
"""

import argparse
import glob
import json
import os
import struct
import sys
import zlib

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import osm_to_field as osm  # noqa: E402

FIELDS_DIR = os.path.join(osm.ROOT, "unity", "Assets", "Resources", "Fields")
PREVIEWS_DIR = os.path.join(osm.ROOT, "docs", "maps", "previews")
CONTACT = os.path.join(osm.ROOT, "docs", "maps", "all.png")

PX = 2  # pixels per metre

BASE = (153, 166, 104)
TONE_COLOR = {"plowed": (150, 104, 67), "sunflower": (130, 154, 76), "stubble": (201, 169, 105)}
POND = (64, 111, 117)
RIVER = (64, 111, 117)
ROAD_OUT = (97, 78, 50)
ROAD_IN = (189, 161, 109)
TRACK = (174, 146, 95)
BELT = (74, 116, 58)
GROVE = (52, 84, 44)
CRATER = (55, 52, 41)
SPAWN = (200, 40, 40)
LANE = (240, 240, 240)


def write_png(path, width, height, pixels):
    """pixels: bytearray RGB rows, top to bottom."""
    raw = bytearray()
    stride = width * 3
    for y in range(height):
        raw.append(0)
        raw += pixels[y * stride:(y + 1) * stride]

    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 6))
    png += chunk(b"IEND", b"")
    with open(path, "wb") as fh:
        fh.write(png)


def fill_poly(img, w, h, poly, color):
    xs = [p[0] for p in poly]
    ys = [p[1] for p in poly]
    x0 = max(0, int(min(xs) * PX))
    x1 = min(w - 1, int(max(xs) * PX) + 1)
    y0 = max(0, int(min(ys) * PX))
    y1 = min(h - 1, int(max(ys) * PX) + 1)
    n = len(poly)
    for y in range(y0, y1 + 1):
        wy = (y + 0.5) / PX
        row = y * w * 3
        for x in range(x0, x1 + 1):
            wx = (x + 0.5) / PX
            c = False
            j = n - 1
            for i in range(n):
                xi, yi = poly[i]
                xj, yj = poly[j]
                if (yi > wy) != (yj > wy) and wx < (xj - xi) * (wy - yi) / (yj - yi) + xi:
                    c = not c
                j = i
            if c:
                o = row + x * 3
                img[o:o + 3] = bytes(color)


def stroke(img, w, h, line, width_m, color):
    r = max(1, int(round(width_m * PX / 2)))
    for i in range(1, len(line)):
        x0, y0 = line[i - 1]
        x1, y1 = line[i]
        steps = int(max(abs(x1 - x0), abs(y1 - y0)) * PX) + 1
        for s in range(steps + 1):
            t = s / steps
            x = (x0 + t * (x1 - x0)) * PX
            y = (y0 + t * (y1 - y0)) * PX
            for dy in range(-r, r + 1):
                for dx in range(-r, r + 1):
                    if dx * dx + dy * dy > r * r:
                        continue
                    px, py = int(x) + dx, int(y) + dy
                    if 0 <= px < w and 0 <= py < h:
                        o = (py * w + px) * 3
                        img[o:o + 3] = bytes(color)


def draw_map(field, lanes_x=None):
    w, h = int(field["size"][0] * PX), int(field["size"][1] * PX)
    img = bytearray(bytes(BASE) * (w * h))
    for f in field.get("features", []):
        t = f.get("type")
        if t == "field" and f.get("poly"):
            fill_poly(img, w, h, f["poly"], TONE_COLOR.get(f.get("tone"), TONE_COLOR["stubble"]))
    for f in field.get("features", []):
        t = f.get("type")
        if t == "pond" and f.get("poly"):
            fill_poly(img, w, h, f["poly"], POND)
        elif t == "river" and f.get("line"):
            stroke(img, w, h, f["line"], f.get("width", 8), RIVER)
        elif t == "road" and f.get("line"):
            stroke(img, w, h, f["line"], f.get("width", 5) + 1.5, ROAD_OUT)
            stroke(img, w, h, f["line"], f.get("width", 5) * 0.8, ROAD_IN)
    for f in field.get("features", []):
        if f.get("type") == "tracks" and f.get("line"):
            stroke(img, w, h, f["line"], 3.2, TRACK)
    for f in field.get("features", []):
        if f.get("type") == "grove" and f.get("poly"):
            fill_poly(img, w, h, f["poly"], GROVE)
        elif f.get("type") == "belt" and f.get("line"):
            stroke(img, w, h, f["line"], f.get("width", 14), BELT)
    for f in field.get("features", []):
        if f.get("type") == "craters":
            for p in f.get("points", []):
                x, y, r = p[0], p[1], p[2] if len(p) > 2 else 1.0
                for dy in range(-int(r * PX), int(r * PX) + 1):
                    for dx in range(-int(r * PX), int(r * PX) + 1):
                        if dx * dx + dy * dy <= r * r * PX * PX:
                            px, py = int(x * PX) + dx, int(y * PX) + dy
                            if 0 <= px < w and 0 <= py < h:
                                o = (py * w + px) * 3
                                img[o:o + 3] = bytes(CRATER)
    if lanes_x:
        for lx in lanes_x:
            for y in range(0, h, 6):
                for x in range(int(lx * PX) - 1, int(lx * PX) + 2):
                    if 0 <= x < w and y + 2 < h:
                        o = (y * w + x) * 3
                        img[o:o + 3] = bytes(LANE)
    for s in field.get("spawns", []):
        for x in range(int(s * PX) - 3, int(s * PX) + 4):
            for y in range(0, 10):
                if 0 <= x < w and y < h:
                    o = (y * w + x) * 3
                    img[o:o + 3] = bytes(SPAWN)
    return w, h, img


def load_lanes(path):
    lanes = {}
    with open(path, encoding="utf-8") as fh:
        for line in fh:
            parts = line.rstrip("\n").split("\t")
            if len(parts) >= 3 and parts[1].isdigit():
                lanes[parts[0]] = [float(x) for x in parts[2].split(",") if x]
    return lanes


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--fields-dir", default=FIELDS_DIR)
    ap.add_argument("--out-dir", default=PREVIEWS_DIR)
    ap.add_argument("--contact", default=CONTACT)
    ap.add_argument("--lanes", default="", help="TSV from tools/maps/lanes.cs (name, count, lane xs, spawns)")
    ap.add_argument("--pattern", default="real-*.json")
    args = ap.parse_args(argv)

    lanes = load_lanes(args.lanes) if args.lanes else {}
    os.makedirs(args.out_dir, exist_ok=True)
    files = sorted(glob.glob(os.path.join(args.fields_dir, args.pattern)))
    thumbs = []
    for path in files:
        with open(path, encoding="utf-8") as fh:
            field = json.load(fh)
        w, h, img = draw_map(field, lanes.get(field.get("name", "")))
        out = os.path.join(args.out_dir, os.path.splitext(os.path.basename(path))[0] + ".png")
        write_png(out, w, h, img)
        print(out)
        # thumbnail 100 x 200 for the contact sheet (nearest neighbour)
        tw, th = 100, 200
        timg = bytearray(bytes(BASE) * (tw * th))
        for y in range(th):
            sy = y * h // th
            for x in range(tw):
                sx = x * w // tw
                timg[(y * tw + x) * 3:(y * tw + x) * 3 + 3] = img[(sy * w + sx) * 3:(sy * w + sx) * 3 + 3]
        thumbs.append((os.path.basename(out), tw, th, timg))

    if thumbs and args.contact:
        cols = 5
        rows = (len(thumbs) + cols - 1) // cols
        cw, ch = 110, 210
        sheet = bytearray(bytes((30, 34, 26)) * (cols * cw * rows * ch))
        for i, (name, tw, th, timg) in enumerate(thumbs):
            ox, oy = (i % cols) * cw + 5, (i // cols) * ch + 5
            for y in range(th):
                for x in range(tw):
                    o = ((oy + y) * cols * cw + ox + x) * 3
                    sheet[o:o + 3] = timg[(y * tw + x) * 3:(y * tw + x) * 3 + 3]
        write_png(args.contact, cols * cw, rows * ch, sheet)
        print(args.contact)


if __name__ == "__main__":
    main()
