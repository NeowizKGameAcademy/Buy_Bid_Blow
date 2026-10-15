from PIL import Image,ImageDraw,ImageFont
import math,json
from pathlib import Path
P=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Environments')
im=Image.new('RGB',(2400,1700),'#f4f1e9');d=ImageDraw.Draw(im)
font='C:/Windows/Fonts/malgun.ttf';bold='C:/Windows/Fonts/malgunbd.ttf'
def text(x,y,s,n=24,c='#253c38',b=False):d.text((x,y),s,font=ImageFont.truetype(bold if b else font,n),fill=c)
def line(ps,c='#53645d',w=3):d.line(ps,fill=c,width=w)
def arrow(ps,c='#31888e',w=6):
 line(ps,c,w);a,b=ps[-2:];t=math.atan2(b[1]-a[1],b[0]-a[0]);d.polygon([b,(b[0]-18*math.cos(t-.45),b[1]-18*math.sin(t-.45)),(b[0]-18*math.cos(t+.45),b[1]-18*math.sin(t+.45))],fill=c)
text(75,45,'BUY · BID · BLOW  /  INTERIOR LAYOUT',42,b=True)
text(78,105,'가구 배치 제안 A  —  1층 거래소 · 2층 경매장',32)
text(78,151,'A20 홀 기준 28 × 40m  |  단위 m  |  2026.10.14  |  제안 도면 · 모델 배치 전',21,c='#69766c')
line([(75,198),(2320,198)],'#b4bdb3',2)
def mapper(cx,cy,s):return lambda x,y:(cx+x*s,cy-y*s)
f=mapper(550,650,22);g=mapper(1740,950,36)
def poly(points,m,fill,outline='#66716b',w=2):d.polygon([m(*p) for p in points],fill=fill);line([m(*p) for p in points+[points[0]]],outline,w)
def rect(x,y,w,h,m,fill,outline='#6f5541'):
 p=m(x-w/2,y+h/2);q=m(x+w/2,y-h/2);d.rectangle([p,q],fill=fill,outline=outline,width=2)
def arc(cx,cy,rx,ry,a,b):return [(cx+rx*math.cos(t),cy+ry*math.sin(t)) for t in [a+(b-a)*i/80 for i in range(81)]]
out=arc(2,5.5,12,12,0,math.pi/2)+arc(-2,5.5,12,12,math.pi/2,math.pi)+arc(-2,-13.5,12,9,math.pi,1.5*math.pi)+arc(2,-13.5,12,9,1.5*math.pi,2*math.pi)
upper=arc(2,5.5,12,12,0,math.pi/2)+arc(-2,5.5,12,12,math.pi/2,math.pi)+[(-14,5.5),(-3.5,5.5)]+[(3.5*math.cos(t),5.5-2.8*math.sin(t)) for t in [math.pi-i*math.pi/60 for i in range(61)]]+[(14,5.5)]
text(88,220,'01   1F 거래 · 상담 / ±0.00',29,b=True)
poly(out,f,'#fffdf8')
poly(upper,f,'#e6e8df','#b5bcb3')
text(*f(-7,11),'중층 하부 · 예비 공간',22,c='#7b867c')
text(*f(-6.5,8.7),'고정 가구 없이 여유 확보',18,c='#7b867c')
text(1180,220,'02   2F 경매 / +4.00',29,b=True)
poly(upper,g,'#fffdf8')
# Fixed stairs and columns.
for m in [f,g]:
 for sign in [-1,1]:
  rect(sign*7.3,3.7,7,3.2,m,'#dedbd2','#727c73');rect(sign*12.4,3.8,3.2,3.4,m,'#d2d0c6','#727c73')
  for i in range(1,24):x=sign*(3.8+7*i/24);line([m(x,2.1),m(x,5.3)],'#a2a59b',1)
  arrow([m(sign*4.3,3.7),m(sign*10.4,3.7)],'#65786b',3)
  text(*m(sign*8.3,3.5),'UP',15)
 for x,y in [(13.52,-.4),(-13.52,-.4),(6.695,16.162),(-6.695,16.162),(-6.695,-21.162),(6.695,-21.162)]:
  if m==g and y<2:continue
  rect(x,y,.96,.96,m,'#42544c','#42544c')
# Door locations on 1F.
for lo,hi,label in [(-4.82,-2.9,'남'),(-8.32,-6.4,'여')]:
 line([f(-14,lo),f(-14,hi)],'#f4f1e9',10);line([f(-14,hi),f(-12.2,hi)],'#586a60',2);text(*f(-17,hi),label+' WC',18)
line([f(14,-7.98),f(14,-2.9)],'#f4f1e9',12)
text(*f(14.8,-3.6),'주출입구',22,b=True)
arrow([f(17,-5.44),f(12,-5.44),f(10,-2),f(4.1,.4),f(4.1,2.0)])
arrow([f(10,-2),f(-3,.4),f(-4.1,2)])
# Medallion.
a=f(-5.5,-1.5);b=f(5.5,-12.5);d.ellipse([a,b],outline='#b58d51',width=6)
text(*f(-3.6,-5.2),'중앙 문양 Ø11m',23,b=True)
text(*f(-3.7,-7),'오픈 로비 · 집결 공간',19)
text(*f(-3.4,-8.5),'가구 배치 없음',18,c='#997744')
placements=[]
def add(kind,x,y,z,tag):placements.append(dict(asset=kind,x=x,y=y,z=z,label=tag))
def desk(x,y,m,z):
 rect(x,y,2.4,.74,m,'#a98762');rect(x,y,2.2,.55,m,'#923f47')
 for dx in [-.58,.58]:rect(x+dx,y-.85,.51,.51,m,'#a3474e');add('chair',x+dx,y-.85,z,'북쪽(+Y)을 향함')
 add('desk',x,y,z,'북쪽(+Y)을 향함')
def plant(x,y,m,z,large=True):
 if large:rect(x,y,.99,.99,m,'#a4815b')
 p=m(x,y);r=10 if m==f else 16;d.ellipse((p[0]-r,p[1]-r,p[0]+r,p[1]+r),fill='#5b8252',outline='#365b35',width=2)
 add('large_planter' if large else 'medium_planter',x,y,z,'')
def tv(x,y,m,z,vertical=False):
 rect(x,y,.14 if vertical else 2.62,2.62 if vertical else .14,m,'#1d2529','#1d2529');add('TV',x,y,z,'벽걸이 · 높이는 하단 기준')
# First-floor consultation tables toward the lower end, outside medallion.
for x in [-7.6,7.6]:
 for y in [-14.8,-18.0]:desk(x,y,f,0)
text(*f(-4.2,-16.4),'거래 · 상담 8석',22,b=True)
text(*f(-3.7,-18.0),'중앙 이동 통로 유지',18)
for x in [-13.7,13.7]:tv(x,-10.7,f,2.2,True)
text(*f(-12.6,-9.9),'TV',17);text(*f(10.5,-9.9),'TV',17)
for x,y in [(-10.9,-12.8),(10.9,-12.8),(-4.2,-20.0),(4.2,-20.0)]:plant(x,y,f,0,abs(x)>5)
# Mezzanine: 8 desks /16 chairs, north-facing. Clear central aisle 3m.
for x in [-5.4,-2.7,2.7,5.4]:
 for y in [8.3,10.9]:desk(x,y,g,4)
rect(0,9.5,3,5.6,g,'#e6f0ec','#cedfd8')
arrow([g(0,6.8),g(0,12.0)],'#31888e',5)
text(*g(-.9,9.8),'3.0m',18)
text(*g(-6.8,6.6),'객석 16석 / 단차 없는 2열',21,b=True)
# Dais area is furniture-only, no raised platform implied.
rect(-.85,14.35,1.15,.83,g,'#a57d54');rect(-.85,14.35,.7,.5,g,'#983e46');add('rostrum',-.85,14.35,4,'남쪽(-Y) 객석을 향함')
rect(1.6,14.35,.94,.82,g,'#ad8b61');rect(1.6,14.35,.65,.58,g,'#a64c54');add('lot_pedestal',1.6,14.35,4,'')
text(*g(-3.3,13.65),'경매대',20);text(*g(1.4,13.65),'전시대',20)
tv(0,17.35,g,6.1);text(*g(-2.4,16.7),'TV · 정면 벽걸이',20)
for x in [-5.2,5.2]:plant(x,14.1,g,4)
for x in [-10.9,10.9]:plant(x,7.2,g,4,False)
# Panel notes and dimension cues.
# Overall width is stated in the drawing header.
text(80,1190,'1F  |  문양 주변·문 앞·계단 입구는 비워두기',24,b=True)
text(80,1233,'TV는 좌우 벽으로 분산해 출입문과 화장실을 피합니다.',22)
text(80,1270,'상담석은 남측으로 모아 로비와 경매 이동 동선을 분리합니다.',22)
text(1180,920,'2F  |  경매대 → 객석 → 계단 순으로 시야와 동선 정리',24,b=True)
text(1180,963,'객석 중앙 통로 3.0m / 책상 열 간격 2.6m',22)
text(1180,1000,'의자 사용 영역을 뺀 열 사이 여유 약 1.1~1.4m',22)
text(1180,1037,'좌우 계단 출구와 측면 통로에는 가구를 두지 않습니다.',22)
text(1180,1074,'경매대·전시대는 기존 2층 바닥 위에 배치합니다.',22)
text(1180,1111,'TV 하단 높이 제안: 1F +2.2m / 2F 바닥에서 +2.1m',22)
line([(75,1340),(2320,1340)],'#b4bdb3',2)
text(80,1370,'제안 수량',28,b=True)
text(80,1420,'경매대 1   ·   전시대 1   ·   TV 3   ·   책상 12   ·   의자 24   ·   대형 화분 4   ·   중형 화분 4',26)
text(80,1480,'기준: A20 홀 외곽·문·계단·기둥·중앙 문양 위치 확인. 가구는 현재 제작 크기를 사용한 평면 배치 제안입니다.',22)
text(80,1520,'도면 방향: 위쪽 = 홀 후면(+Y), 오른쪽 = 주출입구(+X). 1층·2층 도면은 서로 다른 축척입니다.',22)
text(80,1560,'실제 배치 전: 앉은 시점의 TV 가시성, 문 여닫이 범위, 계단 진입부와 화분 잎의 간섭을 3D에서 최종 확인합니다.',22,c='#6b756a')
# Graphic scale bars.
for x,y,sc in [(80,1640,22),(1180,1250,36)]:
 line([(x,y),(x+5*sc,y)],'#253c38',5)
 for i in range(6):line([(x+i*sc,y-5),(x+i*sc,y+5)],'#253c38',2)
 text(x,y-35,'0',17);text(x+5*sc-15,y-35,'5m',17)
outpath=P/'A21_Interior_Layout_Proposal_A.png';im.save(outpath)
(P/'A21_Interior_Layout_Proposal_A.json').write_text(json.dumps({'status':'proposal_not_applied','units':'m','placements':placements},ensure_ascii=False,indent=2),encoding='utf-8')
print(outpath)

