#!/usr/bin/env python3
"""Export/edit/repack Treeline's 23 indexed 2D unit sprites.

This is an EDITING tool; Unity reads the compact text asset at runtime.
Requires Pillow only for PNG I/O:
  pip install pillow
  python tools/art/unit_atlas.py export
  # Edit 48x48 PNGs inside _unit_work/; keep current 18-color palette.
  python tools/art/unit_atlas.py pack

Editing rule: Keep sprite artwork inside its 48x48 tile and preserve alpha.
New palette colors require an explicit palette edit here AND in UnitSpriteAtlas.cs.
Binary PNG exports are disposable work files; repo commits the indexed source.
"""
import base64
import gzip
from pathlib import Path
import sys

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ASSET = ROOT / "unity/Assets/Resources/Art/Units/units_atlas_indexed.txt"
WORK = ROOT / "_unit_work"
CELL, COLUMNS, ROWS = 48, 8, 3
WIDTH, HEIGHT = CELL * COLUMNS, CELL * ROWS

IDS = [
    "recruit", "regular", "special_forces", "old_tank",
    "modern_tank", "ifv", "mrap", "buggy",
    "motorbike", "van", "recon_uav", "fpv",
    "one_way_drone", "helicopter", "rifle_squad", "machine_gun",
    "sniper", "mortar", "anti_tank", "air_defence",
    "fpv_operator", "bomber_operator", "recon_drone"
]

COLORS = [
    (0, 0, 0, 0), (32, 42, 37, 255), (38, 43, 43, 255),
    (41, 46, 43, 255), (48, 58, 47, 255), (57, 62, 50, 255),
    (71, 91, 82, 255), (73, 90, 78, 255), (102, 118, 76, 255),
    (113, 91, 70, 255), (122, 133, 112, 255), (127, 139, 89, 255),
    (137, 130, 105, 255), (153, 76, 58, 255),
    (183, 160, 118, 255), (203, 181, 141, 255),
    (212, 203, 175, 255), (214, 173, 72, 255)
]

def unpack():
    data = gzip.decompress(base64.b64decode(ASSET.read_text().strip()))
    if len(data) != WIDTH * HEIGHT:
        raise ValueError("Unexpected indexed texture shape")
    image = Image.new("RGBA", (WIDTH, HEIGHT))
    image.putdata([COLORS[i] for i in data])
    return image

def export():
    WORK.mkdir(exist_ok=True)
    image = unpack()
    image.save(WORK / "units_atlas.png")
    for i, name in enumerate(IDS):
        x, y = (i % COLUMNS) * CELL, (i // COLUMNS) * CELL
        image.crop((x, y, x + CELL, y + CELL)).save(WORK / (name + ".png"))
    print("Exported", len(IDS), "tiles to", WORK)

def pack():
    image = Image.new("RGBA", (WIDTH, HEIGHT))
    for i, name in enumerate(IDS):
        path = WORK / (name + ".png")
        if not path.is_file():
            raise FileNotFoundError(path)
        sprite = Image.open(path).convert("RGBA")
        if sprite.size != (CELL, CELL):
            raise ValueError(str(path) + " must be 48x48")
        x, y = (i % COLUMNS) * CELL, (i // COLUMNS) * CELL
        image.paste(sprite, (x, y))

    lookup = {tuple(value): i for i, value in enumerate(COLORS)}
    missing = sorted(set(image.getdata()) - set(COLORS))
    if missing:
        raise ValueError(
            "New colors detected. Extend matching palette in this tool "
            "and UnitSpriteAtlas.cs before packing: " + str(missing[:8])
        )
    payload = bytes(lookup[rgba] for rgba in image.getdata())
    ASSET.write_text(base64.b64encode(gzip.compress(payload, compresslevel=9, mtime=0)).decode() + "\n")
    print("Packed", len(IDS), "tiles into", ASSET)

if __name__ == "__main__":
    if len(sys.argv) != 2 or sys.argv[1] not in ("export", "pack"):
        raise SystemExit("Usage: python tools/art/unit_atlas.py export|pack")
    (export if sys.argv[1] == "export" else pack)()
