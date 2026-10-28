import fs from 'node:fs/promises';
export const ROOT='C:/훈련생폴더/3. 유니티/Buy_Bid_Blow';
export const BASE=ROOT+'/Docs/TechnicalPresentation/.v6';
const C={bg:'#F5F2EA',ink:'#123A37',body:'#42544E',gold:'#8B632B',cream:'#F5F2EA',light:'#D5DFD9',muted:'#68766E'};
const FONT='Malgun Gothic';
function text(s,t,x,y,w,h,size=27,bold=false,color=C.body,fill='none'){
 const z=s.shapes.add({geometry:'textbox',name:t.slice(0,40),position:{left:x,top:y,width:w,height:h},fill,line:{fill:'none',width:0}});
 z.text=t;z.text.style={typeface:FONT,fontSize:size,bold,color,wrap:'square',autoFit:'none',alignment:'left',verticalAlignment:'top',insets:0,lineSpacing:1.12};return z;
}
async function img(s,name,x,y,w,h,fit='contain'){
 const bytes=await fs.readFile(BASE+'/assets/'+name+'.jpg');
 s.images.add({blob:new Uint8Array(bytes),contentType:'image/jpeg',alt:'실제 게임 스크린샷: '+name,position:{left:x,top:y,width:w,height:h},fit});
}
function noteBlock(s,h,b,x,y,w=350){text(s,h,x,y,w,44,29,true,C.ink);text(s,b,x,y+53,w,122,26);}
function marker(s,n,x,y){text(s,n,x,y,38,40,31,true,C.ink,C.bg);}
const titles=['BUY, BID, BLOW','목차','게임 개요와 담당 범위','후일담 세계관','캐릭터의 역할 구분','레벨 구조와 이동 동선','메인과 로비','고정 HUD와 상황별 UI','주식 거래 UI','공개·비공개 경매','개인 정보 수첩','HowToPlay','행동 결과와 사운드','경매품과 NPC 연출','공통 설정과 게임 메뉴','결과와 자산 변화','UI와 서버의 역할','본인 입찰 인상 수정','회고와 다음 검증'];
const intent=['','기획 의도와 실제 구현을 함께 설명합니다.','정보를 사는 비용과 투자에 남길 현금을 함께 판단하는 게임입니다.','동화의 소재를 회사로, 돼지와 늑대의 관계를 투자로 옮겼습니다.','회사 색상과 상징으로 운영자와 플레이어의 역할을 구분했습니다.','거래와 정보 경매를 오가는 이동도 플레이어의 선택이 되도록 설계했습니다.','규칙 학습과 참가 준비를 다른 단계로 나눴습니다.','상단에서 현재 상태를 확인하고, 행동할 때만 조작창을 보여줍니다.','주문하기 전에 비용과 거래 후 잔액을 확인하도록 구성했습니다.','공개 범위에 맞춰 최고 입찰과 본인 제출 정보를 구분했습니다.','획득한 정보만 라운드와 회사의 교차점에 기록합니다.','규칙을 길게 읽기보다 실제 화면에서 조작 순서를 따라가도록 구성했습니다.','입력 신호와 서버가 확정한 성공 결과를 구분했습니다.','상품 전시와 NPC 반응을 게임 판정에서 분리했습니다.','어느 화면에서든 같은 설정과 입력 규칙을 사용하도록 구성했습니다.','최종 순위와 함께 자산을 구성한 값과 경기 중 변화를 보여줍니다.','UI는 요청을 보내고, 실제 자산 변경은 서버 결과로 갱신합니다.','기존 예약금을 활용해 추가로 필요한 금액만 묶도록 수정했습니다.',''];
export async function build(p){
 const notes=JSON.parse(await fs.readFile(BASE+'/notes.json','utf8'));
 for(let i=0;i<19;i++){
  const n=i+1,s=p.slides.add({width:1280,height:720});s.background.fill=C.bg;
  s.speakerNotes.text=notes[i]+'\n설계 의도: '+intent[i]+'\n이미지 원본: '+ROOT+'/Logs/TechnicalPresentation. 확대 이미지는 실제 캡처의 일부를 자른 것으로 값을 변경하지 않았다.';
  if(n!==1&&n!==19){
   // Preserve the user-requested heading area as an editable flat band.
   s.shapes.add({geometry:'rect',name:'제목 영역',position:{left:0,top:0,width:1280,height:140},fill:C.ink,line:{fill:'none',width:0}});
   const section=n===2?'CONTENTS':n<=6?'01  기획과 공간':n<=16?'02  화면과 플레이 경험':'03  구현과 문제 해결';
   text(s,section,56,12,1100,24,15,true,'#E4C58B');
   text(s,titles[i],56,40,1168,60,49,true,C.cream);text(s,intent[i],56,103,1168,32,22,false,C.light);
   text(s,String(n).padStart(2,'0'),1180,681,50,24,16,false,C.muted);
  }
  if(n===1||n===19){
   s.background.fill=C.ink;await img(s,'World_ExchangeShot_Start',490,0,790,720,'cover');
   s.shapes.add({geometry:'rect',position:{left:0,top:0,width:490,height:720},fill:C.ink,line:{fill:'none',width:0}});
   text(s,n===1?'BUY, BID,\nBLOW':'회고와\n다음 검증',56,74,400,160,61,true,C.cream);
   text(s,n===1?'세계관·공간 설계\nUI 구현과 연결':'세계관·공간의 의도를\n화면과 행동으로 구체화',56,270,390,106,30,true,'#E4C58B');
   if(n===1){text(s,'개인 담당',56,441,380,42,26,true,C.cream);text(s,'후일담 테마·캐릭터 설정\n레벨 구조설계\nUI 제작 및 상태·입력 연결',56,495,380,150,25,false,C.light);}
   else{ text(s,'구현한 경험',56,420,380,40,26,true,C.cream);text(s,'정보 범위에 맞는 UI\n거래와 경매를 오가는 공간',56,470,380,90,25,false,C.light);text(s,'다음 검증',56,578,380,40,26,true,C.cream);text(s,'다인 플레이의 이동 부담\n초심자의 규칙 이해와 가독성',56,626,390,76,23,false,C.light);}
  }
  if(n===2){const rows=[['01','기획과 공간','게임 개요 / 세계관 / 캐릭터 / 레벨 구조','03–06'],['02','화면과 플레이 경험','진입 / HUD / 거래·경매 / 수첩 / 피드백 / 결과','07–16'],['03','구현과 문제 해결','UI와 서버의 역할 / 본인 입찰 인상','17–18'],['04','회고','구현한 경험과 다음 검증','19']];rows.forEach((r,j)=>{const y=185+j*119;text(s,r[0],56,y,90,55,40,true,C.gold);text(s,r[1],175,y,850,45,32,true,C.ink);text(s,r[2],175,y+48,850,38,25);text(s,r[3],1090,y+12,126,42,26,true,C.gold);});}
  if(n===3){await img(s,'World_AuctionShot_Start',56,190,740,416);text(s,'정보 획득  →  투자  →  자산 평가',56,628,760,42,28,true,C.gold);noteBlock(s,'기획','후일담 테마·캐릭터 설정\n층별 기능과 이동 동선',852,195,372);noteBlock(s,'UI 제작·연결','화면·프리팹 구성\n거래·경매 상태 연결\n입력과 화면 갱신',852,390,372);text(s,'모델·네트워크 기반은 팀 작업과 구분',852,625,372,56,21,false,C.muted);}
  if(n===4){await img(s,'World_ExchangeShot_End',56,200,740,416);noteBlock(s,'짚·나무·벽돌의 회사','Straw · Timber · Brick에\n원작의 소재를 대응',852,213,372);noteBlock(s,'돼지와 늑대의 역할','돼지는 회사를 운영하고\n늑대는 투자자로 참여',852,414,372);}
  if(n===5){for(let j=0;j<5;j++){await img(s,'character-'+j,56+j*235,195,220,330,j===4?'contain':'cover');text(s,['Straw 운영자','Timber 운영자','Brick 운영자','경매 진행자','늑대 투자자'][j],56+j*235,548,220,46,24,true,C.ink);}text(s,'회사 색상과 상징을 캐릭터 외형·간판에 함께 사용했습니다.',56,631,1160,44,28);}
  if(n===6){await img(s,'World_Stairs',56,177,860,484);marker(s,'①',160,580);marker(s,'②',637,437);marker(s,'③',485,326);noteBlock(s,'① 1층 거래소','회사 창구에서 거래',958,195,266);noteBlock(s,'② 계단 연결','경매 참여를 위해 이동',958,357,266);noteBlock(s,'③ 2층 경매장','상품을 향한 참여석',958,519,266);}
  if(n===7){text(s,'메인: 규칙을 배우는 진입점',56,174,552,44,27,true,C.ink);text(s,'로비: 캐릭터로 참가 준비',672,174,552,44,27,true,C.ink);await img(s,'01_Main',56,238,552,311);await img(s,'03_Lobby',672,238,552,311);text(s,'시작·HowToPlay·설정',56,583,552,70,28);text(s,'닉네임·외형·준비 완료',672,583,552,70,28);}
  if(n===8){await img(s,'04_Explore',56,170,900,506);marker(s,'①',295,245);marker(s,'②',885,240);marker(s,'③',447,405);noteBlock(s,'① 자산','현금·보유량을\n한곳에서 확인',1000,198,224);noteBlock(s,'② 진행','라운드·시간과\n현재 단계',1000,361,224);noteBlock(s,'③ 행동','가까운 대상의\n조작 안내',1000,524,224);}
  if(n===9){await img(s,'trade',80,174,500,500);marker(s,'①',352,402);marker(s,'②',40,550);marker(s,'③',393,600);noteBlock(s,'① 수량 선택','수량을 바꾸면 예상 비용 갱신',650,204,574);noteBlock(s,'② 주문 금액','확정 전에 지출을 확인',650,366,574);noteBlock(s,'③ 거래 후 현금','잔액을 보고 다음 행동 판단',650,528,574);}
  if(n===10){text(s,'공개 경매',56,182,552,50,31,true,C.ink);text(s,'비공개 경매',672,182,552,50,31,true,C.ink);await img(s,'public',56,260,552,120);await img(s,'secret',672,260,552,120);text(s,'최고 입찰 공개',56,425,552,48,33,true,C.gold);text(s,'내 금액만 표시',672,425,552,48,33,true,C.gold);text(s,'최고 입찰 취소 불가\n본인 최고가도 추가 인상 가능',56,490,552,125,29);text(s,'마감 전 수정·취소 가능\n타인의 제출 금액은 비공개',672,490,552,125,29);}
  if(n===11){text(s,'획득 전',56,174,552,44,29,true,C.ink);text(s,'획득 후',672,174,552,44,29,true,C.ink);await img(s,'empty',56,227,552,405);await img(s,'owned',672,227,552,405);text(s,'모르는 정보는 빈칸 유지',56,645,552,38,27);text(s,'낙찰받은 방향 정보만 기록',672,645,552,38,27);}
  if(n===12){await img(s,'guide',56,182,860,479);noteBlock(s,'한 페이지 한 주제','거래·경매 분리',964,210,260);noteBlock(s,'핵심은 세 단계','큰 글자로\n순서 안내',964,371,260);noteBlock(s,'실제 UI 예시','설명과 버튼을\n나란히 배치',964,532,260);}
  if(n===13){await img(s,'won',56,220,780,448);text(s,'실제 낙찰 알림',56,169,780,40,26,true,C.ink);noteBlock(s,'서버 결과를 기준으로','거래·베팅·낙찰 성공 응답 뒤\n결과 알림과 효과음을 재생',884,240,340);noteBlock(s,'의미가 다른 효과음','입력 확인·낙찰·정보 열람\n각각 다른 큐로 구분',884,466,340);}
  if(n===14){await img(s,'envelope',56,185,700,470);noteBlock(s,'경매품 전시','경매 중에만 봉투 표시·회전\n앞면에는 대상 라운드 표기',812,205,412);noteBlock(s,'NPC 반응 범위','거래소 인사는 로컬 재생\n경매 진행자는 공통 상태에 반응',812,432,412);text(s,'실제 경매품·받침대 확대',56,657,700,32,21,false,C.muted);}
  if(n===15){await img(s,'settings',56,170,650,494);noteBlock(s,'설정과 저장','음량·감도·화면 설정을 공통 사용\n키보드와 마우스 입력 지원',762,218,462);noteBlock(s,'게임 메뉴의 입력 전환','메뉴를 열면 이동·시점 입력 제한\n닫으면 플레이 조작으로 복귀',762,440,462);}
  if(n===16){text(s,'최종 자산의 구성',56,175,552,44,28,true,C.ink);text(s,'라운드별 자산 변화',672,175,552,44,28,true,C.ink);await img(s,'result',56,232,552,352);await img(s,'graph',672,232,552,352);text(s,'현금과 회사별 주식 평가액',56,607,552,55,28);text(s,'시작과 매 라운드 종료 시점 기록',672,607,552,55,28);}
  if(n===17){const h=['입력','요청','서버 판정','UI 갱신'],b=['TradePanelController\n탭·수량·확정','LocalTradeController\nPlayerNet.CmdTrade','MatchEngine\nTradeService','공개 시장 재고\n본인 Portfolio'];for(let j=0;j<4;j++){const x=56+j*300;text(s,h[j],x,212,255,52,32,true,C.ink);text(s,b[j],x,280,255,120,24);if(j<3)text(s,'→',x+255,275,45,52,34,true,C.gold);}noteBlock(s,'공개 상태','라운드·시세·시장 재고',56,476,545);noteBlock(s,'본인 전용 상태','현금·보유량·낙찰 정보',672,476,552);text(s,'기존 네트워크 기반에 기능별 UI를 연결했습니다.',56,640,1168,42,28,true,C.gold);}
  if(n===18){text(s,'입찰 $5 → $8',56,219,540,70,49,true,C.ink);text(s,'추가 예약 $3',56,306,540,75,52,true,C.gold);text(s,'기존 $5를 유지하고\n차액 $3만 더 묶기',56,423,540,125,31);await img(s,'bid',652,199,572,117);text(s,'실제 최고 입찰 $8 표시',652,334,572,40,23,false,C.muted);noteBlock(s,'수정과 검증','호스트에서 예약금 $5 → $8 확인\n거절되면 기존 입찰·예약 유지',652,443,572);text(s,'로컬 호스트 검증 · 다인 플레이 검증과 구분',56,647,1168,38,22,false,C.muted);}
 }
 return {slides:p.slides.items.length};
}
