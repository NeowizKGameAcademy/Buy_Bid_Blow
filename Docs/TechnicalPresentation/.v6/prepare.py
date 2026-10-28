from pathlib import Path
from zipfile import ZipFile
from lxml import etree as E
from PIL import Image
from io import BytesIO
import posixpath,json
base=Path('Docs/TechnicalPresentation/.v6'); logs=Path('Logs/TechnicalPresentation')
ns={'a':'http://schemas.openxmlformats.org/drawingml/2006/main','p':'http://schemas.openxmlformats.org/presentationml/2006/main','r':'http://schemas.openxmlformats.org/officeDocument/2006/relationships'}
with ZipFile('Docs/TechnicalPresentation/Output/Buy_Bid_Blow_기술문서_v5_목차추가.pptx')as z:
 rels={r.get('Id'):posixpath.normpath('ppt/'+r.get('Target'))for r in E.fromstring(z.read('ppt/_rels/presentation.xml.rels'))}
 order=[rels[e.get('{'+ns['r']+'}id')]for e in E.fromstring(z.read('ppt/presentation.xml')).find('p:sldIdLst',ns)]
 notes=[]
 for i,name in enumerate(order):
  relname=posixpath.dirname(name)+'/_rels/'+posixpath.basename(name)+'.rels';rs={r.get('Id'):(r.get('Type'),posixpath.normpath(posixpath.dirname(name)+'/'+r.get('Target')))for r in E.fromstring(z.read(relname))}
  np=[v[1]for v in rs.values()if v[0].endswith('/notesSlide')]
  note='\n'.join(t.text or '' for t in E.fromstring(z.read(np[0])).findall('.//a:t',ns))if np else ''
  notes.append(note)
  if i==4:
   for j,pic in enumerate(E.fromstring(z.read(name)).findall('.//p:pic',ns)):
    target=rs[pic.find('.//a:blip',ns).get('{'+ns['r']+'}embed')][1];im=Image.open(BytesIO(z.read(target))).convert('RGB');sc=pic.find('.//a:srcRect',ns)
    if sc is not None:
     l,t,r,b=[int(sc.get(k,'0'))/100000 for k in ['l','t','r','b']];w,h=im.size;im=im.crop((w*l,h*t,w*(1-r),h*(1-b)))
    im.save(base/f'assets/character-{j}.jpg',quality=95)
 (base/'notes.json').write_text(json.dumps(notes,ensure_ascii=False),encoding='utf-8')
# Actual gameplay photos. Crops change only framing and preserve all displayed values.
crops={
 'trade':('06_TradeBefore',(995,180,1860,1045)),
 'public':('12_AuctionOpen',(590,15,1330,175)),
 'secret':('17_AuctionSecret',(590,15,1330,175)),
 'empty':('05_NotebookEmpty',(545,90,1515,1010)),
 'owned':('16_NotebookOwned',(545,90,1515,1010)),
 'guide':('Guide_3',(80,40,1840,1020)),
 'won':('15_AuctionWon',(520,285,1390,785)),
 'envelope':('World_Envelope',(775,390,1145,690)),
 'settings':('02_Settings',(390,30,1535,1000)),
 'result':('18_Results',(150,110,1790,1010)),
 'graph':('19_AssetGraph',(150,110,1790,1010)),
 'bid':('14_BidEight',(580,20,1340,175)),
}
for name,(src,box)in crops.items():Image.open(logs/(src+'.png')).convert('RGB').crop(box).save(base/('assets/'+name+'.jpg'),quality=95)
for name in ['01_Main','03_Lobby','04_Explore','World_Stairs','World_ExchangeShot_Start','World_ExchangeShot_End']:
 Image.open(logs/(name+'.png')).convert('RGB').save(base/('assets/'+name+'.jpg'),quality=92)
print('Source notes and actual screenshot crops ready')
