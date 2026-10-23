"""Add the two user-marked inner stair wall positions to revision 02."""
from pathlib import Path

previous=Path(__file__).with_name('draw_sconce_placement_v2.py').read_text(encoding='utf-8')
previous=previous.replace("exec(compile(source,__file__,'exec'))",'')
exec(compile(previous,__file__,'exec'))
source=source.replace('1층 8개 + 2층 4개 = 총 12개','1층 10개 + 2층 4개 = 총 14개')
source=source.replace('REV.02 · 사용자 표시 반영: 초록 동그라미 추가 / 빨간 X 삭제','REV.03 · 기둥 배치 유지 + 좌우 계단 안쪽 벽면 2개 추가')
source=source.replace("text(950,845,'초록 링 = 새 기둥 설치 위치 / 총 8개',23,GREEN,True)","""
for id,x in [('S01',-11.5),('S02',11.5)]:
 px,py=f(x,5.5)
 circle(px,py,15,None,GREEN,4)
 lx=px-75 if x<0 else px+18;ly=py-60
 line([(px,py),(lx+27,ly+27)],GREEN,2)
 box(lx,ly,65,32,BG,BG);text(lx+3,ly+1,id,21,GREEN,True)
 placements.append({'id':id,'floor':1,'status':'new','mount':'inner_stair_wall','schematic_reference_x':x,'schematic_reference_y':5.5,'center_height_above_adjacent_floor':2.5,'note':'사용자 표시한 계단 안쪽 벽면. 도면의 기준점이며 실제 부착점/바닥 높이는 모델 적용 시 확인.'})
text(950,845,'초록 링 = 신규 10개 / 기존 유지 4개',23,GREEN,True)
""")
source=source.replace('새 기둥 등은 실내를 향한 면에 부착합니다.','S01 · S02: 좌우 계단 안쪽 벽면에 1개씩 추가.')
source=source.replace("text(1240,1545,'1층 8개 = 신규 6 + 유지 2',21,b=True)","text(1240,1545,'S01 · S02  계단 안쪽 벽면 / 신규 2개',21,GREEN,True)")
source=source.replace("text(1240,1590,'2층 4개 = 신규 2 + 유지 2',21,b=True)","text(1240,1590,'1층 10개 + 2층 4개 = 총 14개',21,b=True)")
source=source.replace('사용자 표시대로: 기둥 8곳에 신규 설치하고, X 없는 기존 4곳은 유지합니다. 빨간 X 위치 10곳은 제외했습니다.','기둥 8개 + 계단 안쪽 벽면 2개 + 기존 유지 4개. 이전 빨간 X 위치 10곳은 계속 제외합니다.')
source=source.replace('수정 도면만 작성 · A31 모델은 아직 변경하지 않음. 기둥의 정확한 부착면과 주변 간섭은 모델 적용 시 확인합니다.','수정 도면 · 모델 미적용. S01/S02는 표시 위치 기준이며 실제 벽면과 인접 바닥 높이를 확인해 부착합니다.')
source=source.replace('부착판을 기둥 / 벽 표면에 맞춤','계단 등 높이: 인접 바닥 +2.50m')
source=source.replace('A32_Wall_Sconce_Placement_Plan_Rev02','A33_Wall_Sconce_Placement_Plan_Rev03')
source=source.replace('User annotated plan Rev02','User annotated plan Rev03 with inner stair wall pair')
exec(compile(source,__file__,'exec'))
