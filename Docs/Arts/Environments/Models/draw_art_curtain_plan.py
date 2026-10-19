from pathlib import Path
import json,math,html
from PIL import Image,ImageDraw,ImageFont
OUT=Path(__file__).resolve().parents[1]
W,H=2600,1900;BG='#f6f3ec';INK='#263e3b';MUT='#84928c';ART='#197c89';CUR='#a74b65'
im=Image.new('RGB',(W,H),BG);d=ImageDraw.Draw(im);svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}"><rect width="100%" height="100%" fill="{BG}"/>']
def text(x,y,t,size=23,col=INK,bold=False):
 f=ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf' if bold else 'C:/Windows/Fonts/malgun.ttf',size);d.text((x,y),t,font=f,fill=col)
 svg.append(f'<text x="{x}" y="{y+size}" font-family="Malgun Gothic" font-size="{size}" fill="{col}" font-weight="{700 if bold else 400}">{html.escape(t)}</text>')
def line(pts,col=MUT,w=2,dash=False):
 if dash:
  for a,b in zip(pts,pts[1:]):
   dist=math.dist(a,b);n=max(1,int(dist/9))
   for k in range(0,n,2):d.line([(a[0]+(b[0]-a[0])*k/n,a[1]+(b[1]-a[1])*k/n),(a[0]+(b[0]-a[0])*min(k+1,n)/n,a[1]+(b[1]-a[1])*min(k+1,n)/n)],fill=col,width=w)
 else:d.line(pts,fill=col,width=w)
 svg.append(f'<polyline points="{" ".join(f"{x:.1f},{y:.1f}" for x,y in pts)}" fill="none" stroke="{col}" stroke-width="{w}"'+(' stroke-dasharray="9 9"' if dash else '')+'/>')
def poly(pts,fill,col=MUT,w=2):
 d.polygon(pts,fill=fill);line(pts+[pts[0]],col,w);svg.append(f'<polygon points="{" ".join(f"{x:.1f},{y:.1f}" for x,y in pts)}" fill="{fill}" stroke="{col}" stroke-width="{w}"/>')
def rect(x,y,w,h,fill,col=MUT):poly([(x,y),(x+w,y),(x+w,y+h),(x,y+h)],fill,col)
def circle(x,y,r,col):
 d.ellipse((x-r,y-r,x+r,y+r),fill=col);svg.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{col}"/>')
def arc(cx,cy,rx,ry,a,b):return [(cx+rx*math.cos(a+(b-a)*i/80),cy+ry*math.sin(a+(b-a)*i/80)) for i in range(81)]
f=lambda x,y:(540+x*25,790-y*25)
g=lambda x,y:(1510+x*28,810-y*28)
outline=arc(2,5.5,12,12,0,math.pi/2)+arc(-2,5.5,12,12,math.pi/2,math.pi)+arc(-2,-13.5,12,9,math.pi,1.5*math.pi)+arc(2,-13.5,12,9,1.5*math.pi,2*math.pi)
upper=outline[:162]+[(-3.5,5.5)]+[(3.5*math.cos(t),5.5-2.8*math.sin(t)) for t in [math.pi-i*math.pi/60 for i in range(61)]]+[(14,5.5)]
def wp(pts,m,fill):poly([m(*p) for p in pts],fill)
def wr(x,y,w,h,m,fill):wp([(x-w/2,y-h/2),(x+w/2,y-h/2),(x+w/2,y+h/2),(x-w/2,y+h/2)],m,fill)
text(65,40,'BUY · BID · BLOW  /  ARTWORK & CURTAIN PLAN',42,bold=True)
text(65,106,'그림·커튼 배치도  |  아래층 + 중층',32,bold=True)
text(65,160,'A49 배치 좌표 + 기존 28 × 40m 인테리어 도면 기준 · 2026.10.16 · 단위 m',22,col=MUT)
line([(65,210),(2535,210)])
text(90,245,'01  아래층  /  바닥 Z=0',29,bold=True);text(1090,245,'02  중층  /  바닥 Z=4.00',29,bold=True)
wp(outline,f,'#fffdf8');wp(upper,f,'#e8ece5');wp(upper,g,'#fffdf8')
for m in [f,g]:
 for side in [-1,1]:
  wr(side*7.3,3.7,7,3.2,m,'#d3d4ca');wr(side*12.4,3.8,3.2,3.4,m,'#d3d4ca')
  for i in range(1,23):line([m(side*(3.8+7*i/23),2.1),m(side*(3.8+7*i/23),5.3)],'#9fa79d',1)
 for x,y in [(-6.695,16.162),(6.695,16.162)]+([(-13.52,-.4),(13.52,-.4),(-6.695,-21.162),(6.695,-21.162)] if m==f else []):wr(x,y,.96,.96,m,INK)
for lo,hi in [(-4.82,-2.9),(-8.32,-6.4)]:line([f(-14,lo),f(-14,hi)],BG,9)
line([f(14,-7.98),f(14,-2.9)],BG,10)
text(875,892,'주출입구',21);text(72,900,'WC',21)
xs=[-12+24*i/80 for i in range(81)];front=[(x,-13.7-.02*x*x) for x in xs]
wp(front+[(x,y-1.2) for x,y in front][::-1],f,'#c3ae8d');text(470,1175,'접수대',22)
text(470,1285,'직원 공간',20,col=MUT);text(450,960,'중앙 로비',24,col=MUT)
for m,z in [(f,0),(g,4)]:
 for side in [-1,1]:
  wr(side*(7 if z==0 else 8),10,4.5,4.2,m,'#eee5d7');wr(side*(7 if z==0 else 8),11.5,2.6,.95,m,'#b9c9c1')
wr(0,12,2,1.5,f,'#ccb898');text(480,550,'전시',18)
for y in [11.5,14.7]:
 pts=[(x,y+.09*x*x) for x in [-3+i*.1 for i in range(61)]];line([g(*p) for p in pts],'#a88d66',10)
text(1440,420,'기존 객석',22);wr(0,6.2,2.3,.84,g,'#b69b74');text(1455,665,'경매대',18)
for x in [-8,8]:text(*g(x-1,7.5),'휴식 공간',18,col=MUT)
inv=json.loads((OUT/'Models/A49_Placement_Inventory.json').read_text(encoding='utf-8'))
paintings=[p for p in inv if 'Painting' in p['name']]
records=[]
for i,p in enumerate(paintings,1):
 x,y,z=p['position'];m=g if z>4 else f;px,py=m(x,y);ID=f'P{i:02}'
 a=math.atan2(y+2.5,x);tx,ty=-math.sin(a),math.cos(a)
 line([m(x-tx*.8,y-ty*.8),m(x+tx*.8,y+ty*.8)],ART,8);circle(px,py,5,ART)
 dx=math.cos(a)*64;dy=-math.sin(a)*54
 if abs(x)<1:dx=-48
 lx=px+dx-22;ly=py+dy-14
 line([(px,py),(lx+25,ly+13)],ART,2);rect(lx-3,ly-2,59,31,BG,BG);text(lx,ly,ID,20,ART,True)
 records.append({'id':ID,'kind':'painting','status':'A49 existing placement','position_m':[x,y,z],'source':p['name']})
def curtain(ID,x,y,z,width,angle,m,labelpos,proposed):
 dx=math.cos(angle)*width/2;dy=math.sin(angle)*width/2
 line([m(x-dx,y-dy),m(x+dx,y+dy)],CUR,7,proposed)
 px,py=m(x,y);lx,ly=labelpos;line([(px,py),(lx+30,ly+15)],CUR,2,proposed);rect(lx-3,ly-2,66,34,BG,BG);text(lx,ly,ID,23,CUR,True)
 records.append({'id':ID,'kind':'curtain','status':'proposal_not_applied' if proposed else 'A49 existing placement','position_m':[x,y,z],'width_m':width})
curtain('C01',0,17.03,6.55,4.12,0,g,(1820,305),False)
curtain('C02',13.65,-5.44,.15,6,math.pi/2,f,(925,855),True)
curtain('C03',0,17.03,1.05,3.2,0,f,(625,295),True)
text(1090,860,'図面の上 = +Y  /  右 = +X'.replace('図面の上','도면 위').replace('右','오른쪽'),21,col=MUT)
text(1090,910,'그림은 벽면에 부착하고, 전면은 실내를 향합니다.',21)
text(1090,950,'기둥·벽등 사이 배치 / 계단과 난간 앞은 비워 둡니다.',21)
text(1090,1015,'표시 범례',26,bold=True)
line([(1095,1070),(1160,1070)],ART,7);text(1180,1054,'P01–P14  기존 그림 위치',23,ART)
line([(1095,1120),(1160,1120)],CUR,7);text(1180,1104,'C01  기존 중층 장식 커튼',23,CUR)
line([(1095,1170),(1160,1170)],CUR,7,True);text(1180,1154,'C02·C03  추가 커튼 제안 (미배치)',23,CUR)
text(1090,1230,'형태: 직사각 A를 기본으로, 타원 B는 중앙 강조용.',21)
text(1090,1270,'새 액자 A/B 모델로 교체하기 전의 위치 검토도입니다.',20,col=MUT)
line([(2020,245),(2020,1340)],'#cbd1c8')
text(2080,245,'03  설치 높이 · 범위',27,bold=True)
notes=[('그림 / 아래층','P01–P07 중심 +2.35','P13–P14 중심 +2.50'),('그림 / 중층','P08–P12 중심 +3.25','월드 높이 Z=7.25'),('C01 / 중층 뒤 벽','기존: 폭 4.12','바닥 기준 +2.55 ~ +4.69'),('C02 / 주출입구','제안: 전체 폭 약 6.00','바닥 기준 +0.15 ~ +5.10'),('C03 / 아래층 뒤 벽','제안: 전체 폭 약 3.20','바닥 기준 +1.05 ~ +3.45')]
for i,(a,b,c) in enumerate(notes):
 yy=310+i*180;text(2080,yy,a,24,CUR if a.startswith('C') else ART,True);text(2080,yy+44,b,22);text(2080,yy+82,c,21)
text(2080,1240,'커튼은 양옆으로 묶어 고정.',20)
text(2080,1275,'문 개구부·그림 면을 가리지 않음.',19)
line([(65,1390),(2535,1390)])
text(70,1420,'그림 위치표  /  X, Y = 평면 좌표 · Z = 그림 중심의 월드 높이',26,bold=True)
for i,r in enumerate(records[:14]):
 col=i//7;row=i%7;x,y,z=r['position_m'];xx=80+col*830;yy=1480+row*43
 text(xx,yy,r['id'],21,ART,True);text(xx+85,yy,f'X {x:+.2f}    Y {y:+.2f}    Z {z:.2f}',21)
text(1800,1478,'커튼 구성',24,CUR,True)
text(1800,1530,'C01: 중층 중앙 그림을 감싸는 장식 드레이프',21)
text(1800,1580,'C02: 출입구 양옆 고정 커튼 + 상부 장식',21)
text(1800,1630,'C03: 아래층 중앙 그림 양옆의 짧은 커튼',21)
text(1800,1690,'C02·C03은 문틀·천장·가구와 간섭 확인 후 설치.',19,col=MUT)
text(70,1820,'배치 검토도 · 도면만 작성했으며 블렌더 장면은 변경하지 않았습니다. 커튼 제안 치수는 모델 적용 시 조정합니다.',22,col=MUT)
svg.append('</svg>');stem='A50_Artwork_Curtain_Placement_Plan'
im.save(OUT/(stem+'.png'));(OUT/(stem+'.svg')).write_text('\n'.join(svg),encoding='utf-8');(OUT/(stem+'.json')).write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
im.save(OUT/(stem+'.pdf'),'PDF',resolution=160)
print(OUT/(stem+'.png'))

