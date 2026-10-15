from pathlib import Path
import math
P=Path('Docs/Arts/Environments');a=['<svg xmlns="http://www.w3.org/2000/svg" width="1800" height="1450" viewBox="0 0 1800 1450"><rect width="1800" height="1450" fill="white"/>']
def t(x,y,s,n=14,anchor='start'):a.append(f'<text x="{x}" y="{y}" font-family="Malgun Gothic" font-size="{n}" text-anchor="{anchor}" fill="#111">{s}</text>')
def path(d,fill='none',w=1,dash=''):a.append(f'<path d="{d}" fill="{fill}" stroke="#222" stroke-width="{w}" stroke-dasharray="{dash}"/>')
def line(x,y,X,Y,dash=''):path(f'M{x} {y}L{X} {Y}',dash=dash)
def rect(x,y,w,h,fill='#ddd'):a.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{fill}" stroke="#222"/>')
def pt(x,y,cx=390,cy=515,sc=18):return cx+x*sc,cy-y*sc
def poly(points,fill='none',w=1):path('M'+' L'.join(f'{x:.2f} {y:.2f}' for x,y in points)+'Z',fill,w)
def dim(x,y,X,Y,label):line(x,y,X,Y);line(x-4,y-4,x+4,y+4);line(X-4,Y-4,X+4,Y+4);t((x+X)/2+7,(y+Y)/2-7,label,12)

def plan(x,y):return 390+x*16,475-y*16
def outline(shift=5):
 out=[]
 for cx,cy,st,ry in [(2,5.5,0,12),(-2,5.5,90,12),(-2,-8.5-shift,180,9),(2,-8.5-shift,270,9)]:
  for j in range(97):
   th=math.radians(st+j*90/96);out.append((cx+12*math.cos(th),cy+ry*math.sin(th)))
 return out
out=outline();cols=[(6.695,16.162),(-6.695,16.162),(-13.52,-.4),(-6.695,-21.162),(6.695,-21.162),(13.52,-.4)]
t(45,45,'경매장 · 거래소 | 캡슐 길이 5m 확장안',29)
t(45,76,'A–18 · 폭 28.00 × 길이 40.00m / A–17에서 거래소 쪽으로 5.00m 연장 / 단위 m',15)
line(45,100,1755,100)
t(390,140,'01 평면도 — 길이 35 → 40m',22,'middle')
poly([plan(x,y) for x,y in out],'#fafafa',2)
rear=[p for p in out if p[1]>=5.499]
mezz=[plan(-14,5.5)]+[plan(x,y) for x,y in reversed(rear)]+[plan(14,5.5),plan(3.5,5.5)]
mezz +=[plan(3.5*math.cos(i*math.pi/40),5.5-2.8*math.sin(i*math.pi/40)) for i in range(41)]
poly(mezz+[plan(-14,5.5)],'#eaeaea')
# Dashed former lower boundary makes the exact extension visible.
oldlower=[plan(x,y) for x,y in outline(0) if y<=-8.499]
path('M'+' L'.join(f'{x:.2f} {y:.2f}' for x,y in oldlower),dash='8 6')
for sign in [-1,1]:
 x,y=plan(-12.2 if sign<0 else 5.2,5.3);rect(x,y,112,51.2,'white')
 for i in range(1,25):line(x+112*i/25,y,x+112*i/25,y+51.2)
 x,y=plan(-14 if sign<0 else 12.2,5.5);rect(x,y,28.8,54.4,'#bbb')
 t(*plan(sign*8.7,1.25),'← UP' if sign<0 else 'UP →',12,'middle')
 if sign<0:
  for lo,hi in [(-4.76,-2.96),(-8.26,-6.46)]:
   xx,yy=plan(-14,hi);rect(xx,yy,6,(hi-lo)*16,'white');line(xx,yy,xx+22,yy)
  t(*plan(-12,-4),'남자 화장실',12);t(*plan(-12,-7.5),'여자 화장실',12)
 else:
  xx,yy=plan(12.81,-2.9);rect(xx,yy,1.19*16,5.08*16,'#ddd');t(631,552,'대문',13)
 xx=124 if sign<0 else 664;dim(xx,plan(0,2.1)[1],xx,plan(0,-2.9)[1],'5.00')
for i,(x,y) in enumerate(cols,1):
 xx,yy=plan(x,y);rect(xx-7.68,yy-7.68,15.36,15.36,'#555');t(xx+(19 if x<0 else -19),yy-14,f'P{i}',12,'middle')
t(*plan(0,11),'2F 경매장 +4.00',21,'middle')
t(*plan(0,4),'돌출 폭 7.00',12,'middle')
t(*plan(0,-10),'1F 거래소 ±0.00',21,'middle')
t(*plan(0,-15),'점선: 기존 하단 외곽',13,'middle')
dim(166,174,614,174,'28.00');dim(80,195,80,835,'40.00')
dim(698,plan(0,-17.5)[1],698,plan(0,-22.5)[1],'+5.00')
t(390,880,'계단 폭 3.20 / 진행 7.00 / 문–계단 이격 5.00 유지',13,'middle')
t(390,909,'중층·계단·문·중앙 기둥 위치 유지 / 끝쪽 P4·P5만 5m 이동',13,'middle')
# Roof plan, centered on the new length midpoint Y=-2.5.
t(1270,140,'02 천장 평면 — 6방향 아치 연장',22,'middle')
def roof(x,y):return 1270+x*9,355-(y+2.5)*9
poly([roof(x,y) for x,y in out],w=1.8)
for f in [.866,.5]:poly([roof(x*f,-2.5+(y+2.5)*f) for x,y in out],w=.6)
for x,y in cols:
 dx=x;dy=y+2.5;r=math.hypot(dx,dy);line(1270+dx/r*15,355-dy/r*15,*roof(x,y))
poly([(1270+15*math.cos(i*math.pi/8),355+15*math.sin(i*math.pi/8)) for i in range(16)],'#ddd')
t(1270,562,'중앙 허브: 새 평면 중심 Y=−2.50 / 기둥 6개 유지',13,'middle')
# Longitudinal section schematic with expanded width proportional to 40m.
t(1270,620,'03 종단 개념도 — 전체 길이 40m',22,'middle')
x0=950;x1=1590;yb=956;sc=16
path(f'M{x0} {yb}V{yb-144} C{x0} {yb-225} {x0+130} {yb-256} {1270} {yb-256} C{x1-130} {yb-256} {x1} {yb-225} {x1} {yb-144} V{yb}Z','#fafafa',2)
# north mezzanine is 12m deep and remains the same size
rect(x1-12*sc,yb-64,12*sc,5.6,'#ddd')
t(1493,yb-76,'중층 +4.00',13,'middle');t(1270,685,'천장 최고 +16.00 유지',13,'middle')
t(1100,930,'거래소 확장 방향 ←',15,'middle')
dim(x0,997,x1,997,'40.00');t(1270,1040,'종단은 높이·길이 검토용 개념 투영',13,'middle')
# guide inset


t(390,966,'04 확장 원칙',21,'middle')
t(165,1006,'위쪽 곡면: 기존 반경 12m 유지',15)
t(165,1039,'아래쪽 곡면: 타원 반경 12 × 9m 유지',15)
t(165,1072,'양쪽 직선 구간: 14 → 19m (+5m)',15)
line(45,1120,1755,1120)
t(55,1161,'변경 사항',22)
for y,txt in [(1201,'① 전체 폭 28m 유지. 길이 35m → 40m. 거래소 쪽 끝 Y=−17.50 → −22.50으로 5m 이동.'),(1234,'② 곡면을 늘려 찌그러뜨리지 않고, 하단 타원 전체를 5m 이동하여 양쪽 직선 벽만 연장.'),(1267,'③ 중층·계단·대문·화장실 문과 중앙 기둥 P3·P6 위치 유지. 문–계단 5m 이격 유지.'),(1300,'④ 끝쪽 기둥 P4·P5는 곡면과 함께 Y=−21.162로 이동. 총 기둥 6개 / 아치 6방향 유지.'),(1333,'⑤ 천장 중앙 허브는 새 길이 중심 Y=−2.50에 배치 제안. 각 아치 길이는 새 평면에 맞춰 조정.')]:t(55,y,txt,15)
t(55,1400,'A–17 기반 확장 제안도 · Blender 모델에는 아직 미적용',14);t(1735,1400,'A–18',25,'end')
a.append('</svg>');(P/'29_Hall_Length_40m_Proposal.svg').write_text(''.join(a),encoding='utf-8')
