from PIL import Image, ImageDraw, ImageFont
import random, math, sys, os
O='docs/design/visual/round3'; os.makedirs(O,exist_ok=True)
F=ImageFont.load_default(); random.seed(30)
ink='#292d24'; olive='#687349'; light='#9ba477'; skin='#d79e80'; pink='#ed98a0'; bone='#ede2ba'; cyan='#68ddd1'; orange='#ffb358'
def unit(kind, phase=0):
 im=Image.new('RGBA',(28,32)); d=ImageDraw.Draw(im); s=round(math.sin(phase*math.pi/2)); bob=abs(s)
 # Separate body parts: legs, vest, head, helmet, weapon and patch.
 d.rectangle((7,23+s,11,28+s),fill=ink); d.rectangle((16,23-s,20,28-s),fill=ink)
 d.rectangle((5,14+bob,22,24+bob),fill=ink); d.rectangle((7,15+bob,20,23+bob),fill=olive); d.rectangle((10,17+bob,17,22+bob),fill=light)
 c=pink if kind=='pig' else bone if kind=='veteran' else skin
 d.rectangle((8,7+bob,19,16+bob),fill=ink); d.rectangle((9,8+bob,18,15+bob),fill=c)
 if kind=='pig':
  d.rectangle((6,9+bob,8,12+bob),fill=pink); d.rectangle((19,9+bob,21,12+bob),fill=pink); d.rectangle((11,12+bob,17,15+bob),fill='#f3b6ae'); d.point((12,13+bob),fill=ink); d.point((16,13+bob),fill=ink)
 elif kind=='veteran':
  d.point((11,11+bob),fill=ink); d.point((16,11+bob),fill=ink); d.line((12,14+bob,16,14+bob),fill=ink); d.rectangle((22,19+bob,25,23+bob),fill=bone); d.point((23,20+bob),fill=orange)
 else:
  d.point((11,11+bob),fill=ink); d.point((16,11+bob),fill=ink)
  if kind=='experienced': d.line((11,15+bob,17,15+bob),fill='#513f32',width=2); d.rectangle((3,16+bob,5,20+bob),fill=light)
 d.rectangle((8,5+bob,19,9+bob),fill=ink); d.rectangle((9,5+bob,18,8+bob),fill=olive); d.rectangle((7,9+bob,20,10+bob),fill=light)
 d.rectangle((21,13+s,23,24+s),fill=ink); d.rectangle((22,10+s,23,14+s),fill='#777d70')
 d.rectangle((7,17+bob,9,19+bob),fill=cyan if kind!='pig' else orange)
 return im

def units():
 im=Image.new('RGB',(760,220),'#e3dfcf'); d=ImageDraw.Draw(im)
 for i,k in enumerate(['pig','recruit','experienced','veteran']):
  x=20+i*185; d.text((x,10),k.upper(),font=F,fill=ink); u=unit(k); im.paste(u,(x+8,57),u); big=u.resize((112,128),Image.Resampling.NEAREST); im.paste(big,(x+55,44),big); d.text((x+4,185),'1x     4x',font=F,fill=ink)
 im.save(O+'/pixel-units.png')
def walk():
 im=Image.new('RGB',(720,185),'#e3dfcf'); d=ImageDraw.Draw(im)
 for row,k in enumerate(['pig','veteran']):
  d.text((5,10+row*85),k.upper(),fill=ink,font=F)
  for n in range(8):
   u=unit(k,n%4).resize((56,64),Image.Resampling.NEAREST); im.paste(u,(82+n*78,6+row*85),u)
 im.save(O+'/pixel-walk-strip.png')
def phone():
 # 180x390 logical grid, scaled uniformly to 360x780.
 im=Image.new('RGB',(180,390),'#99966a'); d=ImageDraw.Draw(im)
 for i in range(950):
  x=random.randrange(180); y=random.randrange(390); d.rectangle((x,y,x+random.randrange(1,4),y+1),fill=random.choice(['#929063','#a4a071','#898c5e']))
 d.polygon([(74,0),(103,0),(97,100),(130,190),(115,230),(79,152),(64,85)],fill='#8c7958')
 for j in range(4):
  y=55+j*94
  for x in range(-5,200,13):
   yy=y+int(x*.17); d.ellipse((x,yy,x+15,yy+13),fill='#46573a'); d.ellipse((x+3,yy-3,x+12,yy+7),fill='#62744a')
 d.line((16,265,65,245,110,265),fill='#c4b483',width=5)
 for k,x,y in [('pig',90,60),('pig',105,95),('pig',103,132),('recruit',46,236),('experienced',80,249),('veteran',111,260)]:
  u=unit(k).resize((20,23),Image.Resampling.NEAREST); im.paste(u,(x,y),u)
 d.line((63,233,94,157),fill=orange,width=1); d.line((103,246,120,180),fill=cyan,width=1)
 im.resize((360,780),Image.Resampling.NEAREST).save(O+'/pixel-phone.png')
def patches():
 im=Image.new('RGB',(750,270),'#e3dfcf'); d=ImageDraw.Draw(im)
 labels=['STILL HERE','NO DAYS OFF','DEAD TIRED','BONE DRY','LAST COFFEE','NO REFUNDS']
 for i,t in enumerate(labels):
  x=10+(i%3)*245; y=10+(i//3)*130
  d.rounded_rectangle((x,y,x+225,y+110),radius=12,fill='#4e5840',outline='#222a25',width=4)
  d.ellipse((x+88,y+12,x+138,y+62),fill=bone,outline=ink,width=3); d.ellipse((x+98,y+27,x+105,y+34),fill=ink); d.ellipse((x+121,y+27,x+128,y+34),fill=ink); d.line((x+103,y+48,x+123,y+48),fill=ink,width=3)
  d.rectangle((x+15+i*5,y+24,x+30+i*5,y+34),fill=[cyan,orange,pink,'#d6c776','#a0c99b','#c5a6d5'][i]); d.text((x+65,y+78),t,fill='#f1e9c9',font=F)
 im.save(O+'/veteran-patches.png')
def painted():
 src=Image.open(r'C:\Users\Pysku\.codex\generated_images\01a21b32-bf09-75b2-ba30-dcd2e85e9ef4\exec-fb9a3d81-7157-472e-8512-16b94b6c6bb3.png').convert('RGB')
 w,h=src.size; im=Image.new('RGB',(760,220),'#e3dfcf'); d=ImageDraw.Draw(im)
 for i,k in enumerate(['PIG','RECRUIT','EXPERIENCED','VETERAN']):
  x=20+i*185; d.text((x,10),k,fill=ink,font=F); tile=src.crop((i*w//4,0,(i+1)*w//4,h)); tile.thumbnail((28,32),Image.Resampling.LANCZOS); im.paste(tile,(x+8,57)); big=tile.resize((tile.width*4,tile.height*4),Image.Resampling.NEAREST); im.paste(big,(x+55,44)); d.text((x+4,185),'1x     4x',fill=ink,font=F)
 im.save(O+'/painted-units.png')
def paintedphone():
 im=Image.open(r'C:\Users\Pysku\.codex\generated_images\01a21b32-bf09-75b2-ba30-dcd2e85e9ef4\exec-e2346b9d-5064-4f44-a857-dbf9b2368160.png').convert('RGB'); im.resize((360,780),Image.Resampling.LANCZOS).save(O+'/painted-phone.png')
{'units':units,'walk':walk,'phone':phone,'patches':patches,'painted':painted,'paintedphone':paintedphone}[sys.argv[1]]()
