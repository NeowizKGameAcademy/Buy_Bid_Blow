"""Deterministic 1K trim/palette sheet for the three low-poly lounge props."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math, random

ROOT = Path(__file__).resolve().parents[4]
OUT = ROOT / 'Assets/Project/Content/Props/LoungeAccessories/Textures'
OUT.mkdir(parents=True, exist_ok=True)
im = Image.new('RGB', (1024, 1024)); px = im.load()
mask = Image.new('RGBA', im.size, (0, 0, 0, 70)); md = ImageDraw.Draw(mask)
rng = random.Random(29)
for x in range(512):
    for y in range(1024):
        wave = math.sin(x*.11 + 1.8*math.sin(y*.008) + .3*math.sin(y*.033))
        fine = math.sin(x*.57 + .9*math.sin(y*.014))
        grain = 1 + .14*wave + .05*fine + rng.uniform(-.025,.025)
        px[x,y] = tuple(int(c*grain) for c in (85,51,31))
regions = {'brass':((512,0,768,256),(161,118,53),(190,0,0,165)),
           'velvet':((768,0,1024,256),(73,16,28),(0,0,0,32)),
           'charcoal':((512,256,768,512),(29,35,39),(0,0,0,48)),
           'ceramic':((768,256,1024,512),(205,195,158),(0,0,0,170)),
           'paper':((512,512,768,896),(224,210,174),(0,0,0,30)),
           'clock':((768,512,1024,768),(234,220,180),(0,0,0,80)),
           'cream':((768,768,1024,896),(224,210,174),(0,0,0,40)),
           'dark':((512,896,768,1024),(22,17,13),(0,0,0,60))}
d=ImageDraw.Draw(im)
for rect,col,packed in regions.values():
    d.rectangle((rect[0],rect[1],rect[2]-1,rect[3]-1),fill=col)
    md.rectangle((rect[0],rect[1],rect[2]-1,rect[3]-1),fill=packed)
font=lambda size:ImageFont.truetype('C:/Windows/Fonts/georgia.ttf',size)
def text_center(y,text,size,col=(83,39,29)):
    d.text((640,y),text,font=font(size),anchor='mt',fill=col)
d.rectangle((526,528,754,880),outline=(141,103,49),width=2)
text_center(550,'AUCTION',29); text_center(589,'BUY · BID · BLOW',12)
d.rectangle((548,631,732,782),fill=(82,28,37))
d.polygon([(569,676),(640,649),(711,676)],outline=(207,178,118),width=3)
d.line((570,680,710,680),fill=(207,178,118),width=3)
for x in [584,613,642,671,700]: d.rectangle((x,690,x+5,745),fill=(207,178,118))
for y in [752,759]:d.line((568,y,713,y),fill=(207,178,118),width=3)
text_center(806,'SEASON CATALOGUE',13);text_center(836,'COLLECT · TRADE · DISCOVER',9)
cx,cy=896,640
d.ellipse((787,531,1005,749),outline=(143,99,40),width=5)
for i in range(60):
    a=i*math.tau/60; r=94 if i%5 else 86
    d.line((cx+r*math.sin(a),cy-r*math.cos(a),cx+101*math.sin(a),cy-101*math.cos(a)),fill=(61,47,31),width=2 if i%5 else 4)
for i in range(1,13):
    a=i*math.tau/12;d.text((cx+70*math.sin(a),cy-70*math.cos(a)),str(i),font=font(17),anchor='mm',fill=(61,47,31))
d.line((cx,cy,cx-39,cy-40),fill=(50,40,29),width=5)
d.line((cx,cy,cx+54,cy-35),fill=(50,40,29),width=3)
d.ellipse((cx-5,cy-5,cx+5,cy+5),fill=(143,99,40))
im.save(OUT/'Lounge_Atlas_BaseColor.png')
mask.save(OUT/'Lounge_Atlas_MetallicSmoothness.png')
print(OUT)
