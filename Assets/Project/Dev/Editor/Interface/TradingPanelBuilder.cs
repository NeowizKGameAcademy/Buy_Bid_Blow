using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuyBidBlow.Interface.Editor
{
    public static partial class InterfaceSceneBuilder
    {
        const string TradingRoot=Root+"/Trading";
        static Sprite TradingSprite(string id)=>AssetDatabase.LoadAllAssetsAtPath(TradingRoot+"/Sprites/"+id+".png").OfType<Sprite>().Single();
        static void ImportTradingSprite(string path,StringBuilder report)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));
            int width=texture.width,height=texture.height,left=width,right=0,bottom=height,top=0,transparent=0;
            var pixels=texture.GetPixels32();
            for(int y=0;y<height;y++)for(int x=0;x<width;x++) {
                var a=pixels[y*width+x].a;if(a<16)transparent++;
                if(a>24){left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
            }
            if(transparent<width*height/100)throw new Exception("Sprite has no transparent exterior: "+path);
            if(right<=left||top<=bottom)throw new Exception("Empty sprite: "+path);
            UnityEngine.Object.DestroyImmediate(texture);
            left=Math.Max(0,left-3);bottom=Math.Max(0,bottom-3);right=Math.Min(width-1,right+3);top=Math.Min(height-1,top+3);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.alphaIsTransparency=true;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet=new[]{new SpriteMetaData{name=Path.GetFileNameWithoutExtension(path),rect=new Rect(left,bottom,right-left+1,top-bottom+1),pivot=new Vector2(.5f,.5f),alignment=0}};
#pragma warning restore CS0618
            importer.SaveAndReimport();report.AppendLine($"{Path.GetFileName(path)}: {width}x{height}, transparent exterior {(100f*transparent/pixels.Length):F1}%, trim {right-left+1}x{top-bottom+1}.");
        }
        static Image TradeImage(string name,Transform parent,Sprite sprite,float w,float h,float x,float y)
        {
            var image=Image(name,parent,sprite,w,h,x,y,Color.white);image.preserveAspect=false;return image;
        }
        static void TradeInset(Transform parent,float w,float h)
        {
            var edge=TradeImage("Inset Edge",parent,Icon("BG_RoundedDim"),w,h,0,0);edge.type=UnityEngine.UI.Image.Type.Sliced;edge.color=new Color(.58f,.64f,.57f,.35f);
            var fill=TradeImage("Inset Fill",parent,Icon("BG_RoundedDim"),w-3,h-3,1.5f,1.5f);fill.type=UnityEngine.UI.Image.Type.Sliced;fill.color=new Color(.015f,.035f,.03f,.66f);
        }
        static Button TradeButton(string name,Transform parent,Sprite sprite,float w,float h,float x,float y,string label,int size,bool primary=false)
        {
            var image=TradeImage(name,parent,sprite,w,h,x,y);image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,1,.82f);colors.selectedColor=new Color(1,1,.78f);colors.pressedColor=new Color(.72f,.64f,.5f);colors.disabledColor=new Color(.4f,.4f,.4f,.55f);colors.fadeDuration=.12f;button.colors=colors;
            var text=Text("Label",image.transform,label,size,w,h,0,0,TextAnchor.MiddleCenter);text.color=primary?new Color(.12f,.065f,.025f):new Color(.18f,.10f,.045f);
            return button;
        }
        static GameObject MakeTradingPanel()
        {
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/alternative/Pretendard-Regular.ttf");
            var panel=Rect("Trading Panel - Brick",null,new Vector2(860,850),new Vector2(-64,-154),Vector2.one,Vector2.one);
            var bg=TradeImage("Panel Background",panel,TradingSprite("BG_TradingGlass"),860,820,0,30);bg.color=new Color(1,1,1,.88f);bg.raycastTarget=true;
            var title=Group("Company Header",panel,860,180,0,0);
            TradeImage("Walnut Plaque",title,TradingSprite("BG_CompanyPlaque"),472,133,80,20);
            var medallion=TradeImage("Brass Medallion",title,TradingSprite("BG_CompanyMedallion"),188,188,18,-5);medallion.preserveAspect=true;
            var companyIcon=TradeImage("Company Icon",title,Icon("IC_Brick"),112,112,56,33);companyIcon.preserveAspect=true;
            Text("Company Name",title,"Brick",58,345,113,195,29);
            Text("Price Label",title,"현재가",20,235,28,595,18,TextAnchor.MiddleCenter).color=new Color(.75f,.82f,.78f);
            Text("Current Price",title,"$46",76,235,92,595,45,TextAnchor.MiddleCenter);
            Text("Last Change",title,"+$4",36,235,43,595,135,TextAnchor.MiddleCenter).color=AuctionGold;
            var tabs=Group("Trade Tabs",panel,790,62,35,210);
            string[] names={"Buy","Sell","Downside Bet"},labels={"매수","매도","하락 베팅"};
            var buttons=new System.Collections.Generic.List<Button>();
            for(int i=0;i<3;i++) {
                var tab=Group(names[i],tabs,250,58,i*265,0);var target=tab.gameObject.AddComponent<Image>();target.sprite=Icon("BG_RoundedDim");target.type=UnityEngine.UI.Image.Type.Sliced;target.color=new Color(1,1,1,.015f);
                var b=tab.gameObject.AddComponent<Button>();b.targetGraphic=target;var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1,.8f,.4f,1);c.selectedColor=c.highlightedColor;c.pressedColor=new Color(.6f,.5f,.3f);c.disabledColor=Color.gray;b.colors=c;buttons.Add(b);
                Text("Label",tab,labels[i],27,250,52,0,0,TextAnchor.MiddleCenter).color=i==0?AuctionGold:new Color(.65f,.75f,.76f);
            }
            for(int i=1;i<3;i++)Image("Tab Divider",tabs,null,1.2f,27,i*265-8,12,new Color(.7f,.78f,.75f,.5f));
            var selection=Group("Selected Tab Indicator",tabs,205,12,22,51);
            Image("Underline",selection,null,205,2,0,5,AuctionGold);
            var diamond=Image("Diamond",selection,null,11,11,97,0,AuctionGold);diamond.rectTransform.localRotation=Quaternion.Euler(0,0,45);
            var quantity=Group("Quantity Control",panel,790,174,35,292);TradeInset(quantity,790,174);
            Text("Label",quantity,"매수 수량",24,360,37,215,15,TextAnchor.MiddleCenter).color=new Color(.78f,.84f,.8f);
            buttons.Add(TradeButton("Decrease",quantity,TradingSprite("BTN_QuantityKey"),106,98,69,58,"←",37));
            Text("Quantity",quantity,"2",81,110,100,317,55,TextAnchor.MiddleCenter);Text("Unit",quantity,"주",31,55,70,428,80);
            buttons.Add(TradeButton("Increase",quantity,TradingSprite("BTN_QuantityKey"),106,98,615,58,"→",37));
            var stats=Group("Stock Availability",panel,790,48,35,485);
            Text("Market Stock Label",stats,"시장 재고",20,103,44,0,0);Text("Market Stock",stats,"8주",28,60,44,105,0);
            Text("Dot 1",stats,"·",30,30,44,188,0);
            Text("Owned Label",stats,"보유",20,63,44,268,0);Text("Owned",stats,"0주",28,60,44,333,0);
            Text("Dot 2",stats,"·",30,30,44,424,0);
            Text("Maximum Label",stats,"구매 가능",20,103,44,555,0);Text("Maximum",stats,"3주",28,60,44,666,0);
            var payment=Group("Payment Summary",panel,830,221,15,557);TradeInset(payment,830,221);
            Text("Total Label",payment,"합계",23,190,36,33,26);Text("Total Value",payment,"$92",76,235,106,33,60).color=AuctionGold;
            var confirm=TradeButton("Confirm Purchase",payment,TradingSprite("BTN_ConfirmBrass"),445,111,347,30,"",38,true);buttons.Add(confirm);
            var key=TradeImage("Keycap",confirm.transform,Icon("BG_RoundedDim"),51,53,62,29);key.color=new Color(.12f,.07f,.035f,.94f);key.type=UnityEngine.UI.Image.Type.Sliced;
            Text("Key",key.transform,"E",31,51,53,0,0,TextAnchor.MiddleCenter);
            Text("Action",confirm.transform,"매수 확정",38,287,111,129,0,TextAnchor.MiddleCenter).color=new Color(.1f,.055f,.02f);
            Text("Cash After Label",payment,"거래 후 현금",23,190,43,300,163);Text("Cash After",payment,"$58",35,145,43,497,163);
            var close=Group("Close Hint",panel,260,50,300,796);
            var closeKey=TradeImage("Keycap",close,Icon("BG_RoundedDim"),75,43,0,0);closeKey.type=UnityEngine.UI.Image.Type.Sliced;closeKey.color=new Color(.08f,.075f,.055f,.9f);
            Text("Key",closeKey.transform,"Q",23,75,43,0,0,TextAnchor.MiddleCenter);Text("Label",close,"닫기",23,110,43,92,0).color=new Color(.7f,.8f,.8f);
            var closeButton=closeKey.gameObject.AddComponent<Button>();closeButton.targetGraphic=closeKey;closeKey.raycastTarget=true;buttons.Add(closeButton);
            for(int i=0;i<buttons.Count;i++) {
                var nav=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=buttons[(i+buttons.Count-1)%buttons.Count],selectOnRight=buttons[(i+1)%buttons.Count],selectOnUp=buttons[(i+buttons.Count-1)%buttons.Count],selectOnDown=buttons[(i+1)%buttons.Count]};buttons[i].navigation=nav;
            }
            return panel.gameObject;
        }
        static void TradingCompany(Transform panel,string company)
        {
            panel.name="Trading Panel - "+company;
            panel.Find("Company Header/Company Name").GetComponent<Text>().text=company;
            panel.Find("Company Header/Company Icon").GetComponent<Image>().sprite=Icon("IC_"+company);
            int price=company=="Straw"?24:company=="Timber"?35:46;
            panel.Find("Company Header/Current Price").GetComponent<Text>().text="$"+price;
            var change=panel.Find("Company Header/Last Change").GetComponent<Text>();change.text=company=="Straw"?"+$5":company=="Timber"?"−$3":"+$4";change.color=company=="Timber"?new Color(.3f,.75f,1):AuctionGold;
            panel.Find("Stock Availability/Owned").GetComponent<Text>().text=company=="Straw"?"2주":company=="Timber"?"1주":"0주";
            panel.Find("Stock Availability/Maximum").GetComponent<Text>().text=(150/price)+"주";
            panel.Find("Payment Summary/Total Value").GetComponent<Text>().text="$"+(price*2);
            panel.Find("Payment Summary/Cash After").GetComponent<Text>().text="$"+(150-price*2);
        }
        static void CaptureTrading(Camera camera,Canvas canvas,string filename)
        {
            var hidden=new System.Collections.Generic.List<GameObject>();
            try {
                for(int i=0;i<SceneManager.sceneCount;i++) {
                    var other=SceneManager.GetSceneAt(i);if(other==camera.gameObject.scene)continue;
                    foreach(var root in other.GetRootGameObjects())if(root.activeSelf){hidden.Add(root);root.SetActive(false);}
                }
                Capture(camera,canvas,"Trading/"+filename);
            }finally{foreach(var root in hidden)if(root)root.SetActive(true);}
        }
        [MenuItem("Tools/BuyBidBlow/Interface/Build Trading Panel Example")]
        public static void BuildTradingExample()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");
            string prefabPath=TradingRoot+"/PF_TradingPanel.prefab",scenePath=TradingRoot+"/SC_TradingInterface.unity";
            if(File.Exists(prefabPath)||File.Exists(scenePath))throw new Exception("Preserving existing trading assets.");
            var report=new StringBuilder();
            foreach(string id in new[]{"BG_TradingGlass","BG_CompanyPlaque","BG_CompanyMedallion","BTN_QuantityKey","BTN_ConfirmBrass"})ImportTradingSprite(TradingRoot+"/Sprites/"+id+".png",report);
            var panel=MakeTradingPanel();GameObject basePrefab;
            try {basePrefab=PrefabUtility.SaveAsPrefabAsset(panel,prefabPath);}finally{UnityEngine.Object.DestroyImmediate(panel);}
            foreach(var company in new[]{"Straw","Timber","Brick"}) {
                var variant=(GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                try{TradingCompany(variant.transform,company);PrefabUtility.SaveAsPrefabAsset(variant,TradingRoot+"/PF_TradingPanel_"+company+".prefab");}finally{UnityEngine.Object.DestroyImmediate(variant);}
            }
            if(!AssetDatabase.CopyAsset(ScenePath,scenePath))throw new Exception("Could not copy interface scene.");
            var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            try {
                var roots=scene.GetRootGameObjects();var hud=roots.Single(g=>g.name=="HUD - Static Editable Example");
                hud.transform.Find("Interaction Hint - Bottom Center").gameObject.SetActive(false);hud.transform.Find("Crosshair").gameObject.SetActive(false);
                hud.transform.Find("Assets - Top Left/Downside Bet").gameObject.SetActive(false);
                foreach(var item in roots.Where(g=>g.name=="Wolf Characters - Static Examples"))item.SetActive(false);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TradingRoot+"/PF_TradingPanel_Brick.prefab"),scene);instance.transform.SetParent(hud.transform,false);
                var camera=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();camera.transform.position=new Vector3(0,1.65f,-.6f);camera.transform.LookAt(new Vector3(0,1.55f,-4.8f));camera.fieldOfView=67;
                var pig=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Content/Characters/Pig/PF_Pig_Red_Model.prefab"),scene);
                pig.name="Pig Exchange Owner - Static Example";pig.transform.position=new Vector3(1.9f,0,-4.9f);pig.transform.localScale=Vector3.one*3;
                var register=new GameObject("Trading Counter - Example");SceneManager.MoveGameObjectToScene(register,scene);
                var wood=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Wood.mat");var brass=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Brass.mat");
                Cube("Counter Front",register.transform,new Vector3(1.9f,.52f,-4.1f),new Vector3(4.2f,1.04f,.75f),wood);
                Cube("Counter Top",register.transform,new Vector3(1.9f,1.08f,-4.1f),new Vector3(4.3f,.12f,1),brass);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();CaptureTrading(camera,hud.GetComponent<Canvas>(),"TradingBrickPreview.png");
                // Render variants on a temporary detached copy to keep the saved Brick prefab link intact.
                var preview=UnityEngine.Object.Instantiate(instance,hud.transform);instance.SetActive(false);
                try {foreach(string company in new[]{"Straw","Timber"}){TradingCompany(preview.transform,company);CaptureTrading(camera,hud.GetComponent<Canvas>(),"Trading"+company+"Preview.png");}}
                finally{UnityEngine.Object.DestroyImmediate(preview);instance.SetActive(true);}
                EditorSceneManager.SaveScene(scene);
            }finally{EditorSceneManager.CloseScene(scene,true);}
            var saved=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);var text=saved.GetComponentsInChildren<Text>(true);var allButtons=saved.GetComponentsInChildren<Button>(true);
            if(text.Length<20||allButtons.Length!=7)throw new Exception("Unexpected editable component count.");
            if(allButtons.Any(b=>b.onClick.GetPersistentEventCount()!=0||b.navigation.mode!=Navigation.Mode.Explicit))throw new Exception("Unexpected button configuration.");
            if(saved.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>!c.GetType().Namespace.StartsWith("UnityEngine.UI")))throw new Exception("Unexpected runtime script.");
            report.AppendLine($"PASS: {text.Length} editable Text components, {allButtons.Length} Buttons with explicit navigation, ColorTint states, empty onClick. Base prefab plus 3 variants. Static example scene and 3 preview renders. No gameplay binding.");
            File.WriteAllText(TradingRoot+"/Verification.txt",report.ToString());File.WriteAllText("Library/TradingPanelBuild.result",report.ToString());AssetDatabase.Refresh();
        }
        [InitializeOnLoadMethod] static void TradingRequest()
        {
            if(!File.Exists("Library/TradingPanelBuild.request"))return;
            EditorApplication.delayCall+=()=>{File.Delete("Library/TradingPanelBuild.request");try{BuildTradingExample();}catch(Exception ex){File.WriteAllText("Library/TradingPanelBuild.result",ex.ToString());Debug.LogException(ex);}};
        }
        public static void RefineTradingExample()
        {
            string path=TradingRoot+"/PF_TradingPanel.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try {
                prefab.GetComponent<RectTransform>().anchoredPosition=new Vector2(-64,-190);
                foreach(string id in new[]{"Glass Dim - Opacity Editable","Price Header Dim"}) {var old=prefab.transform.Find(id);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);}
                var bg=prefab.transform.Find("Panel Background").GetComponent<Image>();bg.color=new Color(.65f,.78f,.72f,.96f);
                var under=TradeImage("Glass Dim - Opacity Editable",prefab.transform,Icon("BG_RoundedDim"),844,800,8,46);under.type=UnityEngine.UI.Image.Type.Sliced;under.color=new Color(.018f,.055f,.045f,.7f);under.transform.SetAsFirstSibling();
                var priceUnder=TradeImage("Price Header Dim",prefab.transform,Icon("BG_RoundedDim"),260,178,584,0);priceUnder.type=UnityEngine.UI.Image.Type.Sliced;priceUnder.color=new Color(.018f,.055f,.045f,.78f);priceUnder.transform.SetSiblingIndex(2);
                foreach(var group in new[]{"Quantity Control","Payment Summary"}) {
                    prefab.transform.Find(group+"/Inset Edge").GetComponent<Image>().color=new Color(.56f,.63f,.53f,.24f);
                    prefab.transform.Find(group+"/Inset Fill").GetComponent<Image>().color=new Color(.01f,.025f,.02f,.9f);
                }
                PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }finally{PrefabUtility.UnloadPrefabContents(prefab);}
            foreach(string company in new[]{"Straw","Timber","Brick"}) {
                string variantPath=TradingRoot+"/PF_TradingPanel_"+company+".prefab";var variant=PrefabUtility.LoadPrefabContents(variantPath);
                try{variant.GetComponent<RectTransform>().anchoredPosition=new Vector2(-64,-190);PrefabUtility.SaveAsPrefabAsset(variant,variantPath);}finally{PrefabUtility.UnloadPrefabContents(variant);}
            }
            var scene=EditorSceneManager.OpenScene(TradingRoot+"/SC_TradingInterface.unity",OpenSceneMode.Additive);
            try {
                var roots=scene.GetRootGameObjects();var hud=roots.Single(g=>g.name=="HUD - Static Editable Example");
                var pig=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Pig Exchange Owner - Static Example");
                foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<SkinnedMeshRenderer>(true)))if(!r.transform.IsChildOf(pig))r.enabled=false;
                foreach(var canvas in roots.SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)))if(canvas!=hud.GetComponent<Canvas>()&&!canvas.transform.IsChildOf(hud.transform))canvas.gameObject.SetActive(false);
                foreach(var t in roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))if(t.name=="Exchange - Editable Set Dressing")t.gameObject.SetActive(false);
                var instance=hud.transform.Cast<Transform>().Single(t=>t.Find("Company Header")!=null&&t.Find("Payment Summary")!=null).gameObject;
                instance.GetComponent<RectTransform>().anchoredPosition=new Vector2(-64,-190);
                var camera=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();camera.transform.position=new Vector3(0,1.65f,-.6f);camera.transform.LookAt(new Vector3(0,1.6f,-4.8f));
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();CaptureTrading(camera,hud.GetComponent<Canvas>(),"TradingBrickPreview.png");
                var preview=UnityEngine.Object.Instantiate(instance,hud.transform);instance.SetActive(false);
                try {foreach(string company in new[]{"Straw","Timber"}){TradingCompany(preview.transform,company);CaptureTrading(camera,hud.GetComponent<Canvas>(),"Trading"+company+"Preview.png");}}
                finally{UnityEngine.Object.DestroyImmediate(preview);instance.SetActive(true);}
                EditorSceneManager.SaveScene(scene);File.WriteAllText("Library/TradingPanelRefine.result","PASS: dim contrast, header spacing, scene clutter refined; all 3 previews rendered.");
            }finally{EditorSceneManager.CloseScene(scene,true);AssetDatabase.Refresh();}
        }
        [InitializeOnLoadMethod] static void TradingRefineRequest()
        {
            if(!File.Exists("Library/TradingPanelRefine.request"))return;
            EditorApplication.delayCall+=()=>{File.Delete("Library/TradingPanelRefine.request");try{RefineTradingExample();}catch(Exception ex){File.WriteAllText("Library/TradingPanelRefine.result",ex.ToString());Debug.LogException(ex);}};
        }
    }
}
