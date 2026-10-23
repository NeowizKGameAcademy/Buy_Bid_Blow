"""Lay out unmodified Unity preview captures and imported mesh counts."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json

root = Path(__file__).resolve().parents[4]
folder = root / 'Docs/Arts/Props/Models/FurnitureUtilityPack'
budgets = {item['name']: item for item in json.loads((folder / 'Unity_Budget.json').read_text())['assets']}
font = lambda size: ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', size)
canvas = Image.new('RGB', (1500, 1850), '#182321')
draw = ImageDraw.Draw(canvas)
draw.text((45, 28), 'BUY · BID · BLOW', font=font(30), fill='#d9b76b')
draw.text((45, 75), '가구 5종 · A/B 모델 10개', font=font(42), fill='#f1ece0')
draw.text((45, 132), 'Unity 실제 렌더  |  LOD 2단계  |  안내판 글자는 텍스처', font=font(22), fill='#b4c3bd')
types = [('InformationBoard', '안내판'), ('ServiceTrolley', '서비스 카트'), ('Table', '테이블'), ('Bookcase', '책장'), ('Shelf', '선반')]
for i, (name, label) in enumerate(types):
    x = 30 + (i % 2) * 735
    y = 192 + (i // 2) * 540
    image = Image.open(folder / (name + '_Unity.png')).convert('RGB')
    image = image.resize((705, 504), Image.Resampling.LANCZOS)
    # Titles occupy the top of the capture's empty studio background.
    canvas.paste(image, (x, y))
    draw.text((x+20, y+13), label + '  A / B', font=font(25), fill='#fff8e8', stroke_width=1, stroke_fill='#26302c')
    a, b = budgets[name+'_A'], budgets[name+'_B']
    draw.text((x+14, y+510), f"A {a['lod0Triangles']:,} tris   ·   B {b['lod0Triangles']:,} tris", font=font(19), fill='#cbd5d1')
draw.text((800, 1395), '게임용 구성', font=font(31), fill='#ead5a7')
for n, line in enumerate(['공용 BlenderKit 목재 · 황동 재질', '교체 가능한 안내판 텍스처', '책장 내용물 별도 파트', '프리팹 · 충돌 영역 · LOD 포함']):
    draw.text((800, 1460+n*54), line, font=font(24), fill='#d9e0d9')
draw.text((800, 1720), '수치: 소품 포함 LOD0 삼각형 수', font=font(19), fill='#a2b6ac')
canvas.save(folder / 'FurnitureUtilityPack_Overview.png')
print(folder / 'FurnitureUtilityPack_Overview.png')
