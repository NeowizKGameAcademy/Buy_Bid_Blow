from pathlib import Path
import json,math,html
P=Path('Docs/Arts/Environments'); a=['<svg xmlns="http://www.w3.org/2000/svg" width="1800" height="1400" viewBox="0 0 1800 1400"><rect width="1800" height="1400" fill="white"/>']
C='#986323';B='#256e83';G='#222'
def t(x,y,s,n=17):a.append(f'<text x="{x}" y="{y}" font-family="Malgun Gothic" font-size="{n}" fill="#222">{html.escape(s)}</text>')
def path(pts,c=G,w=1.5,fill='none',close=False):a.append('<path d="M'+' L'.join(f'{x:.2f},{y:.2f}' for x,y in pts)+(' Z' if close else '')+f'" fill="{fill}" stroke="{c}" stroke-width="{w}"/>')
def rect(x,y,w,h,fill='#eee',c=G):path([(x,y),(x+w,y),(x+w,y+h),(x,y+h)],c,1,fill,True)
def dim(x,y,X,Y,label):
 path([(x,y),(X,Y)],'#777',1)
 for xx,yy in [(x,y),(X,Y)]:path([(xx-4,yy-4),(xx+4,yy+4)],'#777',1)
 t((x+X)/2+6,(y+Y)/2-8,label,14)
def post(x,y,c=C):rect(x-4,y-4,8,8,c,c)
def plan(x,y):return 370+x*17,475-y*17
pts=json.loads((P/'Models/railing_plan_source.json').read_text());curve=pts[50:]
t(45,45,'경매장 · 거래소 | 새 난간 적용 계획',30)
t(45,78,'A–19 · 현재 A18 방 모델 실측 기준 / 단위 m / 배치 제안 — 방 모델 적용 전',16)
path([(45,102),(1755,102)])
t(100,145,'01 전체 평면 — 중층·계단 난간 위치',23)
out=[]
for cx,cy,st,ry in [(2,5.5,0,12),(-2,5.5,90,12),(-2,-13.5,180,9),(2,-13.5,270,9)]:
 for i in range(33):
  q=math.radians(st+i*90/32);out.append(plan(cx+12*math.cos(q),cy+ry*math.sin(q)))
path(out,fill='#fafafa',close=True)
path([plan(x,y) for x,y,z in pts],fill='#e9e9e9',close=True)
for sign in [-1,1]:
 x0=-12.2 if sign<0 else 5.2
 xx,yy=plan(x0,5.3);rect(xx,yy,7*17,3.2*17,'white')
 for i in range(25):path([plan(x0+7*i/24,2.1),plan(x0+7*i/24,5.3)],'#aaa',.7)
 for y in [2.1,5.3]:
  path([plan(x0,y),plan(x0+7,y)],B,4)
  for j in range(5):post(*plan(x0+7*j/4,y),B)
 lo=-14 if sign<0 else 12.2;xx,yy=plan(lo,5.5);rect(xx,yy,1.8*17,3.4*17,'#ddd')
 path([plan(lo,2.1),plan(lo+1.8,2.1)],C,4)
 path([plan(sign*12.2,5.5),plan(sign*3.5,5.5)],C,4)
 for j in range(6):post(*plan(sign*(3.5+8.7*j/5),5.5))
 t(*plan(sign*9.1,.85),'← UP' if sign<0 else 'UP →',13)
path([plan(x,y) for x,y,z in curve],C,4)
for i in [0,5,10,16,22,27,32]:post(*plan(*curve[i][:2]))
t(*plan(-4,11),'2F 경매장 +4.00',20)
t(*plan(-4,-10),'1F 거래소 ±0.00',20)
t(145,880,'황갈색: 중층·플랫폼 수평 난간',16);t(145,908,'청색: 계단 양측 경사 난간',16)
t(145,936,'■ 접합 기둥 위치는 제안 / 계단 상부 진입은 개방',15)
dim(132,164,608,164,'28.00');dim(85,178,85,858,'40.00')
t(775,145,'02 중층 전면 확대 — 상부 진입 동선 유지',23)
def zp(x,y):return 1250+x*30,415-y*30
path([zp(-14,7),zp(-14,5.5)]+[zp(x,y) for x,y,z in curve]+[zp(14,5.5),zp(14,7)],fill='#eee',close=True)
for sign in [-1,1]:
 path([zp(sign*12.2,5.5),zp(sign*3.5,5.5)],C,5)
 for j in range(6):post(*zp(sign*(3.5+8.7*j/5),5.5))
 path([zp(sign*13.1,3.7),zp(sign*13.1,6.6)],B,2)
 t(*zp(sign*13.1-.6,7.5),'진입',14)
path([zp(x,y) for x,y,z in curve],C,5)
for i in [0,5,10,16,22,27,32]:post(*zp(*curve[i][:2]))
dim(*zp(-3.5,1.7),*zp(3.5,1.7),'돌출 폭 7.00')
t(810,422,'직선부: 좌우 각 8.70m → 5경간 × 약 1.74m',17)
t(810,453,'곡선부: 실제 바닥 외곽 추종 / 약 6경간으로 분할',17)
t(810,484,'01B의 고정 반경을 복제하지 않고 가로대를 외곽에 맞춰 재제작',16)
t(775,550,'03 계단 측면 — 경사 가로대 + 수직 기둥',23)
def sp(x,z):return 840+x*78,1005-z*78
stairs=[sp(0,0)]
for i in range(24):stairs.extend([sp(7*i/24,4*(i+1)/24),sp(7*(i+1)/24,4*(i+1)/24)])
stairs.extend([sp(8.8,4),sp(8.8,0)])
path(stairs,fill='#eee',close=True)
for z in [.25,1.035]:path([sp(0,z),sp(7,4+z),sp(8.8,4+z)],C,7)
for j in range(5):
 x=7*j/4;z=4*j/4
 xx,yy=sp(x,z+1.12);rect(xx-6,yy,12,1.12*78,'#b89c77',C);post(*sp(x,z+1.205))
for j in range(1,32):
 x=7*j/32;z=4*x/7
 path([sp(x,z+.32),sp(x,z+.97)],C,3)
for j in range(1,8):
 x=7+1.8*j/8;path([sp(x,4.32),sp(x,4.97)],C,3)
xx,yy=sp(8.8,5.12);rect(xx-6,yy,12,1.12*78,'#b89c77',C)
dim(*sp(0,-.45),*sp(7,-.45),'수평 진행 7.00');dim(*sp(9.35,0),*sp(9.35,4),'4.00')
t(865,593,'경사 약 29.7° / 수평 1.75m × 4경간 / 좌우 계단에 대칭 적용',16)
t(870,1090,'가로대 상단 약 1.10 / 장식 포함 1.25 — 높이는 수직 기준',16)
path([(45,1130),(1755,1130)])
t(55,1170,'적용 원칙',22)
for y,txt in [(1205,'① 01A·01B의 기둥·세로살·황동 장식과 월넛 재질 유지. 깃발은 사용하지 않음.'),(1238,'② 접합부는 기둥 하나를 공유. 모듈 전체를 늘리지 않고 가로대 길이와 세로살 개수로 맞춤.'),(1271,'③ 계단 상단은 플랫폼을 통해 중층으로 진입하므로 난간으로 가로막지 않음. 벽에 붙은 뒤쪽 외곽은 제외.'),(1304,'④ 곡선 난간은 실제 중층 메시 기준. 표시 선은 가장자리 기준선이며 기둥 받침이 바닥 안에 놓이도록 안쪽으로 조정.')]:t(55,y,txt,17)
t(55,1357,'게임 배경 모델 배치 검토도 · 제작 시 접합부와 받침 간섭 확인 · 기존 방 모델 변경 없음',15)
t(1670,1360,'A–19',25)
a.append('</svg>');(P/'30_Hall_Railing_Application_Plan.svg').write_text(''.join(a),encoding='utf-8')

