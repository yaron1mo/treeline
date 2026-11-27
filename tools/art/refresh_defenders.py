from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import base64, io
root = Path(__file__).resolve().parents[2]
assets = root / 'unity/Assets/Resources/Art/Units'
out = root / 'docs/design/visual/refresh'
out.mkdir(parents=True, exist_ok=True)
def load(name):
    return Image.open(io.BytesIO(base64.b64decode((assets / name).read_text()))).convert('RGBA')
base = load('infantry_regular_idle.txt')
# Reuse the PR 8 uniform and overhead proportions. Replace the pig head and ears.
def defender(level):
    im = base.copy()
    d = ImageDraw.Draw(im)
    # Cover ears and snout with a dark helmet rim and a small exposed face.
    d.polygon([(12,25),(18,23),(22,31),(20,39),(14,38),(11,34)], fill='#414a37')
    d.polygon([(46,23),(51,26),(53,34),(49,38),(44,37),(42,31)], fill='#414a37')
    d.polygon([(23,36),(40,36),(40,45),(37,48),(27,48),(23,44)], fill='#242e28')
    skin = '#cfb894' if level < 2 else '#ded8bd'
    d.polygon([(25,37),(38,37),(38,43),(36,46),(28,46),(25,43)], fill=skin)
    d.rectangle((25,37,38,38), fill='#3a4333')
    d.line([(27,40),(30,40)], fill='#38382f', width=1)
    d.line([(34,40),(37,40)], fill='#38382f', width=1)
    if level == 1:
        d.polygon([(27,43),(37,43),(36,47),(29,47)], fill='#55483c')
        d.line([(26,41),(28,42)], fill='#dfc8ab')
        d.rectangle((26,28,38,30), fill='#343c31')
        d.rectangle((27,28,30,29), fill='#9ba38a')
        d.rectangle((34,28,37,29), fill='#9ba38a')
        d.rectangle((43,43,46,48), fill='#343c31')
        d.line([(45,43),(46,38)], fill='#242e28')
    if level == 2:
        d.rectangle((27,40,30,42), fill='#292f29')
        d.rectangle((34,40,37,42), fill='#292f29')
        d.point((32,43), fill='#554b3c')
        d.line([(29,45),(35,45)], fill='#554b3c')
        d.line([(28,47),(37,47),(39,45)], fill='#a89a80', width=2)
        d.rectangle((14,43,18,47), fill='#ded8bd')
        d.rectangle((15,44,17,46), fill='#514538')
        d.point((19,45), fill='#ded8bd')
    # Small fictional embroidered patch; no text at this scale.
    d.rectangle((39,47,42,50), fill='#272e27')
    d.rectangle((40,48,41,49), fill=('#c8b57d' if level < 2 else '#d8d1b8'))
    if level == 1: d.rectangle((20,47,23,51), fill='#6d6b4d')
    if level == 2:
        d.point((40,48), fill='#3b302c')
        d.point((41,49), fill='#3b302c')
    return im
images = [defender(i) for i in range(3)]
for name, im in zip(('recruit','experienced','veteran'), images):
    b = io.BytesIO(); im.save(b, format='PNG', optimize=True)
    (assets / ('defender_' + name + '.txt')).write_text(base64.b64encode(b.getvalue()).decode()+'\n')
    (assets / ('defender_' + name + '.txt.meta')).write_text('fileFormatVersion: 2\nguid: ' + __import__('hashlib').md5(('treeline-defender-'+name).encode()).hexdigest() + '\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
# Compare the existing rifle-squad tile and the new art at native size and 4x.
from unit_atlas import unpack
old = unpack().crop((288,48,336,96))
pigs = [load('infantry_'+n+'_idle.txt') for n in ('recruit','regular','special_forces')]
sheet = Image.new('RGB',(1120, 6*310+80),'#d4ccb7')
d = ImageDraw.Draw(sheet)
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 18)
d.text((20,12),'PR #8 / refresh: 1x and 4x',font=font,fill='#202821')
for i,(label,before,after) in enumerate([(n,p,p) for n,p in zip(('Pig recruit (kept)','Pig regular (kept)','Pig special forces (kept)'),pigs)] + [(n,old,im) for n,im in zip(('Defender recruit','Defender experienced','Defender veteran'),images)]):
    y=55+i*310
    d.text((20,y),label,font=font,fill='#202821')
    for j,im in enumerate((before,after)):
        x=40+j*520
        d.text((x,y+25),'PR #8' if j==0 else 'Refresh',font=font,fill='#202821')
        sheet.paste(im,(x,y+53),im)
        big=im.resize((im.width * 4, im.height * 4),Image.Resampling.NEAREST)
        sheet.paste(big,(x+100,y+45),big)
sheet.save(out/'before-after.png')
