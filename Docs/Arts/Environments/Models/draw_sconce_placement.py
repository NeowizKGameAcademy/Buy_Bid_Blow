from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import math, json, html

OUT = Path(__file__).resolve().parents[1]
W,H=2400,1800
BG='#f4f1e9'; INK='#253e3b'; MUT='#78847d'; GOLD='#bb7925'; TEAL='#167c86'
im=Image.new('RGB',(W,H),BG); d=ImageDraw.Draw(im)
svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',f'<rect width="{W}" height="{H}" fill="{BG}"/>']
def text(x,y,t,s=24,c=INK,b=False):
 f=ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf' if b else 'C:/Windows/Fonts/malgun.ttf',s)
 d.text((x,y),t,font=f,fill=c)
 svg.append(f'<text x="{x}" y="{y+s}" font-family="Malgun Gothic,sans-serif" font-size="{s}" fill="{c}" font-weight="{700 if b else 400}">{html.escape(t)}</text>')
def line(p,c=MUT,w=2):
 d.line(p,fill=c,width=w)
 svg.append(f'<polyline points="{" ".join(f"{x:.2f},{y:.2f}" for x,y in p)}" fill="none" stroke="{c}" stroke-width="{w}"/>')
def poly(p,fill,stroke=MUT,w=2):
 d.polygon(p,fill=fill); line(p+[p[0]],stroke,w)
 svg.append(f'<polygon points="{" ".join(f"{x:.2f},{y:.2f}" for x,y in p)}" fill="{fill}" stroke="{stroke}" stroke-width="{w}"/>')
def box(x,y,w,h,fill,stroke=MUT):poly([(x,y),(x+w,y),(x+w,y+h),(x,y+h)],fill,stroke)
def circle(x,y,r,fill,stroke=MUT,w=2):
 d.ellipse((x-r,y-r,x+r,y+r),fill=fill,outline=stroke,width=w)
 svg.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill or "none"}" stroke="{stroke}" stroke-width="{w}"/>')
def arc(cx,cy,rx,ry,a,b):return [(cx+rx*math.cos(a+(b-a)*i/80),cy+ry*math.sin(a+(b-a)*i/80)) for i in range(81)]
def mapping(cx,cy,s):return lambda x,y:(cx+x*s,cy-y*s)
f=mapping(510,662,20);g=mapping(1320,780,25)
outline=arc(2,5.5,12,12,0,math.pi/2)+arc(-2,5.5,12,12,math.pi/2,math.pi)+arc(-2,-13.5,12,9,math.pi,1.5*math.pi)+arc(2,-13.5,12,9,1.5*math.pi,2*math.pi)
upper=arc(2,5.5,12,12,0,math.pi/2)+arc(-2,5.5,12,12,math.pi/2,math.pi)+[(-14,5.5),(-3.5,5.5)]+[(3.5*math.cos(t),5.5-2.8*math.sin(t)) for t in [math.pi-i*math.pi/60 for i in range(61)]]+[(14,5.5)]
def worldpoly(p,m,fill,stroke=MUT):poly([m(*v) for v in p],fill,stroke)
def rect(x,y,w,h,m,fill):worldpoly([(x-w/2,y+h/2),(x+w/2,y+h/2),(x+w/2,y-h/2),(x-w/2,y-h/2)],m,fill)
text(65,42,'BUY · BID · BLOW  /  WALL SCONCE PLAN',42,b=True)
text(65,108,'벽등 배치 제안  |  1층 8개 + 2층 6개 = 총 14개',31,b=True)
text(65,157,'28 × 40m 홀 · A21 / A23 도면 + A30 저장 모델 대조 · 2026.10.14 · 게임 배경 배치 검토도',21,c=MUT)
line([(65,205),(2335,205)])
text(70,230,'01  1층 로비 · 접수 / 바닥 Z=0',28,b=True)
text(940,230,'02  2층 경매장 / 바닥 Z=4m',28,b=True)
worldpoly(outline,f,'#fffdf8');worldpoly(upper,f,'#e4e7de')
worldpoly(upper,g,'#fffdf8')
for m in (f,g):
 for sign in (-1,1):
  rect(sign*7.3,3.7,7,3.2,m,'#d6d5ca');rect(sign*12.4,3.8,3.2,3.4,m,'#d6d5ca')
  for i in range(1,24):
   x=sign*(3.8+7*i/24);line([m(x,2.1),m(x,5.3)],'#a5aaa0',1)
 for x,y in [(-6.695,16.162),(6.695,16.162)]+([(-13.52,-.4),(13.52,-.4),(-6.695,-21.162),(6.695,-21.162)] if m==f else []):rect(x,y,.96,.96,m,INK)
text(385,430,'중층 하부',23,c=MUT);text(347,467,'이번 배치에서는 제외',18,c=MUT)
for lo,hi in [(-4.82,-2.9),(-8.32,-6.4)]:line([f(-14,lo),f(-14,hi)],BG,9)
line([f(14,-7.98),f(14,-2.9)],BG,10)
text(804,744,'주출입구',19);text(114,746,'남/여 WC',19)
circle(*f(0,-7),110,'#fffdf8','#baa16d',3)
text(438,765,'중앙 문양',21);text(449,800,'로비 비움',18,c=MUT)
xs=[-12+24*i/80 for i in range(81)]
front=[(x,-13.7-.020*x*x) for x in xs]
back=[(x,y-1.2) for x,y in front]
worldpoly(front+back[::-1],f,'#b69a75')
text(430,966,'곡선 접수대',21)
text(409,1020,'직원 전용 공간',20)
for a,b in [((-4.5,-21.6),(4.5,-21.6)),((-13.1,-13),(-7.7,-20.1)),((13.1,-13),(7.7,-20.1))]:line([f(*a),f(*b)],'#919ba3',4)
line([f(-1.98,-22.5),f(1.98,-22.5)],BG,10)
text(440,1130,'직원 출입문',19)
text(72,1180,'회색 굵은 선 = 높은 TV 3대의 평면 투영',20,c=MUT)
text(72,1215,'TV 하단 Z=5.50m / 벽등 상단 Z=2.90m',20,c=MUT)
text(1090,536,'경매장 · 객석 영역',23,c=MUT)
text(1110,578,'기존 가구 배치 유지',20,c=MUT)
text(1145,785,'계단 / 중층 난간',20,c=MUT)
placements=[]
def lamp(id,x,y,m,color,dx,dy,level):
 px,py=m(x,y);circle(px,py,9,color,color)
 lx,ly=px+dx,py+dy
 line([(px,py),(lx+24,ly+15)],color,2)
 box(lx,ly,61,32,BG,BG);text(lx+3,ly+1,id,21,color,True)
 placements.append({'id':id,'floor':level,'wall_reference_x':round(x,3),'wall_reference_y':round(y,3),'center_world_z':2.5 if level==1 else 6.5,'root_world_z':2.1 if level==1 else 6.1,'note':'벽 기준점 제안. 실제 패널 표면 및 몰딩 간격에 맞춰 위치/회전 조정.'})
for i,(x,y) in enumerate([(14,-2),(14,-9.5),(14,-12),(-14,-2),(-14,-9.5),(-14,-12),(3.3,-13.5-9*math.sqrt(1-(1.3/12)**2)),(-3.3,-13.5-9*math.sqrt(1-(1.3/12)**2))],1):
 dx=25 if x>10 else -87 if x<-10 else 12 if x>0 else -75
 dy=-18 if i<7 else 38
 lamp('L'+str(i).zfill(2),x,y,f,GOLD,dx,dy,1)
for i,(sign,angle) in enumerate([(1,20),(1,43),(1,78),(-1,78),(-1,43),(-1,20)],1):
 a=math.radians(angle);x=sign*(2+12*math.cos(a));y=5.5+12*math.sin(a)
 lamp('U'+str(i).zfill(2),x,y,g,TEAL,15 if sign>0 else -76,-43,2)
text(950,845,'U01–U06  |  곡면 벽을 따라 3개씩 대칭 배치',23,b=True)
text(950,890,'기둥은 피하고 벽 패널 중앙에 정렬합니다.',21)
text(950,932,'등의 앞면은 곡면 벽의 안쪽 수직 방향으로 회전.',21)
text(950,974,'계단 경사벽·난간에는 추가하지 않습니다.',21)
text(950,1016,'측면의 인접 등 간격은 약 4.8~7.3m입니다.',21)
text(950,1070,'방향: 위쪽 +Y / 오른쪽 +X',20,c=MUT)
for x,y,s in [(80,1280,20),(950,1150,25)]:
 line([(x,y),(x+5*s,y)],INK,4)
 for i in range(6):line([(x+i*s,y-5),(x+i*s,y+5)],INK,2)
 text(x,y-30,'0',16);text(x+5*s-12,y-30,'5m',16)
line([(1770,235),(1770,1300)],'#c7ccc3')
text(1820,235,'03  설치 높이',28,b=True)
text(1820,282,'각 층의 바닥 기준',20,c=MUT)
base=890;scale=160
box(1880,335,330,555,'#ece7dc','#d4cbbb')
line([(1840,base),(2260,base)],INK,4)
text(1840,910,'바닥 ±0.00',20)
sx=2040;top=base-2.9*scale;bottom=base-2.1*scale
box(sx-16,top,32,bottom-top,'#f7f0d7',GOLD)
box(sx-22,top,44,10,GOLD,GOLD);box(sx-22,bottom-10,44,10,GOLD,GOLD)
line([(sx,top+10),(sx,bottom-10)],GOLD,2)
for height,label in [(2.9,'상단 2.90m'),(2.5,'중심 2.50m'),(2.1,'하단 2.10m')]:
 yy=base-height*scale;line([(2065,yy),(2110,yy)],GOLD,2);text(2120,yy-16,label,19,GOLD)
text(1840,972,'실측 벽등  H 0.80 × W 0.22m',20,b=True)
text(1840,1012,'깊이 약 0.22m / 원본 크기 유지',19)
text(1840,1070,'1층 중심 Z=2.50 / 루트 Z=2.10',19)
text(1840,1109,'2층 중심 Z=6.50 / 루트 Z=6.10',19)
text(1840,1170,'루트 원점 = 벽등 맨 아래',19,c=MUT)
text(1840,1207,'부착판을 실제 벽 표면에 맞춤',19,c=MUT)
line([(65,1330),(2335,1330)])
text(70,1365,'배치 위치표 / 좌표는 벽 기준 제안값 (m)',26,b=True)
rows=[('L01 · L04','주출입구 / WC 위쪽 끝','X=±14.00, Y=-2.00'),('L02 · L05','주출입구 / WC 아래쪽 끝','X=±14.00, Y=-9.50'),('L03 · L06','접수 구역으로 이어지는 측벽','X=±14.00, Y=-12.00'),('L07 · L08','직원 출입문 양옆','X=±3.30, Y=-22.45')]
for i,(a,b,c) in enumerate(rows):
 y=1420+i*49;text(75,y,a,21,GOLD,True);text(240,y,b,20);text(637,y,c,20)
for i,(a,b,c) in enumerate([('U01 · U06','계단 도착 측 곡면','X=±13.28, Y=+9.60'),('U02 · U05','객석 측면 곡면','X=±10.78, Y=+13.68'),('U03 · U04','경매장 정면 곡면','X=±4.49, Y=+17.24')]):
 y=1420+i*49;text(1260,y,a,21,TEAL,True);text(1430,y,b,20);text(1840,y,c,20)
text(70,1660,'설계 의도: 출입구를 밝히고 → 측벽으로 접수 구역을 연결 → 2층 곡면에 일정한 장식 리듬을 만듭니다.',22,b=True)
text(70,1710,'배치 제안 · 모델 미적용. 최종 부착 시 몰딩·문틀·화분 잎 간섭과 빛 번짐을 확인하고 패널 중앙으로 소폭 조정하세요.',20,c=MUT)
svg.append('</svg>')
stem='A31_Wall_Sconce_Placement_Plan'
im.save(OUT/(stem+'.png'))
(OUT/(stem+'.svg')).write_text('\n'.join(svg),encoding='utf-8')
(OUT/(stem+'.json')).write_text(json.dumps({'status':'proposal_not_applied','units':'m','source_plans':['A21_Interior_Layout_Proposal_A','A23_Reception_Zoning_Plan'],'checked_model':'BBB_Hall_A30_StaffMedallion.blend','sconce':'BBB_Vertical_Wall_Sconce.blend','placements':placements},ensure_ascii=False,indent=2),encoding='utf-8')
print(OUT/(stem+'.png'))

