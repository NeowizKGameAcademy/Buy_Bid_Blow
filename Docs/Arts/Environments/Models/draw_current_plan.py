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
# rounded rectangle plan, small local straightening near relocated doors from last edit
outline=[]
for cx,cy,start in [(2,5.5,0),(-2,5.5,90),(-2,-5.5,180),(2,-5.5,270)]:
 for j in range(25):
  th=math.radians(start+j*90/24);x=cx+12*math.cos(th);y=cy+12*math.sin(th)
  if abs(x)>12 and -9.5<y<-5.5:
   f=min(1,-y-8.5) if y<-8.5 else 0;x=math.copysign(14*(1-f)+abs(x)*f,x)
  outline.append((x,y))
cols=[(11.805,11.805),(6.695,16.162),(-6.695,16.162),(-11.805,11.805),(-11.805,-11.805),(-6.695,-16.162),(6.695,-16.162),(11.805,-11.805)]
t(45,45,'경매장 · 거래소 | 계단 확폭 · 문 5m 이격 · 관통 기둥',27)
t(45,75,'A–14 · 마지막 작업 기록 기준 재구성 / 단위 m / 최신 Blender 파일 미확인 — 모델 재검증 전',14);line(45,96,1755,96)
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
   xx,yy=pt(-14,hi);rect(xx-4,yy,8,(hi-lo)*18,'white');line(xx,yy,xx,yy+(hi-lo)*18);line(xx,yy,xx+26,yy)
  t(175,596,'남자 화장실',12);t(175,659,'여자 화장실',12)
 else:
  xx,yy=pt(14,-3.395);rect(xx-4,yy,8,4.09*18,'white');line(xx,yy,xx,yy+4.09*18)
  xx,yy=pt(13.39,-2.9);rect(xx,yy,1.19*18,5.08*18,'#ddd');t(676,615,'대문',13);t(676,637,'5.08 × H5.99',12)
 # dimension from nearest frame edge to new stair front
 xx=105 if sign<0 else 731;dim(xx,pt(0,2.1)[1],xx,pt(0,-2.9)[1],'5.00')
for i,(x,y) in enumerate(cols,1):
 xx,yy=pt(x,y);rect(xx-8.6,yy-8.6,17.2,17.2,'#555');t(xx+(17 if x<0 else -17),yy-13,f'P{i}',11,'middle')
for x in [-7.5,7.5]:xx,yy=pt(x,10.3);rect(xx-4.5,yy-4.5,9,9,'#888');t(xx,yy+21,'기존 지지기둥',10,'middle')
t(390,300,'2F 경매장 +4.00',21,'middle');t(390,450,'돌출 폭 7.00',12,'middle');t(390,705,'1F 거래소 ±0.00',21,'middle')
dim(138,175,642,175,'28.00');dim(70,200,70,830,'35.00');dim(295,419,295,477,'3.20')
t(390,873,'■ P1–P8: 1층부터 아치까지 관통 / ▒ 양쪽 플랫폼 하부 막음',12,'middle')
# roof plan
cx,cy,sc=1265,380,10
poly([pt(x,y,cx,cy,sc) for x,y in outline],w=1.7)
for fac in [.866,.5]:poly([pt(x*fac,y*fac,cx,cy,sc) for x,y in outline],w=.6)
for x,y in cols:
 r=math.hypot(x,y);line(cx+x/r*16.5,cy-y/r*16.5,cx+x*sc,cy-y*sc)
poly([(cx+16.5*math.cos(i*math.pi/8),cy+16.5*math.sin(i*math.pi/8)) for i in range(16)],'#ddd')
t(1265,134,'02 천장 평면 — 아치 8방향',20,'middle');t(1265,585,'문·계단을 피한 좌우 대칭 배치 / 방향 간격은 비균등',13,'middle')
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
for x in [-11.805,-6.695,6.695,11.805]:
 rect(cx+x*sc-7.04,base-8.84*sc,14.08,8.84*sc,'#ccc');rect(cx+x*sc-10.56,base-8.84*sc,21.12,7)
line(cx-308,base-88,cx+308,base-88);t(cx,base-100,'중층 +4.00 / 기둥은 바닥 ±0.00부터 연속',13,'middle')
t(cx,1030,'양 끝 플랫폼 하부만 폐쇄 / 중앙 중층 아래 공간 유지',13,'middle')
# lower left detail
t(390,925,'04 계단·플랫폼 치수',18,'middle')
rect(216.2,961,64.8,62,'#bbb');rect(281,961,252,58,'white')
for i in range(1,25):line(281+i*252/25,961,281+i*252/25,1019)
t(248,990,'플랫폼',13,'middle');t(248,1008,'하부 폐쇄',11,'middle');dim(281,1040,533,1040,'진행 7.00');dim(555,961,555,1019,'폭 3.20')
t(390,1080,'플랫폼: 진행 방향 1.80 / 깊이 3.40 / 바닥까지 막음',13,'middle')
line(45,1120,1755,1120)
t(55,1155,'반영 사항 및 치수 기준',20)
for y,txt in [(1194,'① 문 이격 5.00m: 새 계단 디딤판 앞선 Y=+2.10 → 가장 가까운 문틀 끝 Y=−2.90. 화장실 두 문은 함께 이동.'),(1225,'② 계단 폭 1.60 → 3.20m, 진행 길이 7.00m 유지. 계단 상단·중층 연결 위치 유지, 앞쪽으로 확폭.'),(1256,'③ 양쪽 연결 플랫폼 아래를 바닥부터 높이 3.65m까지 막음. 중층 중앙 아래 공간은 열린 상태 유지.'),(1287,'④ 새 벽기둥 8개: 전체 높이 8.84m, 받침 0.96 × 0.96m, 몸통 0.64 × 0.64m. 중층 슬래브를 관통.'),(1318,'⑤ 문 이동 구간의 벽을 국부적으로 펴고 바닥 가장자리를 보완. 본 평면의 곡면 벽·세부 접합은 기록에 따른 근사 표현.')]:t(55,y,txt,14)
t(55,1370,'자료 상태: 현재 Blender는 기본 Scene이며 최신 방 파일이 폴더에서 확인되지 않음. 이 도면은 대화의 마지막 작업 치수·좌표로 재구성.',13)
t(55,1400,'모델 실측 재검증 전 참고도 / 정확한 최종 메시와 일치 여부는 최신 .blend 파일 확보 후 확인 필요.',13)
t(1735,1400,'A–14',25,'end');a.append('</svg>');(P/'24_Hall_Current_State_Plan.svg').write_text(''.join(a),encoding='utf-8')

