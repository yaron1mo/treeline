"""Assemble the generated parts into an 8-frame walk test. Requires Pillow."""
from pathlib import Path
from math import sin, tau
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
source = Image.open(ROOT / 'docs/design/visual/round3/modular-source.png').convert('RGBA')
cols = [(45, 405), (470, 805), (900, 1245), (1295, 1780), (1840, 2110)]

def part(row, col, size):
    x0, x1 = cols[col]
    image = source.crop((x0, row * 362, x1, (row + 1) * 362))
    alpha = image.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    image.putalpha(alpha)
    image = image.crop(image.getbbox())
    image.thumbnail(size, Image.Resampling.LANCZOS)
    image.putalpha(image.getchannel('A').point(lambda a: 255 if a >= 128 else 0))
    return image

def frame(row, n):
    head = part(row, 0, (36, 32)); helmet = part(row, 1, (40, 30))
    body = part(row, 2, (43, 45)); gun = part(row, 3, (45, 18)); patch = part(row, 4, (7, 7))
    image = Image.new('RGBA', (64, 64))
    bob = round(abs(sin(n * tau / 8)))
    swing = round(2 * sin(n * tau / 8))
    image.alpha_composite(body, ((64-body.width)//2, 19-bob))
    image.alpha_composite(head, ((64-head.width)//2 + swing//2, 10-bob))
    image.alpha_composite(helmet, ((64-helmet.width)//2 + swing//2, 5-bob))
    image.alpha_composite(gun, ((64-gun.width)//2 + swing, 37-bob))
    image.alpha_composite(patch, (42, 31-bob))
    return image

strip = Image.new('RGBA', (8 * 128, 2 * 128), (72, 83, 65, 255))
for row in range(2):
    for n in range(8):
        strip.alpha_composite(frame(row, n).resize((128,128), Image.Resampling.NEAREST), (n*128,row*128))
strip.save(ROOT / 'docs/design/visual/round3/modular-walk-strip.png')
