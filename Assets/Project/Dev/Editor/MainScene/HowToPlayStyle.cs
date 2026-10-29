using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using BuyBidBlow.UI.Menu;
namespace BuyBidBlow.Dev.Editor {
// Builds the shared three-row tutorial layout from editable uGUI elements.
static class HowToPlayStyle {
 static readonly Color Ink=new Color(.025f,.055f,.055f),Gray=new Color(.27f,.29f,.28f),Red=new Color(.49f,.105f,.08f),Cream=new Color(.98f,.95f,.84f),Teal=new Color(.025f,.13f,.12f);
 static Font bold,black;
 const string Root="Assets/Project/UI/Menu/HowToPlay";
 static RectTransform R(string name,Transform parent,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);Pos(r,x,y,w,h);return r;}
 static void Pos(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
 static Text T(string name,Transform p,string value,float x,float y,float w,float h,int size,Color c){var t=R(name,p,x,y,w,h).gameObject.AddComponent<Text>();t.font=bold;t.text=value;t.fontSize=size;t.color=c;t.raycastTarget=false;t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
 static Image Box(string name,Transform p,float x,float y,float w,float h,Color c){var i=R(name,p,x,y,w,h).gameObject.AddComponent<Image>();i.color=c;i.raycastTarget=false;return i;}
 static void Badge(Transform p,int n,float y){var r=R("Step Number "+n,p,125,y+26,66,66);r.gameObject.AddComponent<CanvasRenderer>();var c=r.gameObject.AddComponent<DossierCircle>();c.color=Red;c.raycastTarget=false;T("Number",r,n.ToString(),0,0,66,66,34,Cream).alignment=TextAnchor.MiddleCenter;}
 static void Key(Transform p,string key,float x,float y,float w=70){Box("Keycap "+key,p,x,y,w,57,new Color(.17f,.22f,.21f));T("Key",p,key,x,y,w,57,29,Cream).alignment=TextAnchor.MiddleCenter;}
 static Transform Card(Transform p,int row){return Box("Example "+(row+1),p,800,300+row*168,685,150,Teal).transform;}
 static void Big(Transform p,string value,string caption){T("Example Value",p,value,26,16,633,69,43,Cream);T("Example Caption",p,caption,26,90,633,43,25,new Color(.78f,.84f,.78f));}
 static void Icon(Transform p,string company,float x,float y,float size=60){var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/UI/Market/TradingPanel/Sprites/IC_"+company+".png");var i=Box("Icon "+company,p,x,y,size,size,Color.white);i.sprite=sprite;i.preserveAspect=true;}
 static void SceneExample(Transform p,string id,string caption){var s=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Examples/"+(id=="Explore"?"Clerk.png":id+".asset"));var image=Box("Actual Scene Example",p,0,0,390,150,Color.white);image.sprite=s;image.preserveAspect=true;Key(p,"E",425,27);T("Interaction Hint",p,caption,515,24,150,96,26,Cream);}
 static void AuctionChairExample(Transform p){
 const string asset=Root+"/Examples/AuctionChairCloseup.asset";
 var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Examples/AuctionChairInteraction.png");
 if(!tex)throw new System.InvalidOperationException("Capture the auction chair interaction before rebuilding HowToPlay.");
 var generated=Sprite.Create(tex,new Rect(276,470,1368,300),new Vector2(.5f,.5f),100);
 var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(asset);
 if(sprite){EditorUtility.CopySerialized(generated,sprite);Object.DestroyImmediate(generated);}else{sprite=generated;AssetDatabase.CreateAsset(sprite,asset);}
 var image=Box("Auction Chair Closeup",p,0,0,685,150,Color.white);image.sprite=sprite;
 }
 static void TradeSection(Transform parent,string path,float x,float y,float w,float h,int selected=0){
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/UI/Market/TradingPanel/PF_TradingPanel_Straw.prefab");var source=Object.Instantiate(prefab);source.SetActive(false);
 try{int q=1;source.GetComponent<BuyBidBlow.UI.Market.TradingPanelView>().Present(new BuyBidBlow.UI.Market.TradingPanelData{company="Straw",price=26,available=200,stock=20,owned=0,change=9},selected,ref q);
 var original=(RectTransform)source.transform.Find(path);var clone=Object.Instantiate(original.gameObject,parent);clone.name="Actual UI - "+path;clone.SetActive(true);var rect=(RectTransform)clone.transform;
 float scale=Mathf.Min(w/original.rect.width,h/original.rect.height);Pos(rect,x,y,original.rect.width,original.rect.height);rect.localScale=Vector3.one*scale;
 foreach(var b in clone.GetComponentsInChildren<Button>(true)){b.interactable=false;b.transition=Selectable.Transition.None;b.navigation=new Navigation{mode=Navigation.Mode.None};}
 foreach(var g in clone.GetComponentsInChildren<Graphic>(true))g.raycastTarget=false;
 }finally{Object.DestroyImmediate(source);}}
 static void Visual(Transform p,int page,int row){var c=Card(p,row);
 switch(page){
 case 0: if(row==0)Big(c,"$200  /  0주","시작 현금과 보유 주식");else if(row==1){Icon(c,"Straw",30,27);Icon(c,"Timber",111,27);Icon(c,"Brick",192,27);T("Tools",c,"거래 · 정보 · 베팅",290,25,360,95,34,Cream);}else Big(c,"현금 + 주식 가치","13라운드 종료 후 총자산 비교");break;
 case 1: if(row==0){Key(c,"W",102,11);Key(c,"A",23,78);Key(c,"S",102,78);Key(c,"D",181,78);T("Move and Look",c,"마우스: 시점\nSpace: 점프",300,23,350,108,30,Cream);}else if(row==1)SceneExample(c,"Explore","창구·의자\n상호작용");else{Key(c,"Tab",27,23,104);T("Notebook Controls",c,"내 정보 수첩\nW/S 스크롤 · Q/Tab 닫기",164,21,495,111,28,Cream);}break;
 case 2: if(row==0)SceneExample(c,"Explore","거래창\n열기");else if(row==1){TradeSection(c,"Trade Tabs",12,3,660,46);TradeSection(c,"Quantity Control",12,53,660,94);}else TradeSection(c,"Payment Summary",12,3,660,145);break;
 case 3: if(row==0)AuctionChairExample(c);else if(row==1){T("Lot Round",c,"6R",25,15,90,50,33,Cream);Icon(c,"Straw",145,10,52);Icon(c,"Timber",220,10,52);T("Lot Round 2",c,"9R",25,79,90,50,33,Cream);Icon(c,"Timber",145,76,52);Icon(c,"Brick",220,76,52);T("Hidden Directions",c,"대상은 공개\n방향은 낙찰 후 확인",326,20,340,110,28,Cream);}else{Key(c,"A",25,30);T("Bid Amount",c,"$36",127,30,155,62,44,Cream);Key(c,"D",292,30);Key(c,"E",422,30);T("Submit",c,"입찰",516,30,146,64,34,Cream);}break;
 case 4: if(row==0){Key(c,"Tab",28,30,110);BigText(c,"정보 수첩",190,28,450,47,36);T("Private",c,"내가 낙찰받은 정보만 기록",190,85,450,38,25,Cream);}else if(row==1){T("Round",c,"5R",30,25,100,65,43,Cream);Icon(c,"Straw",150,20,68);T("Direction",c,"상승",265,25,350,65,43,new Color(1,.5f,.42f));T("Unknown",c,"빈칸 = 아직 모르는 정보",30,105,620,35,25,Cream);}else Big(c,"4R 종가 $20  →  5R 종가 $24","‘5R 상승’은 바로 전 라운드 종가와 비교");break;
 case 5: if(row==0)TradeSection(c,"Trade Tabs",12,43,660,90,2);else if(row==1)Big(c,"현재가 $40  →  참가금 $40","해당 회사 주식이 없어도 참여 가능");else{T("Win",c,"하락 성공  $80",25,20,625,49,37,new Color(.5f,.8f,1));T("Lose",c,"상승·보합  $0",25,83,625,49,33,Cream);}break;
 case 6: if(row==0)Big(c,"13 / 13 ROUND","마지막 주가와 베팅 정산까지 반영");else if(row==1)Big(c,"현금 $80 + 주식 $100 = $180","주식 $100 = 2주 × $30 + 1주 × $40");else{T("Winner",c,"1위  나       $180",25,12,625,60,40,new Color(1,.81f,.35f));T("Other ranks",c,"2위  플레이어 B       $160",25,84,625,45,29,Cream);}break;
 }
 }
 static void BigText(Transform p,string value,float x,float y,float w,float h,int size){T("Example Value",p,value,x,y,w,h,size,Cream);}
 public static void Apply(HowToPlayPanel nav){
 HowToPlayClerkPreview.Create();
 bold=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/alternative/Pretendard-Bold.ttf");black=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/alternative/Pretendard-Black.ttf");
 string[] titles={"가장 부유한 플레이어가 되세요!","이동하고, 바라보고, E!","주식 거래, 세 단계면 끝!","미래 정보를 입찰로 가져오세요!","얻은 정보로 다음 거래를 준비하세요!","가격이 내려가도 수익을 얻어요!","현금과 주식 가치를 더하세요!"};
 string[] subtitles={"거래와 정보를 활용해 가장 많은 자산을 모으세요.","가까운 대상은 화면의 상호작용 안내로 확인하세요.","창구에서 사고팔며 자산을 늘리세요.","경매 상품은 미래 주가의 방향 정보입니다.","라운드와 회사를 확인하고 마감 전에 활용하세요.","현재가 1주분으로 해당 라운드의 하락에 베팅하세요.","주식을 팔지 않아도 마지막 가격으로 점수에 포함됩니다."};
 string[][] heads={new[]{"$200으로 시작","자산을 늘리세요","13라운드 후 비교"},new[]{"WASD로 이동","대상을 보고 E","Tab으로 내 정보"},new[]{"창구에서 E","종류와 수량 선택","금액 확인 후 E"},new[]{"경매 의자에서 E","상품의 대상을 확인","금액 선택 후 E"},new[]{"Tab으로 수첩 열기","라운드와 방향 확인","마감 전에 활용"},new[]{"하락 베팅 선택","현재가 1주분 참가","마감 결과 확인"},new[]{"마지막 가격 반영","현금과 주식 합산","최종 순위 확인"}};
 string[][] bodies={new[]{"현금 $200 · 보유 주식 0주","주식 거래 · 정보 경매 · 하락 베팅","현금 + 주식 가치가 가장 높으면 승리"},new[]{"마우스 시점 · Space 점프","창구 이용 또는 경매 의자에 앉기","W/S 스크롤 · Q/Tab 닫기"},new[]{"회사 돼지를 바라보고 거래창 열기","W/S 매수·매도 · A/D 수량","합계·남은 현금 확인 · E 또는 클릭"},new[]{"의자에 앉으면 입찰 UI가 나타나요","어느 라운드, 어느 회사 정보인지 확인","A/D 금액 · E 입찰 · Q 퇴석"},new[]{"낙찰받은 정보는 나만 확인해요","상승·하락·보합 · 빈칸은 미보유","예: 5R 상승은 4R 종가보다 높다는 뜻"},new[]{"회사 창구에서 W/S로 탭 선택","E 또는 버튼으로 참가","하락: 총 2배 지급 · 상승·보합: 손실"},new[]{"13라운드 종가와 베팅 정산 후 종료","회사별 보유 주식 × 최종 주가","총자산이 높을수록 높은 순위"}};
 string[] rules={"현금 $200 · 주식 0주로 시작합니다. 최소 3명, 최대 6명입니다.\n\n일반 라운드 60초 · 경매 라운드 120초\n2 · 6 · 10R 경매 / 기본 경기 시간 총 16분 (전환 별도)\n\n주가는 라운드 마감에 바뀝니다. 거래량은 주가에 영향을 주지 않습니다.","E: 창구·의자 이용 / Q: 거래창 닫기·경매 의자 퇴석\nTab: 내 정보 수첩 열기 / 수첩 W/S 스크롤 · Q/Tab 닫기\n\n왼쪽 위: 내 자산 / 오른쪽 위: 라운드·남은 시간\n회사별 현재 주가와 직전 변화는 거래소 시세판에서 확인합니다.","회사별 공급 = 시작 인원 × 5 + 5주\n매도한 주식은 시장 재고로 반환 · 추가 발행 없음\n\n정수 단위 거래 · 수수료 없음 · 공매도 불가\nA: 수량 1에서 최대 / D: 최대에서 1\n현금·재고·보유량이 부족한 요청은 전체 거절됩니다.","공개 60초 → 비공개 30초 → 거래 시간 30초\n공개: 최고 입찰보다 $1 이상 높게 / 최고 입찰 취소 불가\n비공개: 마감 전 수정 가능 / C로 취소 가능\n\n동액이면 현재 금액을 먼저 제출한 사람이 우선합니다.\n금액을 바꾸면 우선 시각도 갱신됩니다.\n입찰금은 예약됩니다. Q 퇴석은 입찰 취소가 아닙니다.\n비공개 결과는 본인의 성공·실패만 확인합니다.","상품 하나 = 두 라운드의 정보 / 각 라운드마다 무작위 두 회사\n대상 라운드와 회사는 공개, 방향은 낙찰자만 확인합니다.\n같은 회사 조합이 반복될 수 있습니다.\n\n‘5R 하락’은 4R 종가보다 5R 종가가 낮다는 뜻입니다.\n정보 구매 순간의 가격과 비교하지 않습니다.\n상승 정보가 있어도 매수 시점에 따라 수익은 달라집니다.","최종 유지 베팅은 하나입니다. 참가금은 현재가 1주분입니다.\n마감 전 같은 창구에서 취소하면 참가금 전액 환불됩니다.\n취소 후 같은 라운드에 다시 참여할 수 있습니다.\n\n하한가 $5인 회사에는 참여할 수 없습니다.\n하락 성공: 참가금 포함 총 2배 / 상승·보합: 지급액 $0\n현재 라운드의 실제 종가로 판정합니다.","총자산 = 현금 + 회사별 보유 주식 × 최종 주가\n마지막 주가 변경과 베팅 정산까지 점수에 반영됩니다.\n주식을 미리 매도할 필요는 없습니다.\n\n총자산 동점 → 총 보유 주식 수 비교\n주식 수도 같으면 공동 순위입니다."};
 var folder=nav.transform.Find("File Folder");
 var series=folder.Find("Series").GetComponent<Text>();series.text="BUY, BID, BLOW  /  HOW TO PLAY";series.color=Gray;
 for(int k=0;k<7;k++){
 var p=nav.pages[k].transform;foreach(Transform child in p.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
 T("Page Title",p,titles[k],125,125,1360,82,64,Ink).font=black;
 T("Page Subtitle",p,subtitles[k],125,212,1360,47,29,Gray);
 Box("Header Divider",p,125,278,1360,1,new Color(.3f,.27f,.22f,.35f));
 var steps=R("Steps",p,0,0,1560,810);
 for(int row=0;row<3;row++){float y=300+row*168;Badge(steps,row+1,y);T("Step Title "+(row+1),steps,heads[k][row],218,y+13,560,62,43,Ink).font=black;T("Step Body "+(row+1),steps,bodies[k][row],218,y+76,560,78,31,Gray).font=black;Visual(steps,k,row);if(row<2)Box("Row Divider "+row,steps,125,y+158,1360,1,new Color(.3f,.27f,.22f,.22f));}
 if(k==3)T("Auction Comparison",steps,"공개 60초: 최고가 공개·취소 불가  /  비공개 30초: 내 금액만 표시·수정 및 취소 가능",125,792,1360,34,23,Ink);
 var detail=Box("Details",p,125,300,1360,500,new Color(.97f,.94f,.86f));
 T("Details Title",detail.transform,"자세한 규칙",25,15,1300,55,35,Ink);
 T("Details Body",detail.transform,rules[k],25,85,1295,400,27,Gray).alignment=TextAnchor.UpperLeft;detail.gameObject.SetActive(false);
 T("Details Toggle",p,"[E] 자세한 규칙 +",125,844,430,43,25,Ink);
 }
 Pos((RectTransform)folder.Find("Footer Rule"),100,858,1410,1);Pos(nav.previousHint.rectTransform,100,873,260,43);Pos(nav.counter.rectTransform,735,873,200,43);Pos(nav.nextHint.rectTransform,1200,873,175,43);Pos((RectTransform)folder.Find("Close"),1390,873,180,43);
 }
}
}
