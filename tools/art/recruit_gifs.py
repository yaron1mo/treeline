"""Make one recruit animation preview from the art used by the renderer."""
import argparse
import base64
import io
import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'unity/Assets/Resources/Art/Units'
OUT = ROOT / 'docs/design/visual/units/recruit'
NEAREST = Image.Resampling.NEAREST


def strip(name):
    data = base64.b64decode((ART / ('infantry_recruit_' + name + '.txt')).read_text())
    return Image.open(io.BytesIO(data)).convert('RGBA')


def frames(name, count):
    image = strip(name)
    return [image.crop((64 * n, 0, 64 * (n + 1), 64)) for n in range(count)]


def back_frames(view, crutch):
    stem = 'recruit' + ('_diagonal' if view == 'diagonal' else '') + ('_crutch' if crutch else '') + '_backfall.png'
    image = Image.open(ART / stem).convert('RGBA')
    cell = image.width / 6
    size = cell * (0.8 if view == 'diagonal' and crutch else 1)
    y = (image.height - size) / 2
    return [image.crop((round((i + .5) * cell - size / 2), round(y),
                        round((i + .5) * cell + size / 2), round(y + size))).resize((64, 64), NEAREST)
            for i in range(6)]


def frame_name(view, crutch, pose):
    if view == 'diagonal':
        return 'diagonal_' + ('crutch_' if crutch else '') + pose
    if crutch:
        return 'crutch' if pose == 'walk' else 'crutch_' + pose
    return pose


def field():
    return Image.new('RGBA', (128, 128), (92, 102, 77, 255))


def muzzle_position(view, crutch):
    if view == 'diagonal':
        return (41 if crutch else 43, 80)
    return (49, 50)


def put(canvas, sprite, x=32, y=32):
    canvas.alpha_composite(sprite, (round(x), round(y)))


def finish(images, durations, path):
    # Scale after compositing so the GIF keeps hard pixel edges.
    converted = [im.convert('RGB').resize((384, 384), NEAREST).quantize(colors=128) for im in images]
    converted[0].save(path, save_all=True, append_images=converted[1:], duration=durations,
                      loop=0, disposal=2, optimize=True)
    if path.stat().st_size >= 2_000_000:
        raise RuntimeError('GIF exceeds 2 MB: ' + str(path))


def ordinary(view, crutch, kind):
    walk = frames(frame_name(view, crutch, 'walk'), 4)
    images = []
    if kind == 'walk':
        sequence, delays = [walk[i] for i in (0, 1, 2, 3)], [170, 100, 115, 125]
    elif kind == 'shoot':
        sequence = frames(frame_name(view, crutch, 'shoot'), 3)
        delays = [80, 100, 120]
    elif kind == 'hit':
        base = walk[1]
        red = Image.new('RGBA', base.size, (255, 89, 89, 0))
        red.putalpha(base.getchannel('A'))
        sequence = [base, Image.blend(base, red, .65), Image.blend(base, red, .3), base]
        delays = [100, 50, 60, 150]
    elif kind == 'fall-forward':
        sequence = frames(frame_name(view, crutch, 'fall'), 6)
        delays = [90] * 5 + [600]
    elif kind == 'fall-backward':
        sequence = back_frames(view, crutch)
        delays = [90] * 5 + [600]
    elif kind == 'loose-helmet':
        sequence = frames(frame_name(view, crutch, 'bare_fall'), 6)
        delays = [90] * 5 + [600]
    else:
        raise ValueError(kind)
    for n, sprite in enumerate(sequence):
        canvas = field()
        offset = 3 if kind == 'hit' and n in (1, 2) else 0
        put(canvas, sprite, 32, 32 + offset)
        if kind == 'shoot' and n < 2:
            d = ImageDraw.Draw(canvas)
            x, y = muzzle_position(view, crutch)
            d.polygon([(x - 3, y), (x, y - 4), (x + 3, y), (x, y + 4)], fill=(255, 208, 85, 255))
        if kind == 'loose-helmet':
            helmet = strip('loose_helmet')
            t = min(1, n * .09 / .65)
            put(canvas, helmet, 32 + t * 21, 19 - math.sin(math.pi * t) * 8 - t * 10)
        images.append(canvas)
    return images, delays


def parts_atlas():
    im = Image.open(ART / 'infantry_parts.png').convert('RGBA')
    cell = im.width / 6
    y = (im.height - cell) / 2
    return [im.crop((round(i * cell), round(y), round((i + 1) * cell), round(y + cell))).resize((64, 64), NEAREST)
            for i in range(6)]


def explosion(view, kind):
    radius = {'grenade': 3., 'mortar': 10., 'vehicle': 6.}[kind]
    power = min(1., radius / 12.)
    chunks = 3 + round(3 * power)
    parts = parts_atlas()
    walk = frames(frame_name(view, False, 'walk'), 4)[1]
    images = []
    for frame in range(18):
        canvas = field()
        elapsed = max(0., (frame - 2) * .08)
        if frame < 2:
            put(canvas, walk)
        else:
            draw = ImageDraw.Draw(canvas)
            draw.ellipse((56, 67, 73, 72), fill=(153, 35, 40, 255))
            for i in range(6 + chunks):
                index = 0 if i == 0 else 1 if i <= 2 else i - 1 if i <= 5 else 5
                scale = .48 if i in (0, 5) else .42 if i == 4 else .30
                sprite = parts[index].resize((round(64 * scale), round(64 * scale)), NEAREST)
                angle = math.radians(i * 137.5 + 17)
                vx, vy = math.cos(angle), math.sin(angle) - .55
                length = math.hypot(vx, vy)
                distance = (2 + (i % 4) * .65) * (.8 + power * 1.6) * 64 / 12
                t = min(1., elapsed / (.65 + (i % 3) * .09))
                arc = math.sin(t / .75 * math.pi) if t < .75 else .18 * math.sin((t - .75) / .25 * math.pi)
                height = (1.5 + power * 2.5) * 64 / 12
                spin = (1 if i % 2 == 0 else -1) * (180 + i * 37) * t
                sprite = sprite.rotate(spin, resample=NEAREST, expand=True)
                x = 64 + vx / length * distance * t - sprite.width / 2
                y = 64 - vy / length * distance * t - height * arc - sprite.height / 2
                put(canvas, sprite, x, y)
            if frame == 2:
                draw = ImageDraw.Draw(canvas)
                draw.ellipse((50, 50, 78, 78), outline=(255, 185, 65, 255), width=4)
        images.append(canvas)
    return images, [100, 100] + [80] * 15 + [600]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('view', choices=('straight', 'diagonal'))
    parser.add_argument('kind', choices=('walk', 'shoot', 'hit', 'fall-forward', 'fall-backward', 'loose-helmet', 'grenade', 'mortar', 'vehicle'))
    parser.add_argument('--crutch', action='store_true')
    args = parser.parse_args()
    if args.crutch and args.kind in ('grenade', 'mortar', 'vehicle'):
        parser.error('blast previews use the shared part art')
    images, delays = explosion(args.view, args.kind) if args.kind in ('grenade', 'mortar', 'vehicle') else ordinary(args.view, args.crutch, args.kind)
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / (args.view + '-' + ('crutch-' if args.crutch else '') + args.kind + '.gif')
    finish(images, delays, path)
    print(path.relative_to(ROOT), path.stat().st_size)


if __name__ == '__main__':
    main()
