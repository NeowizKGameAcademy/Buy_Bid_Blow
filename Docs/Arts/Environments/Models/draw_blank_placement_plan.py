from pathlib import Path
source=Path(__file__).with_name('draw_banner_barrier_plan.py').read_text(encoding='utf-8')
source=source.split('records=[]')[0]
source=source.replace('배너 · 접근제한봉 위치 검토','배치 스케치용 기본 도면')
source=source.replace('후보안 01  |  기존 평면 구조 기준 · 실제 소품 미배치 · 2026.10.16','아래층 + 중층  |  배너·접근제한봉 표시 없음')
exec(source)
text(1100,850,'배치 메모',26,col=MUT)
for y in [930,1030,1130,1230]:line([(1100,y),(2050,y)],'#dce0d9',1)
line([(65,1340),(2130,1340)])
text(65,1390,'위 = +Y  /  오른쪽 = +X  ·  기존 평면 구조와 카펫 위치를 간략히 표시한 스케치용 도면',23,col=MUT)
svg.append('</svg>')
stem='A72_Blank_Placement_Plan'
im.save(OUT/(stem+'.png'))
(OUT/(stem+'.svg')).write_text('\n'.join(svg),encoding='utf-8')
