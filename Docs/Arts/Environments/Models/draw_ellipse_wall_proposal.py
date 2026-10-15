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
# Lower corners follow quarter ellipses: rx=12, ry=9, centers (+/-2,-8.5).
# Exact vertical tangency at sidewalls and horizontal tangency at bottom.
outline=[]
for cx,cy,start,ry in [(2,5.5,0,12),(-2,5.5,90,12),(-2,-8.5,180,9),(2,-8.5,270,9)]:
 for j in range(193):
  th=math.radians(start+j*90/192)
  outline.append((cx+12*math.cos(th),cy+ry*math.sin(th)))
cols=[(6.695,16.162),(-6.695,16.162),(-13.52,-.40),(-6.695,-16.162),(6.695,-16.162),(13.52,-.40)]
t(45,45,'경매장 · 거래소 | 기둥 재배치안 · 천장 아치 6방향',27)
t(45,75,'A–17 · 하단 전체 타원 가이드 재설계 / 단위 m / A–14 기록 기반 · Blender 모델 미적용',14);line(45,96,1755,96)
t(390,134,'01 평면도 — 1층·중층 중첩',20,'middle')
poly([pt(x,y) for x,y in outline], '#fafafa',2)
# mezzanine rear boundary
rear=[p for p in outline if p[1]>=5.499]
mezz=[pt(-14,5.5)]+[pt(x,y) for x,y in reversed(rear)]+[pt(14,5.5),pt(3.5,5.5)]
mezz += [pt(3.5*math.cos(i*math.pi/40),5.5-2.8*math.sin(i*math.pi/40)) for i in range(41)]
mezz += [pt(-14,5.5)];poly(mezz,'#eaeaea')
for sign in [-1,1]:
 xlo,xhi=(-12.2,-5.2) if sign<0 else (5.2,12.2)
 x,y=pt(xlo,5.3);rect(x,y,7*18,3.2*18,'white')
 for i in range(1,25):xx=x+7*18*i/25;line(xx,y,xx,y+3.2*18)
 x,y=pt(-14 if sign<0 else 12.2,5.5);rect(x,y,1.8*18,3.4*18,'#bbb')
 t(*pt(sign*8.7,1.3),'← UP' if sign<0 else 'UP →',12,'middle')
 # doors white gap + closed leaf symbol
 x,y=pt(sign*14,.0)
 if sign<0:
  for lo,hi in [(-4.76,-2.96),(-8.26,-6.46)]:
   xx,yy=pt(-14,hi);rect(xx,yy,8,(hi-lo)*18,'white');line(xx,yy,xx,yy+(hi-lo)*18);line(xx,yy,xx+26,yy)
  t(175,596,'남자 화장실',12);t(175,659,'여자 화장실',12)
 else:
  xx,yy=pt(14,-3.395);rect(xx-4,yy,8,4.09*18,'white');line(xx,yy,xx,yy+4.09*18)
  xx,yy=pt(12.81,-2.9);rect(xx,yy,1.19*18,5.08*18,'#ddd');t(676,615,'대문',13);t(676,637,'5.08 × H5.99',12)
 # dimension from nearest frame edge to new stair front
 xx=105 if sign<0 else 731;dim(xx,pt(0,2.1)[1],xx,pt(0,-2.9)[1],'5.00')
for i,(x,y) in enumerate(cols,1):
 xx,yy=pt(x,y);rect(xx-8.6,yy-8.6,17.2,17.2,'#555');t(xx+(17 if x<0 else -17),yy-13,f'P{i}',11,'middle')
t(390,350,'기존 중층 지지기둥 2개 제거',12,'middle')
for x in [-13.52,13.52]:
 xx,yy=pt(x,-.4);t(xx+(24 if x<0 else -24),yy+23,'신규',11,'middle')
t(390,300,'2F 경매장 +4.00',21,'middle');t(390,450,'돌출 폭 7.00',12,'middle');t(390,705,'1F 거래소 ±0.00',21,'middle')
dim(138,175,642,175,'28.00');dim(70,200,70,830,'35.00');dim(295,419,295,477,'3.20')
t(390,873,'■ P1–P6: 1층부터 아치까지 / 양쪽 중앙 기둥 신규 / ▒ 양쪽 플랫폼 하부 막음',12,'middle')
# roof plan
cx,cy,sc=1265,380,10
poly([pt(x,y,cx,cy,sc) for x,y in outline],w=1.7)
for fac in [.866,.5]:poly([pt(x*fac,y*fac,cx,cy,sc) for x,y in outline],w=.6)
for x,y in cols:
 r=math.hypot(x,y);line(cx+x/r*16.5,cy-y/r*16.5,cx+x*sc,cy-y*sc)
poly([(cx+16.5*math.cos(i*math.pi/8),cy+16.5*math.sin(i*math.pi/8)) for i in range(16)],'#ddd')
t(1265,134,'02 천장 평면 — 아치 6방향',20,'middle');t(1265,585,'기둥과 1:1로 연결 / 전후 끝 4개 + 양쪽 중앙 2개',13,'middle')
# transverse projection front-facing mezzanine
cx=1265;base=1000;sc=22
path(f'M{cx-308} {base}V{base-198} A308 154 0 0 1 {cx+308} {base-198}V{base}Z','#fafafa',1.7)
t(cx,616,'03 정면 투영 — 중층 및 계단 하부',20,'middle');t(cx,base-352-12,'천장 내면 +16.00',12,'middle')
rect(cx-308,base-88,616,7,'#ddd')
for sign in [-1,1]:
 lo,hi=(-14,-12.2) if sign<0 else (12.2,14)
 rect(cx+lo*sc,base-88,(hi-lo)*sc,88,'#bbb')
 poly([(cx+sign*5.2*sc,base),(cx+sign*12.2*sc,base-88),(cx+sign*12.2*sc,base)],'#ddd')
 for i in range(1,25):
  x=5.2+7*i/25;z=4*i/25;line(cx+sign*x*sc,base-z*sc,cx+sign*(x+.28)*sc,base-z*sc)
for x in [-6.695,6.695]:
 rect(cx+x*sc-7.04,base-8.84*sc,14.08,8.84*sc,'#ccc');rect(cx+x*sc-10.56,base-8.84*sc,21.12,7)
for x in [-13.52,13.52]:
 path(f'M{cx+x*sc-7.04} {base}V{base-8.84*sc}H{cx+x*sc+7.04}V{base}',dash='5 4')
line(cx-308,base-88,cx+308,base-88);t(cx,base-100,'중층 +4.00 / 기둥은 바닥 ±0.00부터 연속',13,'middle')
t(cx,1030,'실선: 뒤쪽 기둥 / 점선: 양쪽 중앙 기둥 전방 투영',13,'middle')
# lower left detail
t(390,925,'04 신규 기둥 — 계단·문 사이 배치',18,'middle')
rect(165,957,75,28,'#bbb');t(202,951,'계단 끝',12,'middle')
rect(343,960,35,35,'#555');t(360,952,'신규 기둥',12,'middle')
rect(480,957,125,28,'#ddd');t(542,951,'문틀 끝',12,'middle')
dim(240,1020,343,1020,'2.02');dim(343,1048,378,1048,'0.96');dim(378,1020,480,1020,'2.02')
t(390,1080,'5m 구간의 중앙 Y=−0.40 / 받침 기준 양쪽 여유 2.02m',13,'middle')
line(45,1120,1755,1120)
t(55,1155,'반영 사항 및 치수 기준',20)
for y,txt in [(1194,'① 기존 중층 지지기둥 2개 제거. 기존 P1·P4·P5·P8(전후방 바깥쪽 기둥 4개)와 해당 아치 제거.'),(1225,'② 전후 끝쪽 기둥 4개 유지 + 양쪽 벽 중앙 기둥 2개 신규 = 기둥 6개 / 연결 아치 6개. 번호 P1–P6으로 재정리.'),(1256,'③ 신규 기둥 중심: X=±13.52, Y=−0.40. 받침 0.96 × 0.96m. 계단·문틀 사이 5m 구간 중앙에 배치.'),(1287,'④ 모든 기둥은 1층에서 시작. 뒤쪽 기둥은 중층을 관통하여 아치로 연결. 중층 하부 중앙 공간은 개방.'),(1318,'⑤ 하단 전체를 타원 가이드로 재설계: 가로 반경 12m / 세로 반경 9m. 직선 벽과 수직 접선, 최하단과 수평 접선 연결.')]:t(55,y,txt,14)
t(55,1370,'검토: 중층 아래와 양쪽 곡면 벽의 시야가 넓어지고, 중앙 벽의 긴 빈 구간에 기둥이 생기는 배치입니다.',13)
t(55,1400,'게임 공간 배치 제안도 / 기존 A–14와 사용자 표시 기준. 최신 모델 재검증 전이며 Blender에는 아직 적용하지 않음.',13)
t(1735,1400,'A–17',25,'end');a.append('</svg>');(P/'27_Hall_Ellipse_Guided_Wall.svg').write_text(''.join(a),encoding='utf-8')

