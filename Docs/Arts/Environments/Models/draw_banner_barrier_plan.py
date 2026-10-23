from pathlib import Path
src=Path(__file__).with_name('draw_art_curtain_plan.py').read_text(encoding='utf-8')
exec(src.split("text(65,40,")[0])
W,H=2200,1600
im=Image.new('RGB',(W,H),BG);d=ImageDraw.Draw(im)
svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}"><rect width="100%" height="100%" fill="{BG}"/>']
f=lambda x,y:(510+x*25,720-y*25)
g=lambda x,y:(1510+x*25,720-y*25)
BLUE='#237eac';ORANGE='#c56b30';GREEN='#4c9574'
text(65,35,'BUY · BID · BLOW  /  배너 · 접근제한봉 위치 검토',39,bold=True)
text(65,100,'후보안 01  |  기존 평면 구조 기준 · 실제 소품 미배치 · 2026.10.16',23,col=MUT)
text(95,160,'01  아래층',29,bold=True);text(1100,160,'02  중층',29,bold=True)
wp(outline,f,'#fffdf8');wp(upper,g,'#fffdf8')
wp(upper,f,'#e8ece5')
for m in [f,g]:
 for side in [-1,1]:
  wr(side*7.3,3.7,7,3.2,m,'#d3d4ca');wr(side*12.4,3.8,3.2,3.4,m,'#d3d4ca')
  for i in range(1,23):line([m(side*(3.8+7*i/23),2.1),m(side*(3.8+7*i/23),5.3)],MUT,1)
 for x,y in [(-6.695,16.162),(6.695,16.162)]:wr(x,y,.96,.96,m,INK)
for x,y in [(-13.52,-.4),(13.52,-.4),(-6.695,-21.162),(6.695,-21.162)]:wr(x,y,.96,.96,f,INK)
xs=[-12+24*i/80 for i in range(81)];front=[(x,-13.7-.02*x*x) for x in xs]
wp(front+[(x,y-1.2) for x,y in front][::-1],f,'#c3ae8d')
text(*f(-2,-14.8),'접수대',20);text(*f(-2.5,-19),'직원 공간',20)
text(*f(-3,10),'중층 아래 공간',21,col=MUT)
for y in [11.5,14.7]:line([g(x,y+.09*x*x) for x in [-3+i*.1 for i in range(61)]],'#a88d66',10)
text(*g(-2,13.7),'객석',20);wr(0,6.2,2.3,.84,g,'#b69b74');text(*g(-1.5,5.5),'경매대',18)
line([f(14,-7.98),f(14,-2.9)],BLUE,7);text(*f(14.8,-4.5),'입구',20)
line([f(-14,-8.32),f(-14,-6.4)],BLUE,6);line([f(-14,-4.82),f(-14,-2.9)],BLUE,6);text(*f(-17,-5),'WC',20)
# Current carpet footprints, simplified.
wr(7.45,-5.44,12.5,3.7,f,'#e4d1ce');wr(-12.25,-5.6,3,7,f,'#cddfda');wr(-7.275,-5.44,7.55,3.7,f,'#e4d1ce')
wp(arc(0,-5.1,4.5,4.5,0,math.tau),f,'#eadbd4');wp(arc(0,9.6,5.2,3.3,0,math.tau),f,'#eadbd4')
text(*f(-2.5,-4.6),'중앙 카펫',19)
records=[]
def banner(ID,x,y,m,dx,dy):
 px,py=m(x,y);rect(px-12,py-6,24,12,BLUE,BLUE);line([(px,py),(px+dx,py+dy)],BLUE,2);text(px+dx-24,py+dy-25,ID,23,BLUE,True)
 records.append({'id':ID,'type':'banner','xy_m':[x,y],'floor':'lower' if m==f else 'mezzanine','status':'proposal; height and clearance pending'})
banner('B01',-13.1,-.4,f,-60,-45);banner('B02',13.1,-.4,f,42,-45)
banner('B03',-6.695,15.5,g,-60,-45);banner('B04',6.695,15.5,g,30,-45)
def rope(ID,a,b,m,dx,dy):
 pts=[m(a[0]+(b[0]-a[0])*i/2,a[1]+(b[1]-a[1])*i/2) for i in range(3)]
 line(pts,ORANGE,6)
 for p in pts:circle(*p,7,ORANGE)
 px,py=pts[1];line([(px,py),(px+dx,py+dy)],ORANGE,2);text(px+dx-24,py+dy-20,ID,23,ORANGE,True)
 records.append({'id':ID,'type':'barrier group','endpoints_xy_m':[a,b],'status':'schematic candidate; fit to actual gap'})
rope('R01',(-13,-14.9),(-11.4,-17.1),f,-65,20)
rope('R02',(13,-14.9),(11.4,-17.1),f,45,20)
rope('R03',(-2.8,7),(-2.8,10.2),g,-95,5)
rope('R04',(2.8,7),(2.8,10.2),g,55,5)
line([g(0,3),g(0,10)],GREEN,4,True)
text(1100,800,'배너 4곳  /  파란색',27,BLUE,True)
text(1100,850,'B01–02  로비 양쪽 기둥의 실내측',23)
text(1100,890,'B03–04  중층 객석 뒤쪽 기둥의 실내측',23)
text(1100,935,'세로형 배너를 높게 걸어 입구·그림·벽등을 피함.',21,col=MUT)
text(1100,1005,'접근제한봉 4구간  /  주황색',27,ORANGE,True)
text(1100,1055,'R01–02  접수대 양끝 → 직원 공간 경계',23)
text(1100,1095,'R03–04  경매대 양옆 → 관람객 접근 유도',23)
text(1100,1140,'직원 출입 구간은 탈착식 로프를 전제로 검토.',21,col=MUT)
text(1100,1180,'계단·상부 참·중앙 통로는 비워 둠.',23,GREEN,True)
line([(65,1340),(2130,1340)])
text(65,1370,'검토 포인트',27,bold=True)
text(65,1420,'우선 B01–04와 R01–02만 배치하고, R03–04는 경매대 접근 제한이 필요할 때 추가하는 안을 권장합니다.',23)
text(65,1470,'평면 위치를 비교하는 도면입니다. 설치 높이·벽등 간섭·실제 통로 폭은 모델 배치 단계에서 확인합니다.',22,col=MUT)
text(65,1520,'위 = +Y  /  오른쪽 = +X  ·  좌표는 블렌더 기준  ·  가구와 그림 등은 가독성을 위해 생략',20,col=MUT)
svg.append('</svg>')
stem='A72_Banner_RopeBarrier_Proposal'
im.save(OUT/(stem+'.png'));(OUT/(stem+'.svg')).write_text('\n'.join(svg),encoding='utf-8')
(OUT/(stem+'.json')).write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print(OUT/(stem+'.png'))
