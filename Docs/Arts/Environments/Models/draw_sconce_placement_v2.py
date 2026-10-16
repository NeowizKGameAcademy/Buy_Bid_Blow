"""Redraw the native plan using the user's marked column locations."""
from pathlib import Path

source=Path(__file__).with_name('draw_sconce_placement.py').read_text(encoding='utf-8-sig')
source=source.replace('벽등 배치 제안  |  1층 8개 + 2층 6개 = 총 14개','벽등 배치 수정안  |  1층 8개 + 2층 4개 = 총 12개')
source=source.replace('28 × 40m 홀 · A21 / A23 도면 + A30 저장 모델 대조 · 2026.10.14 · 게임 배경 배치 검토도','REV.02 · 사용자 표시 반영: 초록 동그라미 추가 / 빨간 X 삭제 · 2026.10.14 · 모델 적용 전')
source=source.replace("text(347,467,'이번 배치에서는 제외'","text(347,467,'후면 기둥 2개에 설치'")
start=source.index('for i,(x,y) in enumerate([(14,-2)')
end=source.index("text(950,1070",start)
source=source[:start]+'''# Keep the four original lights without red crosses.
for id,x in [('L03',14),('L06',-14)]:
 lamp(id,x,-12,f,GOLD,25 if x>0 else -87,-18,1)
 placements[-1]['status']='retain'
 placements[-1]['mount']='wall'
for id,sign in [('U01',1),('U06',-1)]:
 a=math.radians(20);x=sign*(2+12*math.cos(a));y=5.5+12*math.sin(a)
 lamp(id,x,y,g,TEAL,15 if sign>0 else -76,-43,2)
 placements[-1]['status']='retain'
 placements[-1]['mount']='wall'
GREEN='#2d914c'
def columnlamp(id,x,y,m,level,dx,dy):
 px,py=m(x,y)
 circle(px,py,17,None,GREEN,4)
 lx,ly=px+dx,py+dy
 line([(px,py),(lx+28,ly+15)],GREEN,2)
 box(lx,ly,64,32,BG,BG);text(lx+3,ly+1,id,21,GREEN,True)
 placements.append({'id':id,'floor':level,'status':'new','mount':'column_room_facing_surface','column_center_x':x,'column_center_y':y,'center_world_z':2.5 if level==1 else 6.5,'root_world_z':2.1 if level==1 else 6.1,'note':'기둥 위치 표시. 실제 부착점은 실내를 향한 기둥 표면에서 결정.'})
for id,x,y in [('N01',-6.695,16.162),('N02',6.695,16.162),('N03',-13.52,-.4),('N04',13.52,-.4),('N05',-6.695,-21.162),('N06',6.695,-21.162)]:
 columnlamp(id,x,y,f,1,-82 if x<0 else 24,-48 if y>0 else 28 if y<-20 else -20)
for id,x in [('N07',-6.695),('N08',6.695)]:
 columnlamp(id,x,16.162,g,2,-86 if x<0 else 25,-52)
text(950,845,'초록 링 = 새 기둥 설치 위치 / 총 8개',23,GREEN,True)
text(950,890,'기존 유지: L03 · L06 / U01 · U06',21)
text(950,932,'1층 삭제: L01 · L02 · L04 · L05 · L07 · L08',20)
text(950,974,'2층 삭제: U02 · U03 · U04 · U05',21)
text(950,1016,'새 기둥 등은 실내를 향한 면에 부착합니다.',21)
'''+source[end:]
source=source.replace('부착판을 실제 벽 표면에 맞춤','부착판을 기둥 / 벽 표면에 맞춤')
source=source.replace('배치 위치표 / 좌표는 벽 기준 제안값 (m)','수정 위치표 / 신규 N = 기둥 중심 좌표 (m)')
start=source.index('rows=[')
end=source.index("svg.append('</svg>')",start)
source=source[:start]+'''rows=[('N01 · N02','1층 후면 기둥 / 신규','X=±6.695, Y=+16.162'),('N03 · N04','1층 중앙 측면 기둥 / 신규','X=±13.52, Y=-0.40'),('N05 · N06','1층 접수 뒤 기둥 / 신규','X=±6.695, Y=-21.162'),('L03 · L06','접수로 이어지는 측벽 / 유지','X=±14.00, Y=-12.00')]
for i,(a,b,c) in enumerate(rows):
 y=1420+i*49;text(75,y,a,21,GREEN if i<3 else GOLD,True);text(265,y,b,20);text(660,y,c,20)
for i,(a,b,c) in enumerate([('N07 · N08','2층 후면 기둥 / 신규','X=±6.695, Y=+16.162'),('U01 · U06','계단 도착 측 곡면 / 유지','X=±13.28, Y=+9.60')]):
 y=1420+i*49;text(1240,y,a,21,GREEN if i==0 else TEAL,True);text(1440,y,b,20);text(1880,y,c,20)
text(1240,1545,'1층 8개 = 신규 6 + 유지 2',21,b=True)
text(1240,1590,'2층 4개 = 신규 2 + 유지 2',21,b=True)
text(70,1660,'사용자 표시대로: 기둥 8곳에 신규 설치하고, X 없는 기존 4곳은 유지합니다. 빨간 X 위치 10곳은 제외했습니다.',22,b=True)
text(70,1710,'수정 도면만 작성 · A31 모델은 아직 변경하지 않음. 기둥의 정확한 부착면과 주변 간섭은 모델 적용 시 확인합니다.',20,c=MUT)
'''+source[end:]
source=source.replace("stem='A31_Wall_Sconce_Placement_Plan'","stem='A32_Wall_Sconce_Placement_Plan_Rev02'")
source=source.replace("'checked_model':'BBB_Hall_A30_StaffMedallion.blend'","'base_model':'BBB_Hall_A31_WallSconces.blend','revision':'User annotated plan Rev02','remove_ids':['L01','L02','L04','L05','L07','L08','U02','U03','U04','U05']")
exec(compile(source,__file__,'exec'))
