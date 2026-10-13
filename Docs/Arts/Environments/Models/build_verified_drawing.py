import json,math,collections
from pathlib import Path
P=Path('Docs/Arts/Environments');D=json.loads((P/'Models/Hall_Drawing_Measurements.json').read_text());R=json.loads((P/'Models/Hall_Roof_Centerlines.json').read_text())
a=['<svg xmlns="http://www.w3.org/2000/svg" width="1800" height="1500" viewBox="0 0 1800 1500"><rect width="1800" height="1500" fill="white"/>']
def t(x,y,s,size=15,anc='start'):a.append(f'<text x="{x}" y="{y}" font-family="Malgun Gothic" font-size="{size}" text-anchor="{anc}" fill="#111">{s}</text>')
def line(points,w=1,col='#222',dash=''):
 a.append('<polyline points="'+' '.join(f'{x:.2f},{y:.2f}' for x,y in points)+f'" fill="none" stroke="{col}" stroke-width="{w}" stroke-dasharray="{dash}"/>')
def rect(x,y,w,h,fill='#ddd'):a.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{fill}" stroke="#222"/>')
def topboundary(obj,z):
 c=collections.Counter()
 for f in obj['f']:
  vs=[obj['v'][i] for i in f]
  if all(abs(v[2]-z)<.001 for v in vs):
   for u,v in zip(vs,vs[1:]+vs[:1]):c[tuple(sorted((tuple(round(x,4) for x in u[:2]),tuple(round(x,4) for x in v[:2]))))]+=1
 return [e for e,n in c.items() if n==1]
floor=next(o for k,o in D.items() if 'Floor' in k);mezz=next(o for k,o in D.items() if 'Mezzanine' in k)
def planpt(v,cx=405,cy=525,sc=18):return (cx+v[0]*sc,cy-v[1]*sc)
def planedges(edges,cx,cy,sc,w=1,col='#222'):
 for e in edges:line([planpt(v,cx,cy,sc) for v in e],w,col)
t(45,45,'경매장 · 거래소 | 실제 Blender 모델 대조 도면',28)
t(45,76,'A–13 · 모델에서 추출한 평면·천장 중심선·단면 / 단위 m / 04A 기둥 배치와 신규 아치는 미적용',14)
line([(45,96),(1755,96)])
t(405,136,'01 현재 평면 — 중층·계단·문 실측',20,'middle')
# actual mesh top surfaces
for f in mezz['f']:
 vs=[mezz['v'][i] for i in f]
 if all(abs(v[2]-4)<.001 for v in vs):a.append('<polygon points="'+' '.join(f'{x:.2f},{y:.2f}' for x,y in [planpt(v) for v in vs])+'" fill="#eee"/>')
planedges(topboundary(floor,0),405,525,18,2)
planedges(topboundary(mezz,4),405,525,18,1.5)
for k,o in D.items():
 if 'Steps' in k or 'Landing' in k:
  zs=sorted(set(round(v[2],4) for v in o['v']))
  for z in zs:planedges(topboundary(o,z),405,525,18,.6)
 if 'Columns' in k:
  xs=[v[0] for v in o['v']];ys=[v[1] for v in o['v']];x,y=planpt((min(xs),max(ys)));rect(x,y,(max(xs)-min(xs))*18,(max(ys)-min(ys))*18,'#555')
# sidewall door gaps and leaf notation
for yy,label in [(2.2,'남'),(-1.3,'여')]:
 x,y=planpt((-14,yy+.9));rect(x-4,y,8,1.8*18,'white');line([(x,y),(x+32.4,y),(x+32.4,y+32.4)],.8);t(x+43,y+21,label,13)
x,y=planpt((14,2.345));rect(x-4,y,8,4.09*18,'white');line([(x,y),(x-37,y),(x-37,y+73.6),(x,y+73.6)],1)
# door surround actual footprint
x,y=planpt((13.39,2.84));a.append(f'<rect x="{x}" y="{y}" width="{1.19*18}" height="{5.08*18}" fill="none" stroke="#555" stroke-dasharray="3 3"/>')
t(686,507,'주출입구',14);t(686,530,'외곽 5.08',12);t(686,550,'높이 5.99',12)
t(405,307,'2F 경매장 +4.00',22,'middle');t(270,354,'C4',12);t(535,354,'C6',12)
t(405,663,'1F 거래소 ±0.00',22,'middle');t(405,703,'상부 개방',14,'middle')
t(249,467,'← UP',12,'middle');t(561,467,'UP →',12,'middle')
t(405,494,'돌출 폭 7.00 / 깊이 2.80',12,'middle')
line([(153,183),(657,183)]);t(405,174,'28.00',13,'middle');line([(118,210),(118,840)]);t(84,525,'35.00',13)
t(405,873,'계단: 폭 1.60 / 진행 길이 7.00 / 양쪽 바깥 방향 상승',13,'middle')
t(405,897,'중층 앞선 Y=+5.50 / 돌출 끝 Y=+2.70',13,'middle')
# roof plan actual centerlines
cx,cy,sc=1275,447,15
planedges(topboundary(floor,0),cx,cy,sc,2)
for r in R:line([planpt(r['a'],cx,cy,sc),planpt(r['b'],cx,cy,sc)],1.2 if 'Rib' in r['name'] else .85,'#333' if 'Rib' in r['name'] else '#999')
t(1275,136,'02 현재 천장 평면 — 방사형 돔',20,'middle')
t(1275,747,'방사 리브 16방향 + 수평 링 3단',17,'middle')
t(1275,773,'리브: 192개 직선 조각 / 링: 192개 직선 조각',12,'middle')
t(1275,798,'링 중심 높이: +8.82 / +12.32 / +14.882',12,'middle')
t(1275,824,'중앙 한 점으로 수렴 — 동일 단면의 평행 아치가 아님',14,'middle')
# actual roof section from face-plane crossings
roof=[o for k,o in D.items() if 'Hall_Roof_' in k]
def section(cx,base,scale,axis,title):
 t(cx,944,title,18,'middle')
 for o in roof:
  for f in o['f']:
   vs=[o['v'][i] for i in f];pts=[]
   for v,u in zip(vs,vs[1:]+vs[:1]):
    if abs(v[axis])<1e-6:pts.append(v)
    if v[axis]*u[axis]<-1e-10:
     q=v[axis]/(v[axis]-u[axis]);pts.append([v[i]+q*(u[i]-v[i]) for i in range(3)])
   if len(pts)>=2:line([(cx+p[1-axis]*scale,base-p[2]*scale) for p in pts],.8)
 half=14 if axis==1 else 17.5
 line([(cx-half*scale,base-9*scale),(cx-half*scale,base),(cx+half*scale,base),(cx+half*scale,base-9*scale)],1.5)
 line([(cx-half*scale,base-9*scale),(cx+half*scale,base-9*scale)],.6,'#aaa','4 4')
 t(cx,base-16.15*scale-10,'내면 +16.00 / 외면 +16.15',12,'middle')
 t(cx+half*scale+10,base-9*scale,'+9.00',12)
 t(cx,base+22,('X 방향 28.00' if axis==1 else 'Y 방향 35.00'),12,'middle')
 if axis==0:
  line([(cx+2.7*scale,base-4*scale),(cx+17.5*scale,base-4*scale)],3,'#777');t(cx+10*scale,base-4*scale-8,'중층 +4.00',12,'middle')
section(430,1235,15,1,'03 중앙 횡단면 — Y=0 / 실제 천장 메시')
section(1275,1235,15,0,'04 중앙 종단면 — X=0 / 실제 천장 메시')
line([(45,1285),(1755,1285)])
t(55,1317,'이전 A–12 및 생성 이미지에서 수정한 사항',18)
t(55,1350,'① 평행 아치 5개 → 현재 모델은 방사형 리브 16방향. 기존 천장 형상을 기준으로 기둥 접속 위치를 다시 계획해야 함.',14)
t(55,1377,'② 계단은 정면으로 내려오지 않음. 중층 앞쪽에서 좌우 벽 방향으로 상승하며 중앙 돌출부와 각각 1.70m 이격.',14)
t(55,1404,'③ 04A 소품은 별도 높이 4.00m 모델. 방에는 미배치. 기존 지지기둥 C4·C6은 0.50 × 0.50m, 높이 3.65m로 구분.',14)
t(55,1431,'④ +9.00은 벽·천장 접점이며 기존 목재 하단은 약 +8.725. 04A를 9m로 늘려 바로 연결하면 겹칠 수 있어 접합부 재설계 필요.',14)
t(55,1460,'기준: BBB_Hall_With_GrandEntrance / 실제 메시 좌표 추출. 모델은 변경하지 않음. 출입구·계단과 충돌하는 리브 하부에는 기둥 배치 보류.',12)
t(1740,1470,'A–13',24,'end');a.append('</svg>');(P/'22_Hall_Model_Verified_Plan.svg').write_text(''.join(a),encoding='utf-8')
