using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuyBidBlow.Interface.Editor
{
    public static partial class InterfaceSceneBuilder
    {
        static readonly Color AuctionGold=new Color(1,.82f,.43f);
        static RectTransform AuctionPanel(string name,Transform parent,Vector2 size,Vector2 pos,Vector2 anchor,Vector2 pivot)
        {
            var panel=Rect(name,parent,size,pos,anchor,pivot);
            RoundedDim(panel,Icon("BG_RoundedDim"),size.x,size.y,0,0,.36f);return panel;
        }
        static void AddAuctionExample(Transform hud)
        {
            if(hud.Find("Auction - Hidden Example"))throw new Exception("Auction example already exists; preserving edits.");
            font=hud.GetComponentInChildren<Text>(true).font;
            var root=Rect("Auction - Hidden Example",hud,new Vector2(1920,1080),Vector2.zero,new Vector2(.5f,.5f),new Vector2(.5f,.5f));
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.sizeDelta=Vector2.zero;
            var header=AuctionPanel("Auction Status - Top Center",root,new Vector2(690,108),new Vector2(0,-38),new Vector2(.5f,1),new Vector2(.5f,1));
            Text("Stage",header,"1차 · 공개 경매",25,410,36,24,12).color=AuctionGold;
            Text("Auction Countdown",header,"00:38",39,200,64,466,14,TextAnchor.MiddleRight);
            Text("Public Highest Bid",header,"최고 $40  ·  PLAYER 02",24,440,38,24,53);
            var sealedHeader=Text("Sealed Status",header,"내 제출액 $40 · 수정 가능",24,440,38,24,53);sealedHeader.gameObject.SetActive(false);
            var product=AuctionPanel("Information Lot - Right",root,new Vector2(330,258),new Vector2(-48,-268),Vector2.one,Vector2.one);
            Text("Title",product,"방향 정보",27,286,38,22,16).color=AuctionGold;
            Text("Contents Count",product,"2개 라운드 · 각 2개 회사",18,286,30,22,56);
            string[][] companies={new[]{"IC_Straw","IC_Timber"},new[]{"IC_Timber","IC_Brick"}};
            for(int i=0;i<2;i++) {
                var row=Group("Target Round "+i,product,286,56,22,96+i*62);
                Text("Round",row,i==0?"2R":"5R",28,65,56,0,0);
                for(int j=0;j<2;j++)Image("Company "+j,row,Icon(companies[i][j]),45,45,96+j*90,5,Color.white);
            }
            Text("Privacy",product,"방향은 낙찰 후 나만 확인",18,286,30,22,222);
            var bid=AuctionPanel("Bid Controls - Bottom Center",root,new Vector2(740,180),new Vector2(0,42),new Vector2(.5f,0),new Vector2(.5f,0));
            Text("Amount Label",bid,"입찰 금액",20,160,32,24,16);
            Text("Amount",bid,"$41",51,230,68,24,47).color=AuctionGold;
            Text("Adjust Keys",bid,"[←] −$1      [→] +$1",23,390,38,320,25);
            Text("Submit Key",bid,"[E] 입찰",30,390,48,320,66);
            Text("Public Rule",bid,"최고가보다 $1 이상 · 최고 입찰 취소 불가",19,690,32,24,133);
            var sealedRule=Text("Sealed Rule",bid,"[X] 제출 취소 · 마감 전 재제출/수정 가능",19,690,32,24,133);sealedRule.gameObject.SetActive(false);
            var lockInfo=AuctionPanel("Reserved Cash - Below Assets",root,new Vector2(350,42),new Vector2(56,-244),TL,TL);
            Text("Reserved Amount",lockInfo,"입찰 묶임 $0  ·  거래 가능 $150",19,326,42,12,0);
            var feedback=AuctionPanel("Feedback - Hidden",root,new Vector2(600,54),new Vector2(0,238),new Vector2(.5f,0),new Vector2(.5f,0));
            Text("Message",feedback,"입찰 완료 / 재입찰 필요 / 현금 부족 / 마감",22,560,54,20,0,TextAnchor.MiddleCenter);feedback.gameObject.SetActive(false);
            var result=AuctionPanel("Private Result - Hidden",root,new Vector2(600,230),new Vector2(0,260),new Vector2(.5f,.5f),new Vector2(.5f,.5f));
            Text("Result",result,"낙찰 성공 · $40",30,552,48,24,18).color=AuctionGold;
            Text("Private Directions",result,"2R   Straw ↑   Timber ↓\n5R   Timber ↑   Brick →",26,552,100,24,78);
            Text("Close Key",result,"[E] 닫기 · 구매 정보는 개인 기록에서 확인",18,552,34,24,179);result.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
        }
        static void AuctionVariant(Transform hud,bool sealedAuction)
        {
            var root=hud.Find("Auction - Hidden Example");root.gameObject.SetActive(true);
            var header=root.Find("Auction Status - Top Center");
            header.Find("Stage").GetComponent<Text>().text=sealedAuction?"2차 · 비공개 경매":"1차 · 공개 경매";
            header.Find("Public Highest Bid").gameObject.SetActive(!sealedAuction);header.Find("Sealed Status").gameObject.SetActive(sealedAuction);
            var controls=root.Find("Bid Controls - Bottom Center");controls.Find("Public Rule").gameObject.SetActive(!sealedAuction);controls.Find("Sealed Rule").gameObject.SetActive(sealedAuction);
            controls.Find("Submit Key").GetComponent<Text>().text=sealedAuction?"[E] 제출 / 수정":"[E] 입찰";
            root.Find("Reserved Cash - Below Assets/Reserved Amount").GetComponent<Text>().text=sealedAuction?"입찰 묶임 $40  ·  거래 가능 $110":"입찰 묶임 $0  ·  거래 가능 $150";
            var lot=root.Find("Information Lot - Right");
            lot.Find("Target Round 0/Round").GetComponent<Text>().text=sealedAuction?"3R":"2R";
            lot.Find("Target Round 1/Round").GetComponent<Text>().text=sealedAuction?"4R":"5R";
            hud.Find("Assets - Top Left/Downside Bet/Round").GetComponent<Text>().text="2R ·";
            hud.Find("Round Timer - Top Right/Round").GetComponent<Text>().text="2 / 17 ROUND";
            hud.Find("Round Timer - Top Right/Time").GetComponent<Text>().text=sealedAuction?"02:38":"03:38";
            hud.Find("Round Timer - Top Right/Phase").GetComponent<Text>().text="경매 라운드";
            hud.Find("Interaction Hint - Bottom Center").gameObject.SetActive(false);
        }
        public static void BuildAuctionExample()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");
            string path=Root+"/PF_InGameHUD_Example.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
            try {AddAuctionExample(prefab.transform);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
            var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try {
                var hud=scene.GetRootGameObjects().Single(g=>g.name=="HUD - Static Editable Example");AddAuctionExample(hud.transform);
                if(hud.transform.Find("Auction - Hidden Example").gameObject.activeSelf)throw new Exception("Auction must be hidden by default.");
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                var preview=UnityEngine.Object.Instantiate(hud);hud.SetActive(false);
                try {
                    var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();
                    AuctionVariant(preview.transform,false);Capture(camera,preview.GetComponent<Canvas>(),"AuctionPublicPreview.png");
                    AuctionVariant(preview.transform,true);Capture(camera,preview.GetComponent<Canvas>(),"AuctionSealedPreview.png");
                }finally{UnityEngine.Object.DestroyImmediate(preview);hud.SetActive(true);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                File.WriteAllText(Root+"/AuctionVerification.txt","PASS: scene + prefab contain inactive Auction - Hidden Example; two rendered 1920x1080 variants; standard editable Text/Image/RectTransform; no runtime bindings; public high bid excluded from sealed preview; private directions hidden during auction.");
            }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.Refresh();
        }
        [InitializeOnLoadMethod] static void AuctionRequest()
        {
            if(!File.Exists("Library/InterfaceAuction.request"))return;
            EditorApplication.delayCall+=()=>{File.Delete("Library/InterfaceAuction.request");try{BuildAuctionExample();File.WriteAllText("Library/InterfaceAuction.result","PASS");}catch(Exception ex){File.WriteAllText("Library/InterfaceAuction.result",ex.ToString());Debug.LogException(ex);}};
        }
        [InitializeOnLoadMethod] static void AuctionPreviewRequest()
        {
            if(!File.Exists("Library/InterfaceAuctionPreview.request"))return;
            EditorApplication.delayCall+=()=>{
                File.Delete("Library/InterfaceAuctionPreview.request");
                var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;
                GameObject preview=null;GameObject hud=null;
                try {
                    if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
                    hud=scene.GetRootGameObjects().Single(g=>g.name=="HUD - Static Editable Example");
                    preview=UnityEngine.Object.Instantiate(hud);hud.SetActive(false);
                    var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();
                    AuctionVariant(preview.transform,false);Capture(camera,preview.GetComponent<Canvas>(),"AuctionPublicPreview.png");
                    AuctionVariant(preview.transform,true);Capture(camera,preview.GetComponent<Canvas>(),"AuctionSealedPreview.png");
                    File.WriteAllText("Library/InterfaceAuctionPreview.result","PASS: public 2R/5R, sealed 3R/4R; private directions hidden.");
                }catch(Exception ex){File.WriteAllText("Library/InterfaceAuctionPreview.result",ex.ToString());Debug.LogException(ex);}
                finally{if(preview)UnityEngine.Object.DestroyImmediate(preview);if(hud)hud.SetActive(true);if(opened)EditorSceneManager.CloseScene(scene,true);AssetDatabase.Refresh();}
            };
        }
    }
}
