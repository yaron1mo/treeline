"""Make one regular infantry GIF from the frames used by the renderer."""
import argparse
import base64
import io
from pathlib import Path
from PIL import Image
import recruit_gifs as preview

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'unity/Assets/Resources/Art/Units'
OUT = ROOT / 'docs/design/visual/units/regular'


def strip(name):
    data = base64.b64decode((ART / ('infantry_regular_' + name + '.txt')).read_text())
    return Image.open(io.BytesIO(data)).convert('RGBA')


def png_frames(name, count):
    image = Image.open(ART / ('regular_' + name + '.png')).convert('RGBA')
    cell = image.width / count
    size = cell * (.7 if name == 'diagonal_shoot' else 1.)
    y = (image.height - size) / 2
    return [image.crop((round((i + .5) * cell - size / 2), round(y),
                        round((i + .5) * cell + size / 2), round(y + size))).resize((64, 64), Image.Resampling.NEAREST)
            for i in range(count)]


def frames(name, count):
    if name.startswith('diagonal_'):
        return png_frames(name, count)
    image = strip(name)
    return [image.crop((64 * i, 0, 64 * (i + 1), 64)) for i in range(count)]


def back_frames(view, crutch):
    return png_frames(('diagonal_' if view == 'diagonal' else '') + 'backfall', 6)


def frame_name(view, crutch, pose):
    return ('diagonal_' if view == 'diagonal' else '') + pose


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('view', choices=('straight', 'diagonal'))
    parser.add_argument('kind', choices=('walk', 'shoot', 'hit', 'fall-forward', 'fall-backward', 'loose-helmet', 'grenade', 'mortar', 'vehicle'))
    args = parser.parse_args()
    preview.strip, preview.frames = strip, frames
    preview.back_frames, preview.frame_name = back_frames, frame_name
    if args.kind in ('grenade', 'mortar', 'vehicle'):
        images, delays = preview.explosion(args.view, args.kind)
    else:
        images, delays = preview.ordinary(args.view, False, args.kind)
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / (args.view + '-' + args.kind + '.gif')
    preview.finish(images, delays, path)
    print(path.relative_to(ROOT), path.stat().st_size)


if __name__ == '__main__':
    main()
