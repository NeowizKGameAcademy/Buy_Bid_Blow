using System;
using System.IO;
using System.Linq;
using BuyBidBlow.UI.Shared;
using BuyBidBlow.UI.Lobby;
using BuyBidBlow.UI.Menu;
using BuyBidBlow.UI.Loading;
using BuyBidBlow.UI.Result;
using BuyBidBlow.UI.Round;
using BuyBidBlow.UI.Auction;
using BuyBidBlow.UI.Market;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad] public static class UIShowcaseBuilder
    {
        public const string Root="Assets/Project/Dev/Sandbox/UIShowcase";
        public const string ScenePath=Root+"/SC_UIShowcase.unity";
        private const string UI="Assets/Project/UI/";
        private static Font font,bold;
        private static Sprite panel,banner;
        private static Sprite[] buttons;
        private static readonly Color Cream=new Color(.99f,.95f,.82f),Gold=new Color(.95f,.76f,.37f),Muted=new Color(.74f,.79f,.74f);
        static UIShowcaseBuilder(){EditorApplication.update+=()=>{if(!File.Exists("Library/UIShowcaseBuild.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;File.Delete("Library/UIShowcaseBuild.request");try{Build();File.WriteAllText("Library/UIShowcaseBuild.result","PASS");}catch(Exception e){File.WriteAllText("Library/UIShowcaseBuild.result",e.ToString());Debug.LogException(e);}};}
        private static RectTransform R(string name,Transform p,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;
        }
        private static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        private static Image I(string n,Transform p,Sprite s,float x,float y,float w,float h,Color? color=null)
        {
            var i=R(n,p,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=s;i.raycastTarget=false;i.color=color??Color.white;if(s&&s.border.sqrMagnitude>0){i.type=Image.Type.Sliced;i.pixelsPerUnitMultiplier=2;}return i;
        }
        private static Text T(string n,Transform p,string text,float x,float y,float w,float h,int size=28,Color? color=null,TextAnchor align=TextAnchor.MiddleCenter)
        {
            var t=R(n,p,x,y,w,h).gameObject.AddComponent<Text>();t.font=bold;t.text=text;t.fontSize=size;t.color=color??Cream;t.alignment=align;t.raycastTarget=false;t.supportRichText=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        private static Button B(Transform p,string text,float x,float y,float w=280,float h=74)
        {
            var i=I(text,p,buttons[0],x,y,w,h);var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;b.transition=Selectable.Transition.SpriteSwap;b.spriteState=new SpriteState{highlightedSprite=buttons[1],selectedSprite=buttons[1],pressedSprite=buttons[2],disabledSprite=buttons[3]};b.navigation=new Navigation{mode=Navigation.Mode.None};
            var label=T("Label",i.transform,text,0,0,w-42,h-16,27);label.resizeTextForBestFit=true;label.resizeTextMinSize=20;label.resizeTextMaxSize=27;return b;
        }
        private static RectTransform Full(string name,Transform canvas){var r=R(name,canvas,0,0,1920,1080);Stretch(r);return r;}
        private static TView Modal<TView>(Transform canvas,string name,float width=900,float height=560,int count=2) where TView:ModalView
        {
            var root=Full(name,canvas);var shade=I("Backdrop Dim - opacity editable",root,null,0,0,1920,1080,new Color(0,0,0,.65f));Stretch(shade.rectTransform);shade.raycastTarget=true;
            var p=R("Panel",root,0,5,width,height);I("Frame - shared sliced sprite",p,panel,0,0,width,height);
            var v=root.gameObject.AddComponent<TView>();v.Title=T("Title",p,"안내",0,height/2-92,width-110,65,42,Gold);
            I("Title Rule",p,null,0,height/2-139,width-130,1,new Color(.78f,.62f,.30f,.55f));
            v.Body=T("Message",p,"안내 문구",0,height/2-201,width-135,94,28);
            v.CauseArea=R("Cause Area - optional",p,0,-35,width-130,125).gameObject;I("Cause Dim",v.CauseArea.transform,null,0,0,width-130,125,new Color(0,0,0,.25f));
            v.Cause=T("Cause Text - editable",v.CauseArea.transform,"오류 원인",0,0,width-180,104,25,Muted,TextAnchor.MiddleLeft);
            v.Buttons=new Button[count];for(int i=0;i<count;i++)v.Buttons[i]=B(p,"확인",(i-(count-1)/2f)*310,-height/2+94,count==3?240:280);
            v.Footer=T("Keyboard Hints",p,"←→ 선택 · Enter 확인 · Esc 닫기",0,-height/2+34,width-110,30,20,Muted);v.CauseArea.SetActive(false);return v;
        }
        private static void Save(Component c,string folder,string name){Directory.CreateDirectory(UI+folder);foreach(var child in c.GetComponentsInChildren<Transform>(true))child.gameObject.layer=5;c.gameObject.SetActive(false);PrefabUtility.SaveAsPrefabAsset(c.gameObject,UI+folder+"/"+name+".prefab");}
        private static Sprite Art(string id,string file)
        {
            string path=UI+"Shared/Modal/Art/"+file;AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();
            var raw=new Texture2D(2,2);raw.LoadImage(File.ReadAllBytes(path));var px=raw.GetPixels32();int l=raw.width,b=raw.height,r=0,t=0;for(int y=0;y<raw.height;y++)for(int x=0;x<raw.width;x++)if(px[y*raw.width+x].a>32){l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,y);t=Math.Max(t,y);}
            var s=Sprite.Create(AssetDatabase.LoadAssetAtPath<Texture2D>(path),new Rect(l,b,r-l+1,t-b+1),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(100,100,100,100));s.name=id;var ap=UI+"Shared/Modal/Art/"+id+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(ap);if(old){EditorUtility.CopySerialized(s,old);UnityEngine.Object.DestroyImmediate(s);s=old;}else AssetDatabase.CreateAsset(s,ap);UnityEngine.Object.DestroyImmediate(raw);return s;
        }
        private static NotificationStackView Notices(Transform canvas,string name,bool right)
        {
            var root=R(name,canvas,right?680:-680,185,490,400);root.anchorMin=root.anchorMax=new Vector2(right?1:0,1);root.pivot=new Vector2(right?1:0,1);root.anchoredPosition=new Vector2(right?-32:32,-230);
            var v=root.gameObject.AddComponent<NotificationStackView>();v.Cards=new GameObject[3];v.Titles=new Text[3];v.Details=new Text[3];v.Accents=new Image[3];
            for(int n=0;n<3;n++){var card=R("Notice "+(n+1),root,245,-60-n*126,490,114);card.anchorMin=card.anchorMax=new Vector2(0,1);v.Cards[n]=card.gameObject;I("Card Frame",card,banner,0,0,490,114);v.Accents[n]=I("Status Accent",card,null,-222,0,4,65,Gold);v.Titles[n]=T("Title",card,"알림",10,22,410,36,26,Gold,TextAnchor.MiddleLeft);v.Details[n]=T("Detail",card,"내용",10,-23,410,55,22,Cream,TextAnchor.MiddleLeft);card.gameObject.SetActive(false);}return v;
        }
        [MenuItem("Tools/Buy Bid Blow/UI/Build UI Showcase")]
        public static void Build()
        {
            Directory.CreateDirectory(Root);font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/alternative/Pretendard-Regular.ttf");bold=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/alternative/Pretendard-Bold.ttf");panel=Art("SP_ModalTeal","BG_ModalTeal.png");banner=Art("SP_BannerTeal","BG_BannerTeal.png");buttons=new[]{"Normal","Focused","Pressed","Disabled"}.Select(n=>AssetDatabase.LoadAllAssetsAtPath(UI+"Menu/Sprites/BTN_MainStates_v3.png").OfType<Sprite>().Single(s=>s.name==n)).ToArray();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try{
            var cam=new GameObject("Showcase Camera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(cam.gameObject,scene);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.05f,.045f);cam.orthographic=true;cam.cullingMask=1<<5;cam.tag="Untagged";
            var canvas=new GameObject("UI Showcase Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();SceneManager.MoveGameObjectToScene(canvas.gameObject,scene);canvas.gameObject.layer=5;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var bg=I("Environment - visual reference only",canvas.transform,AssetDatabase.LoadAssetAtPath<Sprite>(UI+"Menu/Sprites/BG_MainHall.png"),0,0,1920,1080);Stretch(bg.rectTransform);I("Environment Dim",canvas.transform,null,0,0,1920,1080,new Color(0,0,0,.27f));
            var demo=canvas.gameObject.AddComponent<UIShowcase>();
            demo.PreviewHud=R("Reference HUD - Dev labels only",canvas.transform,0,0,1920,1080).gameObject;
            I("Asset plate",demo.PreviewHud.transform,panel,-700,413,440,186);T("Cash",demo.PreviewHud.transform,"Total  $268\n$150     Straw 2 · Timber 2 · Brick 0",-700,420,375,120,25);
            I("Round plate",demo.PreviewHud.transform,banner,730,444,370,110);T("Clock",demo.PreviewHud.transform,"6 / 17 ROUND     03:00",730,444,315,60,25);
            demo.Error=Modal<ModalView>(canvas.transform,"Common Error",960,590);demo.Error.SetButtons("확인");demo.Error.Show("요청을 처리할 수 없습니다","아래 원인을 확인해주세요.","오류 원인");Save(demo.Error,"Shared/Modal","PF_ErrorModal");
            var buttonSample=R("Button State Sample",canvas.transform,0,0,1320,110);for(int i=0;i<4;i++){var bt=B(buttonSample,new[]{"기본","선택","눌림","비활성"}[i],(i-1.5f)*310,0);bt.image.sprite=buttons[i];if(i==3)bt.interactable=false;}PrefabUtility.SaveAsPrefabAsset(buttonSample.gameObject,UI+"Shared/Modal/PF_ButtonStates.prefab");UnityEngine.Object.DestroyImmediate(buttonSample.gameObject);
            demo.Connection=Modal<ConnectionPanelView>(canvas.transform,"Connection States",940,590);
            foreach(ConnectionPanelState state in Enum.GetValues(typeof(ConnectionPanelState))){demo.Connection.ShowState(state,state==ConnectionPanelState.Connecting?"":"연결 상태를 확인해주세요.","localhost:7777");Save(demo.Connection,"Lobby/Connection","PF_Connection_"+state);}
            demo.LeaveConfirm=Modal<ModalView>(canvas.transform,"Leave Confirmation",850,470);demo.LeaveConfirm.SetButtons("취소","나가기");demo.LeaveConfirm.Show("방을 나가시겠습니까?","진행 중인 경기를 떠납니다.");demo.LeaveConfirm.EscapeAction=0;Save(demo.LeaveConfirm,"Menu/Pause","PF_LeaveConfirm");
            demo.Pause=Modal<PauseMenuView>(canvas.transform,"Pause Menu",680,700,4);demo.Pause.VerticalButtons=true;demo.Pause.OpenMenu();for(int i=0;i<4;i++){var r=demo.Pause.Buttons[i].GetComponent<RectTransform>();r.anchoredPosition=new Vector2(0,130-i*95);r.sizeDelta=new Vector2(470,80);}demo.Pause.Body.rectTransform.anchoredPosition=new Vector2(0,-240);demo.Pause.Body.fontSize=23;demo.Pause.Footer.text="↑↓ 선택 · Enter 확인 · Esc 계속하기";Save(demo.Pause,"Menu/Pause","PF_PauseMenu");
            demo.Controls=Modal<ModalView>(canvas.transform,"Controls",900,720,1);demo.Controls.SetButtons("닫기");demo.Controls.Show("조작 안내","WASD  이동    /    마우스  시점\nSpace  점프    /    E  상호작용·확인\nI  정보 수첩    /    Q  닫기·일어서기\n←→  수량·입찰금    /    ↑↓  거래 메뉴\nC  비공개 입찰 취소    /    Alt  커서 표시");demo.Controls.Body.rectTransform.anchoredPosition=new Vector2(0,20);demo.Controls.Body.rectTransform.sizeDelta=new Vector2(710,320);demo.Controls.Body.lineSpacing=1.65f;Save(demo.Controls,"Menu/Pause","PF_Controls");
            BuildLoading(canvas.transform,demo);BuildResults(canvas.transform,demo);BuildSettings(canvas.transform,demo);
            demo.Auction=Modal<AuctionResultView>(canvas.transform,"Auction Result",850,480);demo.Auction.ShowPublic("루나",35,true);Save(demo.Auction,"Auction/Result","PF_AuctionResult");
            var round=R("Round Start",canvas.transform,0,235,720,170);I("Banner",round,banner,0,0,720,170);demo.Round=round.gameObject.AddComponent<RoundStartView>();demo.Round.RoundLabel=T("Round",round,"6 / 17 ROUND",0,27,620,62,44,Gold);demo.Round.Detail=T("Detail",round,"경매 라운드 · 공개 경매가 시작됩니다",0,-36,620,40,26);Save(demo.Round,"Round","PF_RoundStart");
            demo.MarketStack=Notices(canvas.transform,"Market Notices",false);demo.Market=demo.MarketStack.gameObject.AddComponent<MarketNoticeView>();demo.Market.Stack=demo.MarketStack;var commonStack=UnityEngine.Object.Instantiate(demo.MarketStack.gameObject,canvas.transform);UnityEngine.Object.DestroyImmediate(commonStack.GetComponent<MarketNoticeView>());PrefabUtility.SaveAsPrefabAsset(commonStack,UI+"Shared/Notification/PF_NotificationStack.prefab");UnityEngine.Object.DestroyImmediate(commonStack);Save(demo.Market,"Market/Notifications","PF_MarketNotices");demo.MarketStack.gameObject.SetActive(true);
            demo.LobbyStack=Notices(canvas.transform,"Lobby Notices",true);demo.Lobby=demo.LobbyStack.gameObject.AddComponent<LobbyNoticeView>();demo.Lobby.Stack=demo.LobbyStack;Save(demo.Lobby,"Lobby/Notifications","PF_LobbyNotices");demo.LobbyStack.gameObject.SetActive(true);
            var chrome=R("Dev Controls - not shipped",canvas.transform,0,0,1920,1080);I("Top strip",chrome,null,0,518,1920,44,new Color(0,0,0,.88f));demo.SpecimenLabel=T("Specimen",chrome,"",0,518,1780,38,22,Gold);I("Bottom strip",chrome,null,0,-510,1920,60,new Color(0,0,0,.88f));demo.Status=T("Hints",chrome,"",0,-510,1800,55,21,Muted);
            var es=new GameObject("Showcase EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));SceneManager.MoveGameObjectToScene(es,scene);var module=es.GetComponent<InputSystemUIInputModule>();module.AssignDefaultActions();module.move=null;module.submit=null;module.cancel=null;
            foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;
            demo.ShowCase(0);EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();Capture(demo,canvas,cam);File.WriteAllText(Root+"/BuildVerification.txt","PASS: UI prefab construction, 21 specimen states and screenshots. Runtime keyboard checks are separate. Production scenes/build settings not modified.\n");
            }finally{EditorSceneManager.CloseScene(scene,true);AssetDatabase.Refresh();}
        }
        private static void BuildLoading(Transform canvas,UIShowcase demo)
        {
            var root=Full("Loading",canvas);I("Dim",root,null,0,0,1920,1080,new Color(0,0,0,.6f));var icon=I("Gavel Icon",root,AssetDatabase.LoadAssetAtPath<Sprite>(UI+"Menu/Sprites/IC_Gavel.png"),0,120,130,130);icon.preserveAspect=true;
            var v=root.gameObject.AddComponent<LoadingView>();v.Title=T("Title",root,"경매장으로 이동 중",0,-8,1000,70,44,Gold);v.Detail=T("Detail",root,"화면을 준비하고 있습니다.",0,-75,1100,60,27);
            I("Track",root,null,0,-160,670,12,new Color(0,0,0,.65f));v.ProgressFill=I("Progress",root,null,0,-160,660,8,Gold);v.ProgressFill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");v.ProgressFill.type=Image.Type.Filled;v.ProgressFill.fillMethod=Image.FillMethod.Horizontal;v.ProgressLabel=T("Progress Label",root,"불러오는 중…",0,-202,700,40,23,Muted);demo.Loading=v;Save(v,"Loading","PF_Loading");
        }
        private static void BuildResults(Transform canvas,UIShowcase demo)
        {
            var v=Modal<MatchResultView>(canvas,"Match Results",1480,850,1);demo.Results=v;v.EscapeEnabled=false;v.Winner=T("Winner",v.Title.transform.parent,"루나",0,215,1250,55,38,Gold);v.Body.rectTransform.anchoredPosition=new Vector2(0,270);v.Body.rectTransform.sizeDelta=new Vector2(1000,36);v.Body.fontSize=23;
            string[] headings={"순위","플레이어","총자산","현금","주식 평가","보유 주식"};float[] x={-610,-345,-40,195,405,595};float[] w={90,350,210,180,180,165};for(int j=0;j<6;j++)T("Header "+j,v.Title.transform.parent,headings[j],x[j],160,w[j],44,25,Muted);
            v.Rows=new GameObject[6];v.RankLabels=new Text[6];v.NameLabels=new Text[6];v.TotalLabels=new Text[6];v.CashLabels=new Text[6];v.StockLabels=new Text[6];v.SharesLabels=new Text[6];v.RowBackgrounds=new Image[6];
            for(int n=0;n<6;n++){var row=R("Result Row "+n,v.Title.transform.parent,0,98-n*62,1320,57);v.Rows[n]=row.gameObject;v.RowBackgrounds[n]=I("Row Highlight",row,null,0,0,1320,57);var arr=new Text[6];for(int j=0;j<6;j++)arr[j]=T(headings[j],row,"—",x[j],0,w[j]-12,50,j==2?29:26,j==2?Gold:Cream);arr[1].resizeTextForBestFit=true;arr[1].resizeTextMinSize=20;arr[1].resizeTextMaxSize=26;v.RankLabels[n]=arr[0];v.NameLabels[n]=arr[1];v.TotalLabels[n]=arr[2];v.CashLabels[n]=arr[3];v.StockLabels[n]=arr[4];v.SharesLabels[n]=arr[5];}
            v.ShowResults(UIShowcase.Entries(3,false),true,true);Save(v,"Result","PF_MatchResults");
        }
        private static void BuildSettings(Transform canvas,UIShowcase demo)
        {
            var v=Modal<SettingsPanelView>(canvas,"Settings",1040,820,3);demo.Settings=v;v.ValueLabels=new Text[5];v.Fills=new Image[4];v.RowHighlights=new Image[5];v.Body.rectTransform.anchoredPosition=new Vector2(0,218);v.Body.fontSize=23;
            string[] labels={"전체 음량","배경음","효과음","마우스 감도","화면 모드"};
            for(int i=0;i<5;i++){float y=135-i*76;v.RowHighlights[i]=I("Selection Row "+i,v.Title.transform.parent,null,0,y,870,68,Color.clear);T(labels[i],v.Title.transform.parent,labels[i],-280,y,260,50,28,Cream,TextAnchor.MiddleLeft);if(i<4){I("Track "+i,v.Title.transform.parent,null,80,y,340,7,new Color(0,0,0,.6f));v.Fills[i]=I("Fill "+i,v.Title.transform.parent,null,80,y,340,7,Gold);v.Fills[i].sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");v.Fills[i].type=Image.Type.Filled;v.Fills[i].fillMethod=Image.FillMethod.Horizontal;}v.ValueLabels[i]=T("Value "+i,v.Title.transform.parent,"",i<4?340:140,y,i<4?100:400,50,26,Gold);}
            v.Footer.text="↑↓ 항목 · ←→ 조절 · Enter 선택 · Esc 닫기";v.ShowSettings(SettingsValues.Defaults);Save(v,"Menu/Settings","PF_Settings");
        }
        private static void Capture(UIShowcase demo,Canvas canvas,Camera cam)
        {
            string dir=Root+"/Previews";Directory.CreateDirectory(dir);var rt=new RenderTexture(1920,1080,24);var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;cam.targetTexture=rt;cam.aspect=16f/9;
            for(int i=0;i<demo.Count;i++){demo.ShowCase(i);Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(dir+"/"+(i+1).ToString("00")+".png",tex.EncodeToPNG());}}
            finally{cam.targetTexture=null;RenderTexture.active=previous;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
        }
    }
}
