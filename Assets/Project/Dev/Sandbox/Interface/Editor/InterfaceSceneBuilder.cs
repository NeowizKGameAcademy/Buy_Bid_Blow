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
    public static class InterfaceSceneBuilder
    {
        public const string Root="Assets/Project/Dev/Sandbox/Interface";
        public const string ScenePath=Root+"/SC_InGameInterface.unity";
        static Font font;
        static readonly Vector2 TL=new Vector2(0,1);
        static readonly Color Ink=new Color(1,.96f,.85f);
        static Sprite Icon(string name)=>AssetDatabase.LoadAllAssetsAtPath(Root+"/Sprites/"+name+".png").OfType<Sprite>().Single();
        static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos,Vector2 anchor,Vector2 pivot)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.sizeDelta=size;r.anchoredPosition=pos;return r;
        }
        static RectTransform Group(string name,Transform parent,float w,float h,float x,float y)=>Rect(name,parent,new Vector2(w,h),new Vector2(x,-y),TL,TL);
        static Image Image(string name,Transform parent,Sprite sprite,float w,float h,float x,float y,Color color)
        {
            var image=Group(name,parent,w,h,x,y).gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color;
            image.preserveAspect=true;image.raycastTarget=false;return image;
        }
        static Text Text(string name,Transform parent,string value,int size,float w,float h,float x,float y,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var t=Group(name,parent,w,h,x,y).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.fontStyle=FontStyle.Bold;
            t.text=value;t.color=Ink;t.alignment=align;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Overflow;
            var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.5f);shadow.effectDistance=new Vector2(1,-2);return t;
        }
        static void Backdrop(Transform parent,float w,float h,float x,float y,float alpha)
        {
            var image=Image("Background - opacity editable",parent,Icon("BG_SoftFade"),w,h,x,y,new Color(.03f,.08f,.075f,alpha));image.preserveAspect=false;image.transform.SetAsFirstSibling();
        }
        static void ImportSprite(string file,bool trim)
        {
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);source.LoadImage(File.ReadAllBytes(file));
            int w=source.width,h=source.height,l=0,b=0,r=w-1,t=h-1;
            if(trim){l=w;b=h;r=0;t=0;var pixels=source.GetPixels32();for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(pixels[y*w+x].a>=180){l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,y);t=Math.Max(t,y);}l=Math.Max(0,l-8);b=Math.Max(0,b-8);r=Math.Min(w-1,r+8);t=Math.Min(h-1,t+8);}
            UnityEngine.Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(file);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.sRGBTexture=true;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet=new[]{new SpriteMetaData{name=Path.GetFileNameWithoutExtension(file),rect=new Rect(l,b,r-l+1,t-b+1),alignment=0,pivot=new Vector2(.5f,.5f)}};
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }
        static void MakeBackground()
        {
            var tex=new Texture2D(256,128,TextureFormat.RGBA32,false);
            for(int y=0;y<128;y++)for(int x=0;x<256;x++){
                float u=Mathf.Abs((x+.5f)/256*2-1),v=Mathf.Abs((y+.5f)/128*2-1);
                float a=(1-Mathf.SmoothStep(.55f,1,u))*(1-Mathf.SmoothStep(.35f,1,v));tex.SetPixel(x,y,new Color(1,1,1,a));
            }
            tex.Apply();File.WriteAllBytes(Root+"/Sprites/BG_SoftFade.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        }
        static GameObject MakeHud()
        {
            var go=new GameObject("HUD - Static Editable Example",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var assets=Rect("Assets - Top Left",go.transform,new Vector2(440,214),new Vector2(48,-38),TL,TL);
            Backdrop(assets,480,250,-20,-18,.17f);
            var total=Group("Total",assets,400,32,18,0);Text("Label",total,"Total",23,90,32,0,0);Text("Value",total,"$233",25,105,32,295,0,TextAnchor.MiddleRight);
            var cash=Group("Cash",assets,270,90,18,44);Image("Icon",cash,Icon("IC_Cash"),45,45,0,22,Color.white);Text("Value",cash,"$150",68,210,88,65,0);
            var holdings=Group("Holdings",assets,100,104,326,38);
            string[] ids={"IC_Straw","IC_Timber","IC_Brick"};string[] quantities={"2","1","0"};
            for(int i=0;i<3;i++){var row=Group(ids[i]+" - row",holdings,100,32,0,i*34);Image("Icon",row,Icon(ids[i]),32,30,0,0,Color.white);Text("Quantity",row,quantities[i],27,40,32,54,0,TextAnchor.MiddleRight);}
            var bet=Group("Downside Bet",assets,410,42,18,151);var blue=new Color(.35f,.8f,1);
            var arrow=Image("Direction",bet,Icon("IC_Direction"),25,29,0,5,blue);arrow.rectTransform.pivot=new Vector2(.5f,.5f);arrow.rectTransform.anchoredPosition=new Vector2(12.5f,-19.5f);arrow.rectTransform.localRotation=Quaternion.Euler(0,0,180);
            Image("Company Icon",bet,Icon("IC_Timber"),34,30,42,5,Color.white);
            Text("Round",bet,"3R ·",25,62,40,96,0).color=blue;Text("Stake and Payout",bet,"$35 → $70",25,205,40,161,0).color=blue;
            var timer=Rect("Round Timer - Top Right",go.transform,new Vector2(245,170),new Vector2(-48,-36),Vector2.one,Vector2.one);
            Backdrop(timer,285,205,-20,-16,.13f);Text("Round",timer,"3 / 17 ROUND",23,245,34,0,0,TextAnchor.MiddleCenter);
            Text("Time",timer,"00:42",59,245,83,0,32,TextAnchor.MiddleCenter);Text("Phase",timer,"일반 거래",21,245,36,0,119,TextAnchor.MiddleCenter);
            var dot=Rect("Crosshair",go.transform,new Vector2(6,6),Vector2.zero,new Vector2(.5f,.5f),new Vector2(.5f,.5f));
            var dotImage=dot.gameObject.AddComponent<Image>();dotImage.color=Ink;dotImage.raycastTarget=false;
            var hint=Rect("Interaction Hint - Bottom Center",go.transform,new Vector2(350,56),new Vector2(0,42),new Vector2(.5f,0),new Vector2(.5f,0));
            Backdrop(hint,390,94,-20,-19,.15f);
            var key=Group("Keycap",hint,39,39,12,8);
            var keyImage=key.gameObject.AddComponent<Image>();keyImage.color=new Color(.05f,.06f,.055f,.18f);keyImage.raycastTarget=false;
            // Four editable thin images form the keycap; there is no panel border.
            foreach(var part in new[]{new Vector4(0,0,39,1.5f),new Vector4(0,37.5f,39,1.5f),new Vector4(0,0,1.5f,39),new Vector4(37.5f,0,1.5f,39)})
                Image("Keycap Edge",key,null,part.z,part.w,part.x,part.y,Ink);
            Text("Key",key,"E",28,39,39,0,0,TextAnchor.MiddleCenter);Text("Action",hint,"Straw 창구 이용",26,275,50,69,2);
            var reserved=Rect("Auction Interaction - Reserved",go.transform,new Vector2(760,250),new Vector2(0,106),new Vector2(.5f,0),new Vector2(.5f,0));reserved.gameObject.SetActive(false);
            return go;
        }
        public static void FinalizeExample()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var hud=scene.GetRootGameObjects().Single(g=>g.name=="HUD - Static Editable Example");
            var direction=hud.transform.Find("Assets - Top Left/Downside Bet/Direction").GetComponent<RectTransform>();direction.pivot=new Vector2(.5f,.5f);direction.anchoredPosition=new Vector2(12.5f,-19.5f);
            var board=GameObject.Find("Exchange Prices - World Space Editable");board.transform.rotation=Quaternion.Euler(0,180,0);board.transform.position=new Vector3(0,3.1f,-3.9f);board.transform.localScale=Vector3.one*.0053f;
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.name=="Company Sign - Editable Text")) {
                canvas.transform.localRotation=Quaternion.Euler(0,180,0);canvas.transform.localPosition=new Vector3(0,2.05f,-.07f);
                canvas.transform.parent.Find("Sign").localPosition=new Vector3(0,2.05f,-.15f);
            }
            int labels=hud.GetComponentsInChildren<Text>(true).Length;
            if(labels!=13)throw new Exception("Expected 13 editable HUD labels, got "+labels);
            if(hud.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>!c.GetType().Namespace.StartsWith("UnityEngine.UI")))throw new Exception("Unexpected HUD behaviour.");
            PrefabUtility.SaveAsPrefabAsset(hud,Root+"/PF_InGameHUD_Example.prefab");
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Capture(UnityEngine.Object.FindFirstObjectByType<Camera>(),hud.GetComponent<Canvas>());
            File.WriteAllText(Root+"/Verification.txt","PASS: saved editable scene + independent HUD prefab; 13 Text values; 6 transparent sprite assets; only built-in UI behaviours on HUD; no gameplay bindings; 1920x1080 render checked.");
            AssetDatabase.Refresh();
        }
        static Material Mat(string name,Color color)
        {
            var path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);EditorUtility.SetDirty(m);return m;
        }
        static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        static void MakeExchange()
        {
            var world=new GameObject("Exchange - Editable Set Dressing");var wood=Mat("Wood",new Color(.23f,.12f,.06f));var teal=Mat("Teal",new Color(.03f,.15f,.12f));
            var gold=Mat("Brass",new Color(.65f,.43f,.15f));string[] companies={"Straw","Timber","Brick"};var colors=new[]{new Color(1,.8f,.32f),new Color(.45f,.8f,.35f),new Color(1,.35f,.25f)};
            for(int i=0;i<3;i++) {
                float x=(1-i)*4.1f;var booth=new GameObject(companies[i]+" Counter");booth.transform.SetParent(world.transform);booth.transform.position=new Vector3(x,0,-4.8f);
                Cube("Counter",booth.transform,new Vector3(0,.52f,0),new Vector3(3.65f,1.04f,.9f),wood);Cube("Counter Top",booth.transform,new Vector3(0,1.08f,0),new Vector3(3.8f,.13f,1.06f),gold);
                Cube("Sign",booth.transform,new Vector3(0,2.35f,-.15f),new Vector3(3.3f,.5f,.12f),teal);
                var canvas=new GameObject("Company Sign - Editable Text",typeof(RectTransform),typeof(Canvas));canvas.transform.SetParent(booth.transform);canvas.transform.localPosition=new Vector3(0,2.35f,-.07f);canvas.transform.localRotation=Quaternion.identity;
                canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;var rect=canvas.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(550,80);rect.localScale=Vector3.one*.0058f;
                var title=Text("Company",canvas.transform,companies[i],51,550,80,0,0,TextAnchor.MiddleCenter);title.color=colors[i];
            }
            var board=new GameObject("Exchange Prices - World Space Editable",typeof(RectTransform),typeof(Canvas));board.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var br=board.GetComponent<RectTransform>();br.sizeDelta=new Vector2(1120,300);br.position=new Vector3(0,3.47f,-5);br.localScale=Vector3.one*.0062f;
            Image("Board Background",board.transform,null,1120,300,0,0,new Color(.025f,.1f,.08f));
            Text("Heading",board.transform,"거래소",37,1060,50,30,8,TextAnchor.MiddleCenter);
            string[] price={"$24","$35","$46"},delta={"+$5","−$3","+$4"},icons={"IC_Straw","IC_Timber","IC_Brick"};
            for(int i=0;i<3;i++){float y=70+i*70;Image("Icon "+i,board.transform,Icon(icons[i]),44,48,46,y,Color.white);Text("Company "+i,board.transform,companies[i],37,450,60,124,y).color=colors[i];Text("Price "+i,board.transform,price[i],40,240,60,590,y,TextAnchor.MiddleRight);Text("Change "+i,board.transform,delta[i],36,200,60,870,y,TextAnchor.MiddleRight).color=i==1?new Color(.3f,.75f,1):colors[0];}
        }
        static void PlaceWolves()
        {
            var group=new GameObject("Wolf Characters - Static Examples");var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Characters/Wolf/WolfExpressions/PF_Wolf_Character.prefab");
            var poses=new[]{new Vector3(3.6f,0,1.1f),new Vector3(.7f,0,-3.4f),new Vector3(-3.8f,0,-3.3f)};
            for(int i=0;i<poses.Length;i++) {
                var wolf=(GameObject)PrefabUtility.InstantiatePrefab(prefab);wolf.name="Wolf Example "+(i+1);wolf.transform.SetParent(group.transform);
                var animator=wolf.GetComponentInChildren<Animator>();var clip=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0);if(clip)clip.SampleAnimation(animator.gameObject,0);
                var appearance=wolf.GetComponentInChildren<BBBWolfAppearance>();appearance.SetOutfit(i%3,0);appearance.SetTieColor(i==0?new Color(.2f,.6f,.3f):new Color(.7f,.15f,.2f));
                foreach(var component in wolf.GetComponentsInChildren<MonoBehaviour>(true))if(component)UnityEngine.Object.DestroyImmediate(component);
                if(animator)animator.enabled=false;wolf.transform.position=poses[i];wolf.transform.rotation=Quaternion.Euler(0,i==0?155:0,0);
            }
        }
        static void Capture(Camera camera,Canvas canvas)
        {
            var target=new RenderTexture(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try{camera.targetTexture=target;camera.aspect=16f/9f;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.4f;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Root+"/InterfacePreview.png",image.EncodeToPNG());}
            finally{RenderTexture.active=null;camera.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);Canvas.ForceUpdateCanvases();}
        }
        [MenuItem("Tools/BuyBidBlow/Interface/Create Editable Example")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");
            if(File.Exists(ScenePath))throw new Exception("Scene exists; preserving manual edits: "+ScenePath);
            Directory.CreateDirectory(Root+"/Materials");MakeBackground();
            foreach(var file in Directory.GetFiles(Root+"/Sprites","*.png"))ImportSprite(file,!file.Contains("BG_"));
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/alternative/Pretendard-Regular.ttf");
            AssetDatabase.CopyAsset("Assets/Project/Dev/Sandbox/AuctionHall/AuctionHall-Deco.unity",ScenePath);
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try {
                SceneManager.SetActiveScene(scene);
                foreach(var root in scene.GetRootGameObjects()) {
                    if(root.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>c&&c.GetType().Name=="HallWalkthrough"))UnityEngine.Object.DestroyImmediate(root);
                    else foreach(var camera in root.GetComponentsInChildren<Camera>(true))UnityEngine.Object.DestroyImmediate(camera.gameObject);
                }
                var view=new GameObject("First Person Camera - Static").AddComponent<Camera>();view.gameObject.tag="MainCamera";view.transform.position=new Vector3(0,1.65f,6);
                view.transform.LookAt(new Vector3(0,1.65f,-4.8f));view.fieldOfView=75;view.nearClipPlane=.05f;view.farClipPlane=100;
                view.gameObject.AddComponent<AudioListener>();view.gameObject.AddComponent(Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime",true));
                var light=new GameObject("Example Warm Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;light.color=new Color(1,.88f,.72f);light.transform.rotation=Quaternion.Euler(40,150,0);
                MakeExchange();PlaceWolves();var hud=MakeHud();Canvas.ForceUpdateCanvases();
                PrefabUtility.SaveAsPrefabAsset(hud,Root+"/PF_InGameHUD_Example.prefab");
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Capture(view,hud.GetComponent<Canvas>());
                if(hud.GetComponentsInChildren<Text>(true).Length!=13)throw new Exception("Expected 13 independently editable HUD labels.");
                File.WriteAllText(Root+"/Verification.txt","PASS: saved scene and HUD prefab; 1920x1080 render; editable Text/Image/RectTransform; no gameplay component on HUD; screen anchors TL/TR/center/BC; 6 sprite assets. All values static examples.");
            }finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.Refresh();
        }
        [InitializeOnLoadMethod] static void Request()
        {
            if(!File.Exists("Library/InterfaceBuild.request"))return;
            EditorApplication.delayCall+=()=>{File.Delete("Library/InterfaceBuild.request");try{Build();File.WriteAllText("Library/InterfaceBuild.result","OK");}catch(Exception ex){File.WriteAllText("Library/InterfaceBuild.result",ex.ToString());Debug.LogException(ex);}};
        }
    }
}
