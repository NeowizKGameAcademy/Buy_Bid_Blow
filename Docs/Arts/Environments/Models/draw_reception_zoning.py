from PIL import Image,ImageDraw,ImageFont
import math
P='C:/course/unity/BuyBidBlow/Docs/Arts/Environments/'
im=Image.new('RGB',(1900,1600),'#f4f1e9');d=ImageDraw.Draw(im)
def txt(x,y,t,s=24,c='#253e3b'):
 d.text((x,y),t,font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',s),fill=c)
def arc(cx,cy,rx,ry,a,b):
 return [(cx+rx*math.cos(a+(b-a)*i/60),cy+ry*math.sin(a+(b-a)*i/60)) for i in range(61)]
out=arc(2,5.5,12,12,0,math.pi/2)+arc(-2,5.5,12,12,math.pi/2,math.pi)+arc(-2,-13.5,12,9,math.pi,1.5*math.pi)+arc(2,-13.5,12,9,1.5*math.pi,2*math.pi)
def panel(cx,cy,scale,detail=False):
 def m(x,y):return(cx+x*scale,cy-y*scale)
 def poly(p,fill):d.polygon([m(*q) for q in p],fill=fill)
 def ln(p,c,w=3):d.line([m(*q) for q in p],fill=c,width=w)
 def box(x,y,w,h,c):
  d.rectangle([m(x-w/2,y+h/2),m(x+w/2,y-h/2)],fill=c)
 if not detail:
  poly(out,'#fffdf8');ln(out+[out[0]],'#65716b')
  for sign in [-1,1]:
   box(sign*7.3,3.7,7,3.2,'#cfcec3');box(sign*12.4,3.8,3.2,3.4,'#cfcec3')
   for i in range(24):ln([(sign*(3.8+i*7/24),2.1),(sign*(3.8+i*7/24),5.3)],'#969e95',1)
  txt(*m(-5,10),'중층 하부 / 기존 계단',19)
  d.ellipse([m(-5.5,-1.5),m(5.5,-12.5)],outline='#b28a50',width=4)
  txt(*m(-3.9,-5.8),'중앙 문양 Ø11m',19)
  txt(*m(-3.4,-7.5),'고객 로비 · 비움',17)
  ln([(14,-2.9),(14,-7.98)],'#f4f1e9',8);txt(*m(14.3,-4),'주출입구',16)
  for lo,hi in [(-4.82,-2.9),(-8.32,-6.4)]:ln([(-14,lo),(-14,hi)],'#f4f1e9',8)
 else:
  curve=arc(-2,-13.5,12,9,math.pi,1.5*math.pi)+arc(2,-13.5,12,9,1.5*math.pi,2*math.pi)
  poly([(-14,-11)]+curve+[(14,-11)],'#fffdf8');ln(curve,'#65716b')
 xs=[-12+24*i/100 for i in range(101)]
 front=[(x,-13.7-.020*x*x) for x in xs];back=[(x,y-1.2) for x,y in front]
 # Staff area behind the continuous reception.
 lower=arc(-2,-13.5,12,9,math.pi,1.5*math.pi)+arc(2,-13.5,12,9,1.5*math.pi,2*math.pi)
 poly(back+list(reversed(lower)),'#e9d8c7')
 poly(front+list(reversed(back)),'#ad8b64');ln(front,'#254e47',5);ln(back,'#735a41',2)
 for x,label in [(-8,'벽돌'),(0,'나무'),(8,'짚')]:
  y=-13.7-.020*x*x
  box(x,y-.7,1.1,.20,'#263237');box(x,y-2.0,.65,.65,'#ac705a')
  txt(*m(x-.65,y+.85),label,19)
 for x in [-12.75,12.75]:
  y=-13.7-.020*12*12-.4
  ln([(x-.55,y),(x+.55,y)],'#ad6953',5)
 for x in [-6.695,6.695]:box(x,-21.162,.96,.96,'#43584e')
 for a,b in [((-4.5,-21.6),(4.5,-21.6)),((-13.1,-13),(-7.7,-20.1)),((13.1,-13),(7.7,-20.1))]:
  for j in range(15):
   t=j/15;u=(j+.55)/15
   ln([(a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t),(a[0]+(b[0]-a[0])*u,a[1]+(b[1]-a[1])*u)],'#7c8990',2)
 txt(*m(-3,-19),'직원 전용 공간',20)
 if detail:
  txt(*m(-5,-11.7),'고객 접근 / 접수 대기',22)
  txt(*m(-3.8,-22.8),'TV 3대 · 높은 벽걸이 위치 투영',16)
 else:txt(*m(-4.9,-24),'28 × 40m 홀 / 1층 평면',20)
txt(70,40,'BUY · BID · BLOW   /   RECEPTION ZONING',38)
txt(70,102,'곡선형 접수 데스크로 고객 공간과 직원 공간 분리',28)
txt(70,155,'초록 표시 반영 · 배치 제안 B · 치수는 설계 제안이며 모델 미적용',20,'#64756b')
txt(70,224,'01  전체 배치',27);panel(500,790,24)
txt(970,224,'02  거래 접수 구역 확대',27);panel(1415,160,29,True)
txt(970,920,'배치 원칙',27)
notes=['• 고객 정면 기준 왼쪽부터: 짚 → 나무 → 벽돌','• 평면도는 반대 시점: 왼쪽 벽돌 / 오른쪽 짚','• 연속 곡선 카운터: 폭 약 24m / 깊이 1.2m','• 거래 창구 3개: 각 창구 모니터 1대·직원 1석','• 양끝: 직원 전용 게이트 약 1.2m 확보 목표','• 고객 상판 1.1m / 직원 작업대 0.75m 높이','• 데스크 뒤 / TV 쪽: 직원 전용 업무 공간','• TV는 현재 배치 위치를 평면에 투영']
for i,t in enumerate(notes):txt(970,970+i*49,t,21)
txt(70,1450,'재질 방향  |  중간 톤 목재 · 오트밀 아이보리 · 짙은 청녹색 · 테라코타 · 은은한 황동',24)
txt(70,1510,'직원 출입구·의자 사용 범위·곡면 벽 간격은 실제 3D 배치 시 확인하고 조정합니다.',21,'#64756b')
im.save(P+'A23_Reception_Zoning_Plan.png')

