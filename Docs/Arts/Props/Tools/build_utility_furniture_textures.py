"""Editable lettering is rendered into textures; no text meshes are needed."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import random
ROOT=Path(__file__).resolve().parents[4]
OUT=ROOT/'Assets/Project/Content/Props/FurnitureUtilityPack/Textures'
OUT.mkdir(parents=True,exist_ok=True)
GOLD=(193,158,89);TEAL=(30,63,62);PAPER=(230,219,190)
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
serif=lambda n:ImageFont.truetype('C:/Windows/Fonts/georgia.ttf',n)
def diamond(d,x,y,r,col):d.polygon([(x,y-r),(x+r*.6,y),(x,y+r),(x-r*.6,y)],fill=col)
def heading(d,x,y,text,size,col):d.text((x,y),text,font=font(size),anchor='mt',fill=col)
def make_sign(kind,blank=False):
    w,h=(512,1024) if kind=='A' else (1024,640)
    bg,ink=(TEAL,GOLD) if kind=='A' else (PAPER,(111,78,31))
    im=Image.new('RGB',(w,h),bg);d=ImageDraw.Draw(im)
    d.rectangle((18,18,w-19,h-19),outline=ink,width=2)
    d.rectangle((28,28,w-29,h-29),outline=ink,width=1)
    diamond(d,w/2,90 if kind=='A' else 70,20,ink)
    if kind=='A':
        heading(d,w/2,155,'INFORMATION',33,ink)
        heading(d,w/2,211,'경매장 이용 안내',29,ink)
        for y in [288,807]:d.line((68,y,w-68,y),fill=ink,width=2)
        if not blank:
            for y,title,sub in [(339,'매칭 대기','중앙 홀'),(482,'경매장','중층'),(625,'거래 안내','접수대')]:
                heading(d,w/2,y,title,34,ink);heading(d,w/2,y+55,sub,24,(191,192,159))
            heading(d,w/2,863,'직원에게 문의해 주세요',22,ink)
    else:
        heading(d,w/2,121,'WELCOME',39,ink);heading(d,w/2,182,'경매장 이용 안내',30,ink)
        d.line((126,258,w-126,258),fill=ink,width=2)
        if not blank:
            for y,text in [(309,'매칭 대기  ·  중앙 홀'),(382,'경매장  ·  중층'),(455,'거래 안내  ·  접수대')]:heading(d,w/2,y,text,29,ink)
    diamond(d,w/2,h-70,12,ink)
    im.save(OUT/f'InformationBoard_{kind}{"_Blank" if blank else "_Text"}.png')
for kind in ['A','B']:
    make_sign(kind);make_sign(kind,True)
im=Image.new('RGB',(512,512),PAPER);d=ImageDraw.Draw(im)
colors=[(39,62,52),(91,39,29),(104,70,34),(30,49,58),(69,31,42),(65,65,38)]
for i,col in enumerate(colors):
    x=i*64;d.rectangle((x,0,x+63,255),fill=col)
    d.rectangle((x+6,9,x+57,246),outline=GOLD,width=1)
    for y in [26,47,190,215]:d.line((x+4,y,x+59,y),fill=GOLD,width=2)
    for y in [93,151]:diamond(d,x+32,y,10,GOLD)
    d.text((x+32,64),'VOL',font=serif(10),anchor='mt',fill=GOLD)
    d.text((x+32,116),str(i+1),font=serif(18),anchor='mt',fill=GOLD)
# Leaf chart: opaque polygon meshes supply the silhouette; veins are in the texture.
d.rectangle((384,0,511,255),fill=(34,63,28))
d.line((448,16,448,240),fill=(112,134,59),width=4)
for y in range(45,230,30):
    d.line((448,y,402,y-30),fill=(71,105,41),width=2);d.line((448,y,494,y-30),fill=(71,105,41),width=2)
marble=Image.open(ROOT/'Docs/Arts/Props/MaterialSources/FurnitureUtilityPack/green marble3_BaseColor.png').convert('RGB').resize((256,256),Image.Resampling.LANCZOS)
im.paste(marble,(0,256))
d=ImageDraw.Draw(im);d.rectangle((256,256,511,511),fill=(39,61,47))
rng=random.Random(12)
for _ in range(12000):
    x,y=rng.randrange(256,512),rng.randrange(256,512);v=rng.randrange(-4,5);d.point((x,y),fill=(39+v,61+v,47+v))
im.save(OUT/'Furniture_Accents_BaseColor.png')
print(OUT)
