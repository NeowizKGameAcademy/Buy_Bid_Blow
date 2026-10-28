from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
from copy import deepcopy
from lxml import etree as E
import json
B=Path('Docs/TechnicalPresentation/.revision')
ns={'a':'http://schemas.openxmlformats.org/drawingml/2006/main','p':'http://schemas.openxmlformats.org/presentationml/2006/main'}
q=lambda pref,tag:'{'+ns[pref]+'}'+tag
EMU=9525
# Map text shapes in each retained slide. Source screenshots and speaker notes remain embedded.
data=[
('BUY, BID,\nBLOW','세계관과 공간 설계\nUI 구현과 연결', {3:'개인 기여 기술문서',4:'후일담 테마와 캐릭터 설정\n레벨 구조 및 UI/UX',5:''}),
('정보를 사고, 주식에 투자하는 게임','정보 경매와 거래에 사용할 현금을 함께 판단합니다.',{3:'정보 획득   →   투자 판단   →   시세 공개   →   자산 평가',4:'정보는 낙찰자만 확인하고, 최종 총자산으로 순위를 결정합니다.'}),
('동화의 소재를 세 회사로 재해석','아기돼지 삼형제의 후일담을 금융 활동으로 옮겼습니다.',{3:'짚·나무·벽돌 회사',4:'Straw · Timber · Brick에\n원작의 소재를 대응',5:'돼지와 늑대의 역할',6:'돼지는 회사 운영자\n늑대는 직접 움직이는 투자자',7:'',8:'',9:''}),
('외형으로 구분하는 캐릭터 역할','회사 운영자와 경매 진행자, 플레이어의 역할을 구분했습니다.',{8:'회사 색상과 상징을 외형·간판에 함께 사용했습니다.'}),
('1층 거래소와 2층 경매장','거래와 정보 획득을 오가도록 층별 기능과 동선을 설계했습니다.',{3:'1층: 거래소와 계단 연결',4:'거래소에서 계단으로 2층 이동',5:'2층: 경매 참여석',6:'진행자·경매품을 향한 참여석',7:''}),
('메인에서 배우고, 로비에서 준비','게임 설명과 참가 준비를 서로 다른 단계로 나눴습니다.',{3:'메인: 시작·설명·설정',4:'HowToPlay에서 규칙과 조작 확인',5:'로비: 닉네임·외형·준비',6:'캐릭터로 존재하는 대기 공간',7:''}),
('고정 HUD와 상황별 UI','항상 필요한 값은 상단에, 행동 안내는 상황에 맞춰 표시합니다.',{3:'고정 정보는 상단에',4:'자산은 왼쪽, 진행은 오른쪽\n중앙에는 시야 확보',5:'조작창은 이용할 때만',6:'창구에서 거래창 표시\n경매석에서 입찰 UI 표시',7:'',8:'',9:''}),
('거래 전에 비용과 잔액 확인','예상값을 보여주고, 체결 결과로 현금·보유량·재고를 갱신합니다.',{3:'거래 전: 수량과 비용',4:'W/S 메뉴 · A/D 수량 · E 확정',5:'거래 후: 보유량과 재고',6:'2주 매수 후 보유 2주, 시장 재고 8주',7:''}),
('공개·비공개 경매의 정보 구분','같은 조작 틀에서 공개 범위와 취소 조건을 구분했습니다.',{3:'공개: 최고 입찰 표시',4:'취소 불가 · 본인 최고가도 추가 인상',5:'비공개: 본인 제출만 표시',6:'마감 전 수정·취소 가능',7:''}),
('낙찰 정보는 개인 수첩에 기록','라운드와 회사의 교차점에서 보유 정보를 확인합니다.',{3:'획득 전: 빈칸',4:'모르는 정보는 비워 두기',5:'획득 후: 방향 기록',6:'Tab 열기 · Q/Tab 닫기 · W/S 스크롤',7:''}),
('큰 단계 설명과 실제 UI 예시','한 페이지를 한 주제로 줄이고, 조작 순서를 크게 보여줬습니다.',{3:'거래: 선택·수량·확정',4:'설명 옆에 실제 거래 UI 배치',5:'경매: 접근·대상·입찰',6:'핵심 3단계와 별도 상세 규칙',7:''}),
('성공 결과를 화면과 소리로 전달','입력 신호와 서버가 확정한 성공 결과를 구분했습니다.',{3:'베팅: 참여와 유지 상태',4:'성공 응답 후 화면·효과음 표시',5:'낙찰: 획득과 열람',6:'낙찰과 정보 열람에 서로 다른 소리',7:''}),
('경매 상태에 반응하는 상품·NPC','게임 판정과 전시·애니메이션 연출의 책임을 분리했습니다.',{3:'경매품 전시',4:'경매 중에만 봉투 표시·회전\n라운드는 3D 텍스트로 갱신',5:'NPC 반응 범위',6:'거래소: 로컬 인사 재생\n경매장: 공통 상태로 재생',7:'',8:'',9:''}),
('모든 화면에서 같은 설정 사용','메인·로비·인게임에 공통 옵션과 조작 규칙을 적용했습니다.',{3:'음량·감도·화면 설정',4:'키보드와 마우스 지원 · 설정 저장',5:'게임 메뉴와 메인 복귀',6:'메뉴가 열리면 이동·시점 입력 제한',7:''}),
('최종 자산과 라운드별 변화 확인','순위와 함께 현금·주식 평가액, 경기 중 자산 흐름을 보여줍니다.',{3:'최종 현금과 주식 평가액',4:'최종 주가를 기준으로 주식 평가',5:'라운드별 자산 그래프',6:'시작 시점과 매 라운드 종료 자산 기록',7:''}),
('UI 입력과 서버 판정의 분리','화면은 요청을 보내고, 실제 자산 변경은 서버 결과를 반영합니다.',{3:'입력',4:'TradePanelController\n탭·수량·확정',6:'요청',7:'LocalTradeController\nPlayerNet.CmdTrade',9:'서버 판정',10:'MatchEngine\nTradeService',12:'UI 갱신',13:'시장 재고\n본인 Portfolio',14:'공개 상태',15:'라운드 · 시세 · 시장 재고',16:'본인 전용 상태',17:'현금 · 보유량 · 낙찰 정보',18:'기존 네트워크 기반에 기능별 UI를 연결했습니다.'}),
('본인 입찰 인상 시 차액만 예약','최고 입찰자가 다시 올릴 때 기존 예약금을 활용하도록 수정했습니다.',{3:'기존 입찰 $5 · 예약 $5',4:'추가로 필요한 예약금은 $3',5:'변경 입찰 $8 · 예약 $8',6:'총액을 중복 예약하지 않도록 처리',7:'로컬 호스트에서 입찰 $5 → $8, 예약금 $5 → $8 확인'}),
('회고와\n다음 검증','세계관·공간의 의도를\n실제 선택으로 연결',{3:'구현한 경험',4:'거래·경매를 공간에 배치하고\n정보 범위에 맞춰 UI를 구성',5:'다음 검증',6:'다인 플레이의 이동 부담\n초심자의 규칙 이해와 가독성'})
]

def text(sp):return '\n'.join(t.text or '' for t in sp.findall('.//a:t',ns))
def settext(sp,value,size=None,bold=None,color=None):
 body=sp.find('p:txBody',ns)
 if body is None:return
 old=body.find('a:p',ns)
 rpr=old.find('.//a:rPr',ns) if old is not None else None
 rpr=deepcopy(rpr) if rpr is not None else E.Element(q('a','rPr'))
 if size:rpr.set('sz',str(round(size*75)))
 if bold is not None:rpr.set('b','1' if bold else '0')
 if color:
  for e in list(rpr):
   if e.tag in [q('a','solidFill'),q('a','gradFill')]:rpr.remove(e)
  E.SubElement(E.SubElement(rpr,q('a','solidFill')),q('a','srgbClr'),val=color)
 for e in list(body):
  if e.tag==q('a','p'):body.remove(e)
 for line in value.split('\n'):
  p=E.SubElement(body,q('a','p'))
  pp=E.SubElement(p,q('a','pPr'));E.SubElement(E.SubElement(pp,q('a','lnSpc')),q('a','spcPct'),val='110000')
  r=E.SubElement(p,q('a','r'));r.append(deepcopy(rpr));E.SubElement(r,q('a','t')).text=line

def frame(el,x,y,w,h):
 xf=el.find('p:spPr/a:xfrm',ns)
 if xf is None:return
 off=xf.find('a:off',ns);ext=xf.find('a:ext',ns)
 for k,v in [('x',x),('y',y)]:off.set(k,str(round(v*EMU)))
 for k,v in [('cx',w),('cy',h)]:ext.set(k,str(round(v*EMU)))

def crop(pic,l,t,r,b):
 fill=pic.find('p:blipFill',ns)
 sc=fill.find('a:srcRect',ns)
 if sc is None:sc=E.Element(q('a','srcRect'));fill.insert(1,sc)
 for k,v in [('l',l),('t',t),('r',r),('b',b)]:sc.set(k,str(v))

with ZipFile(B/'reordered.pptx') as z:contents={n:z.read(n) for n in z.namelist()}
plan=[]
for n,(title,sub,changes) in enumerate(data,1):
 name=f'ppt/slides/slide{n}.xml';root=E.fromstring(contents[name]);sh=root.findall('.//p:sp',ns);pics=root.findall('.//p:pic',ns)
 cover=n in [1,18]
 if cover:
  settext(sh[1],title,60,True,'FFFFFF');settext(sh[2],sub,29,True,'E4C58B')
 else:
  settext(sh[0],title,54,True,'123A37');frame(sh[0],64,46,1152,74)
  settext(sh[1],sub,24,False,'52635D');frame(sh[1],64,133,1152,44)
  settext(sh[2],f'{n:02d}',16)
 for j,v in changes.items():settext(sh[j],v)
 if cover:
  for j in [3,5]:
   if j<len(sh):settext(sh[j],changes.get(j,text(sh[j])),25,True,'FFFFFF')
  for j in [4,6]:
   if j<len(sh):settext(sh[j],changes.get(j,text(sh[j])),23,False,'CCD7D0')
 elif n in [3,7,13]:
  for j,yy in [(3,225),(5,410)]:
   frame(sh[j],64,yy,350,44);settext(sh[j],changes[j],28,True,'123A37')
   frame(sh[j+1],64,yy+58,350,110);settext(sh[j+1],changes[j+1],25)
  if pics:frame(pics[0],440,208,776,436)
 elif n==4:
  for j,im in enumerate(pics):
   frame(im,64+j*232,220,216,300)
   if j<3:crop(im,35000,15000,35000,10000)
   elif j==3:crop(im,21000,0,21000,0)
  for j in range(3,8):settext(sh[j],text(sh[j]),24,True)
  settext(sh[8],changes[8],24)
 elif len(pics)==2:
  for j in [3,5]:settext(sh[j],changes[j],27,True,'123A37')
  for j in [4,6]:settext(sh[j],changes[j],24)
 if n==16:
  # Keep the existing editable request flow while shortening technical labels.
  for j in [3,6,9,12,14,16]:settext(sh[j],changes[j],29,True)
  for j in [4,7,10,13,15,17]:settext(sh[j],changes[j],24)
 if n==17:settext(sh[7],changes[7],20,False,'68766E')
 contents[name]=E.tostring(root,xml_declaration=True,encoding='UTF-8',standalone=True)
 plan.append({'page':n,'title':title.replace('\n',' '),'summary':sub.replace('\n',' '),'points':list(changes.values())})
 # Preserve source notes and append detail moved out of main slides.
 note=f'ppt/notesSlides/notesSlide{n}.xml'
 if note in contents:
  nr=E.fromstring(contents[note]);sp=nr.find('.//p:sp[p:txBody]',ns)
  if sp is not None:
   existing=text(sp)
   moved=''
   if n==2:moved='\n개인 담당: 후일담 테마, 캐릭터 설정, 레벨 구조설계, UI 시안·조립·상태 연결. 모델·원본 애니메이션·네트워크 전체의 단독 제작을 의미하지 않는다.'
   if n==16:moved='\n기능별 책임: NetworkPlayerHUD는 공개 MatchNet과 본인 Portfolio 표시. TradePanelController는 거래 입력. NetworkNotebookController는 소유 정보 반영. NetworkMatchFeedback은 성공·실패·낙찰·결과 알림. GameMenuController는 공통 메뉴·커서·설정 입력.'
   if n==17:moved='\n기존 검증 기록: 2026-10-22 규칙 EditMode 21개, 호스트 PlayMode 2개 통과. 이번 문서 촬영은 1인 로컬 호스트에서 시간을 단축한 별도 시나리오이며 사람 간 다인 플레이 검증과 구분한다.'
   settext(sp,existing+moved)
   contents[note]=E.tostring(nr,xml_declaration=True,encoding='UTF-8',standalone=True)
with ZipFile(B/'candidate.pptx','w',ZIP_DEFLATED) as z:
 for name,b in contents.items():z.writestr(name,b)
(B/'revised-outline.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
md='# 기술문서 구성 수정안\n\n기존 24장에서 18장으로 압축. 제목 40.5pt, 본문 약 18pt, 핵심 설명 중심으로 정리.\n\n| 페이지 | 제목 | 핵심 내용 |\n|---|---|---|\n'
for p in plan:md+=f"| {p['page']:02} | {p['title']} | {p['summary']} |\n"
md+='\n## 정리 원칙\n\n- 담당 범위는 도입부의 발표자 노트로 통합\n- HUD 원칙과 실제 HUD 설명을 한 페이지로 통합\n- 컴포넌트 목록과 검증 조건은 관련 페이지의 발표자 노트에 유지\n- 과거 밸런스 실험과 제작 도구 부록은 본문에서 제외\n- 실제 게임 스크린샷과 편집 가능한 텍스트 유지\n'
Path('Docs/TechnicalPresentation/페이지별_구성_v4.md').write_text(md,encoding='utf-8')
print('Revised 18 slides')
