using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace BuyBidBlow.Interface.Editor {
 public static partial class InterfaceSceneBuilder {
 static Sprite AuctionShape(string id,int w,int h){
 string path=Root+"/Sprites/"+id+".png";var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);
 // Cut shoulders, shallow raised center, clipped lower corners; front-facing auction nameplate.
 Vector2[] points={new Vector2(22,h-12),new Vector2(w*.42f,h-12),new Vector2(w*.5f,h-3),new Vector2(w*.58f,h-12),new Vector2(w-22,h-12),new Vector2(w-2,h-32),new Vector2(w-2,24),new Vector2(w-24,2),new Vector2(24,2),new Vector2(2,24),new Vector2(2,h-32)};
 for(int y=0;y<h;y++)for(int x=0;x<w;x++){
 var p=new Vector2(x+.5f,y+.5f);bool inside=false;float dist=float.MaxValue;
 for(int i=0,j=points.Length-1;i<points.Length;j=i++){
 var a=points[j];var b=points[i];if(((a.y>p.y)!=(b.y>p.y))&&(p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x))inside=!inside;
 var ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);dist=Mathf.Min(dist,(p-(a+ab*t)).magnitude);
 }
 Color color=Color.clear;if(inside)color=dist<2?new Color(.52f,.39f,.20f,.95f):new Color(.016f,.067f,.058f,.84f);texture.SetPixel(x,y,color);
 }
 texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);ImportSprite(path,false);return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
 }
 static RectTransform NewAuctionPanel(string name,Transform parent,Sprite sprite,float w,float h,Vector2 pos,Vector2 anchor){var r=Rect(name,parent,new Vector2(w,h),pos,anchor,anchor);var bg=Image("Background - editable opacity",r,sprite,w,h,0,0,Color.white);bg.preserveAspect=false;return r;}
 static void AuctionKey(Transform parent,string key,float x,float y){var im=Image("Keycap "+key,parent,Icon("BG_RoundedDim"),50,42,x,y,new Color(.42f,.32f,.17f,.9f));im.type=UnityEngine.UI.Image.Type.Sliced;Text("Key",im.transform,key,25,50,42,0,0,TextAnchor.MiddleCenter);}
 [MenuItem("Tools/BuyBidBlow/Interface/Apply Auction First Person HUD")]
 public static void ApplyAuctionFirstPerson(){
 string path=Root+"/SC_AuctionSecondFloorInterface.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 if(scene.isDirty)EditorSceneManager.SaveScene(scene);var previous=SceneManager.GetActiveScene();SceneManager.SetActiveScene(scene);
 var hidden=new System.Collections.Generic.List<GameObject>();
 try{
 foreach(var png in Directory.GetFiles(Root+"/Sprites","*.png"))AssetDatabase.ImportAsset(png,ImportAssetOptions.ForceSynchronousImport);
 font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/alternative/Pretendard-Bold.ttf");
 var assetsShape=AuctionShape("BG_AuctionAssets",460,220);var headerShape=AuctionShape("BG_AuctionHeader",720,140);var timerShape=AuctionShape("BG_AuctionTimer",270,190);var bidShape=AuctionShape("BG_AuctionBid",760,230);
 var roots=scene.GetRootGameObjects();var old=roots.FirstOrDefault(g=>g.name=="HUD - Static Editable Example");if(old)old.SetActive(false);
 var existing=roots.FirstOrDefault(g=>g.name=="Auction HUD - Editable v2");if(existing)UnityEngine.Object.DestroyImmediate(existing);
 var hud=new GameObject("Auction HUD - Editable v2",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));hud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=hud.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
 var assets=NewAuctionPanel("Assets - Top Left",hud.transform,assetsShape,460,220,new Vector2(32,-24),TL);
 Text("Total Label",assets,"Total",24,120,38,28,18);Text("Total Value",assets,"$268",25,120,38,305,18,TextAnchor.MiddleRight);
 Image("Cash Icon",assets,Icon("IC_Cash"),55,58,28,76,Color.white);Text("Cash",assets,"$150",65,235,95,98,58);
 for(int i=0;i<3;i++){Image("Holding Icon "+i,assets,Icon(new[]{"IC_Straw","IC_Timber","IC_Brick"}[i]),32,31,350,65+i*33,Color.white);Text("Holding Quantity "+i,assets,new[]{"2","2","0"}[i],24,36,32,391,65+i*33,TextAnchor.MiddleRight);}
 Image("Divider",assets,null,399,1,29,167,new Color(.5f,.4f,.23f,.75f));Text("Reserved Cash",assets,"입찰 묶임 $35 · 거래 가능 $115",20,405,38,27,175);
 var header=NewAuctionPanel("Auction Status - Top Center",hud.transform,headerShape,720,140,new Vector2(0,-26),new Vector2(.5f,1));
 Text("Stage",header,"1차 공개 경매",27,420,45,30,18).color=AuctionGold;Text("Highest Bid",header,"최고 입찰 $35 · 나",23,330,40,30,70);Text("Leading Status",header,"현재 최고 입찰 중",18,220,40,327,70).color=AuctionGold;
 Image("Timer Divider",header,null,1,79,556,31,new Color(.5f,.4f,.23f,.75f));Text("Countdown Label",header,"경매 마감",18,137,29,569,23,TextAnchor.MiddleCenter).color=AuctionGold;Text("Countdown",header,"00:38",39,137,59,569,51,TextAnchor.MiddleCenter);
 var timer=NewAuctionPanel("Round Timer - Top Right",hud.transform,timerShape,270,190,new Vector2(-32,-24),Vector2.one);
 Text("Round",timer,"6 / 17 ROUND",24,240,40,15,20,TextAnchor.MiddleCenter);Text("Time",timer,"03:38",57,240,80,15,56,TextAnchor.MiddleCenter);Text("Phase",timer,"라운드 종료",20,240,36,15,137,TextAnchor.MiddleCenter);
 var bid=NewAuctionPanel("Bid Controls - Bottom Center",hud.transform,bidShape,760,230,new Vector2(0,32),new Vector2(.5f,0));
 Text("Amount Label",bid,"다음 입찰 금액",20,235,36,33,22);Text("Amount",bid,"$36",58,225,88,33,55).color=AuctionGold;
 AuctionKey(bid,"←",286,37);Text("Decrease",bid,"−$1",25,88,42,348,37);AuctionKey(bid,"→",484,37);Text("Increase",bid,"+$1",25,88,42,546,37);
 var submit=Image("Submit - Visual Only",bid,Icon("BG_RoundedDim"),322,57,286,94,new Color(.62f,.42f,.16f,.96f));submit.type=UnityEngine.UI.Image.Type.Sliced;AuctionKey(submit.transform,"E",77,7);Text("Submit Label",submit.transform,"입찰",26,110,57,142,0,TextAnchor.MiddleCenter);
 Image("Divider",bid,null,690,1,34,171,new Color(.5f,.4f,.23f,.75f));Text("Cash After",bid,"입찰 후 거래 가능 $114",20,350,38,33,179);Text("Rule",bid,"최고 입찰 취소 불가",20,300,38,430,179,TextAnchor.MiddleRight);
 var reserve=Rect("World Lot Label - Reserved For Later",hud.transform,new Vector2(320,200),new Vector2(0,220),new Vector2(.5f,.5f),new Vector2(.5f,.5f));reserve.gameObject.SetActive(false);
 var stage=roots.First(g=>g.name=="Auction Stage - Editable Example").transform;var pig=stage.Find("Pig Auctioneer - Static Model");pig.position=new Vector3(1.5f,4.15f,-13.2f);pig.localScale=Vector3.one*3;
 stage.Find("Auctioneer Podium").position=new Vector3(1.5f,4.55f,-12.5f);stage.Find("Podium Top").position=new Vector3(1.5f,5.14f,-12.5f);var sign=stage.Find("Auction Stage Sign");if(sign)sign.gameObject.SetActive(false);
 var oldComposition=stage.Find("First Person Composition - Proxies");if(oldComposition)UnityEngine.Object.DestroyImmediate(oldComposition.gameObject);var composition=new GameObject("First Person Composition - Proxies").transform;composition.SetParent(stage,false);
 var wood=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Wood.mat");var brass=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Brass.mat");
 Cube("Low Lot Pedestal - Model Placeholder",composition,new Vector3(.35f,4.42f,-11.4f),new Vector3(1.5f,.84f,1.0f),wood);Cube("Pedestal Top",composition,new Vector3(.35f,4.87f,-11.4f),new Vector3(1.65f,.10f,1.1f),brass);
 Cube("Participant Front Rail",composition,new Vector3(0,4.55f,-8.1f),new Vector3(7,.08f,.13f),wood);
 foreach(float x in new[]{-3.3f,-1.1f,1.1f,3.3f})Cube("Rail Post",composition,new Vector3(x,4.36f,-8.1f),new Vector3(.11f,.8f,.11f),wood);
 foreach(Transform t in stage)if(t.name=="Wolf Bidder - Static Example"){t.position=new Vector3(t.position.x<0?-2.6f:2.6f,4,-9.3f);t.LookAt(new Vector3(1.5f,4,-13.2f));t.Rotate(0,180,0);}
 var camera=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();camera.transform.position=new Vector3(0,5.45f,-6.35f);camera.transform.LookAt(new Vector3(0,5.3f,-12.8f));camera.fieldOfView=67;camera.name="First Person Auction Preview Camera";
 ApplyAuctionPremiumAssets(hud);PrefabUtility.SaveAsPrefabAsset(hud,Root+"/PF_AuctionHUD_Example.prefab");EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 for(int i=0;i<SceneManager.sceneCount;i++){var other=SceneManager.GetSceneAt(i);if(other!=scene&&other.isLoaded)foreach(var g in other.GetRootGameObjects())if(g.activeSelf){g.SetActive(false);hidden.Add(g);}}
 Capture(camera,hud.GetComponent<Canvas>(),"AuctionSecondFloorPreview.png");
 if(hud.GetComponentsInChildren<Text>(true).Length<20)throw new Exception("Missing editable labels");if(hud.GetComponentsInChildren<Image>(true).Where(i=>i.name.Contains("Icon")||i.name.Contains("Background")).Any(i=>!i.sprite))throw new Exception("Missing sprite");
 File.WriteAllText(Root+"/AuctionSecondFloorVerification.txt","PASS: 1920x1080 HUD render, four custom shaped panels, editable native Text/Image/RectTransform, TL/TC/TR/BC anchors, private direction omitted, first-person camera over participant rail; placeholder pedestal. No input or auction gameplay binding. Original HUD preserved inactive. Central world-space product label deferred.");
 }finally{foreach(var g in hidden)if(g)g.SetActive(true);if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);AssetDatabase.Refresh();}
 }
 [InitializeOnLoadMethod] static void AuctionFirstPersonRequest(){EditorApplication.update+=()=>{if(!File.Exists("Library/AuctionFirstPerson.request")||EditorApplication.isCompiling||EditorApplication.isUpdating)return;File.Delete("Library/AuctionFirstPerson.request");try{ApplyAuctionFirstPerson();File.WriteAllText("Library/AuctionFirstPerson.result","PASS");}catch(Exception ex){File.WriteAllText("Library/AuctionFirstPerson.result",ex.ToString());Debug.LogException(ex);}};}
 }
}


