from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
from lxml import etree as E
from copy import deepcopy
import posixpath
B=Path('Docs/TechnicalPresentation/.revision')
ns={'a':'http://schemas.openxmlformats.org/drawingml/2006/main','p':'http://schemas.openxmlformats.org/presentationml/2006/main','r':'http://schemas.openxmlformats.org/officeDocument/2006/relationships'}
Q=lambda p,t:'{'+ns[p]+'}'+t
with ZipFile(B/'toc-base.pptx')as z:files={n:z.read(n) for n in z.namelist()}
rel=E.fromstring(files['ppt/_rels/presentation.xml.rels']);rels={r.get('Id'):posixpath.normpath('ppt/'+r.get('Target')) for r in rel}
pr=E.fromstring(files['ppt/presentation.xml']);order=[rels[e.get(Q('r','id'))] for e in pr.find('p:sldIdLst',ns)]
def txt(sp):return '\n'.join(e.text or '' for e in sp.findall('.//a:t',ns))
def frame(sp,x,y,w,h):
 xf=sp.find('p:spPr/a:xfrm',ns)
 off=xf.find('a:off',ns);ext=xf.find('a:ext',ns)
 for k,v in [('x',x),('y',y)]:off.set(k,str(round(v*9525)))
 for k,v in [('cx',w),('cy',h)]:ext.set(k,str(round(v*9525)))
def paint(sp,fill):
 pp=sp.find('p:spPr',ns)
 for e in list(pp):
  if e.tag in[Q('a','solidFill'),Q('a','noFill')]:pp.remove(e)
 sf=E.Element(Q('a','solidFill'));E.SubElement(sf,Q('a','srgbClr'),val=fill);pp.insert(2,sf)
def restyle(sp,color,size=None):
 for r in sp.findall('.//a:rPr',ns):
  if size:r.set('sz',str(round(size*75)))
  old=r.find('a:solidFill',ns)
  if old is not None:r.remove(old)
  sf=E.Element(Q('a','solidFill'));E.SubElement(sf,Q('a','srgbClr'),val=color);r.insert(0,sf)
def settext(sp,value,size,color,bold=False):
 body=sp.find('p:txBody',ns)
 for p in body.findall('a:p',ns):body.remove(p)
 for line in value.split('\n'):
  p=E.SubElement(body,Q('a','p'));pp=E.SubElement(p,Q('a','pPr'))
  E.SubElement(E.SubElement(pp,Q('a','lnSpc')),Q('a','spcPct'),val='110000')
  r=E.SubElement(p,Q('a','r'));rp=E.SubElement(r,Q('a','rPr'),sz=str(round(size*75)),b='1' if bold else '0')
  E.SubElement(E.SubElement(rp,Q('a','solidFill')),Q('a','srgbClr'),val=color)
  for f in ['latin','ea','cs']:E.SubElement(rp,Q('a',f),typeface='Malgun Gothic')
  E.SubElement(r,Q('a','t')).text=line
# Copy editable text shape to ensure native typography and placeholders remain compatible.
model=E.fromstring(files[order[2]]).findall('.//p:sp',ns)[0]
def addtext(tree,value,x,y,w,h,size,color,bold=False):
 sp=deepcopy(model);nv=sp.find('p:nvSpPr/p:cNvPr',ns);nv.set('id',str(5000+len(tree)));nv.set('name',value[:40]);frame(sp,x,y,w,h);settext(sp,value,size,color,bold);tree.append(sp);return sp
for n,name in enumerate(order,1):
 root=E.fromstring(files[name]);tree=root.find('p:cSld/p:spTree',ns)
 if n in [1,19]:continue
 sh=tree.findall('p:sp',ns)
 if n==2:
  bg=root.find('p:cSld/p:bg',ns)
  if bg is None:
   bg=E.Element(Q('p','bg'));root.find('p:cSld',ns).insert(0,bg)
   bp=E.SubElement(bg,Q('p','bgPr'));E.SubElement(E.SubElement(bp,Q('a','solidFill')),Q('a','srgbClr'),val='F5F2EA')
  title=addtext(tree,'목차',64,46,1152,74,54,'F5F2EA',True)
  subtitle=addtext(tree,'기획 의도부터 실제 구현과 문제 해결까지',64,126,1152,42,23,'D5DFD9')
  addtext(tree,'02',1175,675,50,25,16,'68766E')
  groups=[('01','기획과 공간','게임 개요 / 세계관 / 캐릭터 / 레벨 구조','03–06'),('02','화면과 플레이 경험','메인·로비 / HUD / 거래·경매 / 수첩 / 피드백 / 결과','07–16'),('03','구현과 문제 해결','UI와 서버의 역할 / 본인 입찰 인상 수정','17–18'),('04','회고','기획 의도와 다음 검증','19')]
  for j,(num,h,b,pages)in enumerate(groups):
   y=220+j*112
   addtext(tree,num,64,y,90,58,40,'8B632B',True)
   addtext(tree,h,180,y,850,45,31,'123A37',True)
   addtext(tree,b,180,y+49,850,36,23,'52635D')
   addtext(tree,pages,1090,y+12,126,40,25,'8B632B',True)
 else:
  title,subtitle=sh[:2];frame(title,64,49,1152,72);restyle(title,'F5F2EA',54)
  frame(subtitle,64,126,1152,42);restyle(subtitle,'D5DFD9',23)
  # Update displayed folio to account for the inserted contents page.
  settext(sh[2],f'{n:02d}',16,'68766E')
 # A single editable title band clearly separates the heading from the page content.
 band=deepcopy(model);nv=band.find('p:nvSpPr/p:cNvPr',ns);nv.set('id','7000');nv.set('name','제목 영역');settext(band,'',1,'123A37');frame(band,0,0,1280,184);paint(band,'123A37');tree.insert(2,band)
 section='CONTENTS' if n==2 else '01  기획과 공간' if n<=6 else '02  화면과 플레이 경험' if n<=16 else '03  구현과 문제 해결'
 addtext(tree,section,64,14,1050,26,16,'E4C58B',True)
 files[name]=E.tostring(root,xml_declaration=True,encoding='UTF-8',standalone=True)
with ZipFile(B/'candidate-v5.pptx','w',ZIP_DEFLATED)as z:
 for k,v in files.items():z.writestr(k,v)
print('19 slides with title areas and contents')
