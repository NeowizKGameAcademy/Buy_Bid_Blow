"""Slice the generated settings atlas without painting or resampling it."""
from pathlib import Path
from PIL import Image
import shutil

root = Path(__file__).resolve().parents[2]
archive = root / 'Docs/Arts/UI/Options'
out = root / 'Assets/Project/UI/Menu/Options/Sprites'
archive.mkdir(parents=True, exist_ok=True)
out.mkdir(parents=True, exist_ok=True)
image = Image.open(archive / 'SettingsAtlas.png').convert('RGBA')
regions = {
    'SettingsPanel': (20, 65, 375, 420),
    'WoodTitle': (395, 160, 880, 340),
    'GearMedallion': (900, 160, 1085, 345),
    'Divider': (1110, 225, 1430, 290),
    'ButtonNormal': (20, 510, 375, 675),
    'ButtonSelected': (380, 510, 725, 675),
    'ButtonPressed': (728, 510, 1075, 675),
    'ButtonDisabled': (1080, 510, 1435, 675),
    'PrimaryNormal': (15, 795, 380, 980),
    'PrimarySelected': (385, 795, 735, 980),
    'PrimaryPressed': (740, 795, 1095, 980),
    'SliderHandle': (1195, 785, 1310, 985),
}
for name, region in regions.items():
    tile = image.crop(region)
    bounds = tile.getchannel('A').point(lambda a: 255 if a > 40 else 0).getbbox()
    tile.crop(bounds).save(out / (name + '.png'))
    print(name, tile.crop(bounds).size)
panel = Image.open(archive / 'SettingsPanelSource.png').convert('RGBA')
bounds = panel.getchannel('A').point(lambda a: 255 if a > 40 else 0).getbbox()
panel.crop(bounds).save(out / 'SettingsPanel.png')
icons = Image.open(archive / 'MenuIconsAtlas.png').convert('RGBA')
width, height = icons.size
for n, name in enumerate(['GearNormal', 'GearSelected', 'GearPressed', 'GearDisabled', 'ResumeIcon', 'HomeIcon', 'ControlsIcon', 'ResultsIcon']):
    x, y = n % 4, n // 4
    tile = icons.crop((x*width//4, y*height//2, (x+1)*width//4, (y+1)*height//2))
    bounds = tile.getchannel('A').point(lambda a: 255 if a > 40 else 0).getbbox()
    tile.crop(bounds).save(out / (name+'.png'))
