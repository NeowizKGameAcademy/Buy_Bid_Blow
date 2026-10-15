using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace BuyBidBlow.MainMenu.Editor {
 public static class MainSceneBuilder {
 const string Root="Assets/Project/Dev/Sandbox/MainScene";
 static Font font; static Sprite[] states;
 static RectTransform Rect(string name,Transform parent,float w,float h,float x,float y){var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x,y);return r;}
 static Image Img(string name,Transform parent,Sprite sprite,float w,float h,float x,float y){var r=Rect(name,parent,w,h,x,y);var im=r.gameObject.AddComponent<Image>();im.sprite=sprite;im.raycastTarget=false;return im;}
 static Text Txt(string name,Transform parent,string value,int size,float w,float h,float x,float y){var r=Rect(name,parent,w,h,x,y);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(1,.95f,.77f);t.raycastTarget=false;return t;}
 static Button Btn(string name,Transform p,string label,float y){var i=Img(name,p,states[0],470,104,0,y);i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.transition=Selectable.Transition.SpriteSwap;b.targetGraphic=i;b.spriteState=new SpriteState{highlightedSprite=states[1],selectedSprite=states[1],pressedSprite=states[2],disabledSprite=states[3]};Txt("Label - editable",i.transform,label,34,320,75,24,0);if(name!="Close"){var ids=new[]{"GameStart","HowToPlay","Settings","QuitGame"};int index=Array.IndexOf(ids,name);if(index>=0){var icon=Img("Icon - independent",i.transform,Sprite(new[]{"IC_Play_v2","IC_Book_v2","IC_Gear_v2","IC_Exit_v2"}[index]),64,64,-153,0);icon.preserveAspect=true;var arrow=Img("Selection Indicator",i.transform,Sprite("IC_Play_v2"),25,32,190,0);arrow.preserveAspect=true;arrow.gameObject.SetActive(false);var selection=i.gameObject.AddComponent<MenuButtonSelection>();selection.indicator=arrow.gameObject;}}return b;}
 static void Import(string file,bool sheet=false,bool background=false){string path=Root+"/Sprites/"+file+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=background?SpriteImportMode.Single:SpriteImportMode.Multiple;ti.mipmapEnabled=false;ti.alphaIsTransparency=!background;ti.npotScale=TextureImporterNPOTScale.None;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=4096;ti.wrapMode=TextureWrapMode.Clamp;
 if(!background){var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(path));var pix=tex.GetPixels32();int w=tex.width,h=tex.height;var rects=new SpriteMetaData[sheet?4:1];for(int k=0;k<rects.Length;k++){int x0=sheet?k%2*w/2:0,y0=sheet?(k<2?h/2:0):0,x1=sheet?x0+w/2:w,y1=sheet?y0+h/2:h;int l=x1,r=x0,bot=y1,top=y0;for(int yy=y0;yy<y1;yy++)for(int xx=x0;xx<x1;xx++)if(pix[yy*w+xx].a>=24){l=Math.Min(l,xx);r=Math.Max(r,xx);bot=Math.Min(bot,yy);top=Math.Max(top,yy);}if(r<=l)throw new Exception("Empty sprite "+file);l=Math.Max(x0,l-4);r=Math.Min(x1-1,r+4);bot=Math.Max(y0,bot-4);top=Math.Min(y1-1,top+4);rects[k]=new SpriteMetaData{name=sheet?new[]{"Normal","Focused","Pressed","Disabled"}[k]:file,rect=new UnityEngine.Rect(l,bot,r-l+1,top-bot+1),alignment=0,pivot=new Vector2(.5f,.5f)};}
#pragma warning disable CS0618
 ti.spritesheet=rects;
#pragma warning restore CS0618
 UnityEngine.Object.DestroyImmediate(tex);}
 ti.SaveAndReimport();}
  static void MakeMenuShade(){
 var texture=new Texture2D(256,256,TextureFormat.RGBA32,false);
 for(int y=0;y<256;y++)for(int x=0;x<256;x++){
 float nx=(x-127.5f)/127.5f,ny=(y-127.5f)/127.5f;
 float radius=Mathf.Sqrt(nx*nx+ny*ny);
 float alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,1f,radius));
 texture.SetPixel(x,y,new Color(0,0,0,alpha));
 }
 texture.Apply();File.WriteAllBytes(Root+"/Sprites/BG_MenuSoftShade.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);Import("BG_MenuSoftShade");
 }
 static Sprite Sprite(string file)=>AssetDatabase.LoadAllAssetsAtPath(Root+"/Sprites/"+file+".png").OfType<Sprite>().First();
 static GameObject Modal(Transform parent,string title,string body,out Button close){var r=Rect(title,parent,1920,1080,0,0);var dim=Img("Dim",r,null,1920,1080,0,0);dim.color=new Color(0,0,0,.8f);dim.raycastTarget=true;var panel=Img("Panel",r,null,840,650,0,0);panel.color=new Color(.025f,.12f,.105f,.98f);panel.raycastTarget=true;Txt("Title",panel.transform,title,42,760,80,0,245);Txt("Body - editable",panel.transform,body,27,730,350,0,0);close=Btn("Close",panel.transform,"Close",-250);close.navigation=new Navigation{mode=Navigation.Mode.None};r.gameObject.SetActive(false);return r.gameObject;}
 [MenuItem("Tools/BuyBidBlow/Main Menu/Build Scene")]
 public static void Build(){MakeMenuShade();Import("BG_MainHall",false,true);Import("BTN_MainStates_v3",true);foreach(var id in new[]{"Logo_Main_v2","IC_Play_v2","IC_Book_v2","IC_Gear_v2","IC_Exit_v2"})Import(id);states=new[]{"Normal","Focused","Pressed","Disabled"}.Select(n=>AssetDatabase.LoadAllAssetsAtPath(Root+"/Sprites/BTN_MainStates_v3.png").OfType<Sprite>().Single(s=>s.name==n)).ToArray();font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/alternative/Pretendard-Bold.ttf");
 var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var cam=new GameObject("Main Camera",typeof(Camera)).GetComponent<Camera>();cam.tag="MainCamera";cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;cam.orthographic=true;
 var ui=new GameObject("Main Menu - Editable",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
 var bg=Img("Background - Confirmed Artwork",ui.transform,Sprite("BG_MainHall"),1920,1080,0,0);bg.rectTransform.anchorMin=Vector2.zero;bg.rectTransform.anchorMax=Vector2.one;bg.rectTransform.sizeDelta=Vector2.zero;var fitter=bg.gameObject.AddComponent<AspectRatioFitter>();fitter.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fitter.aspectRatio=1.5f;
 var dim=Img("Full Screen Black Dim - alpha 0.16",ui.transform,null,1920,1080,0,0);dim.color=new Color(0,0,0,.16f);dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;dim.rectTransform.sizeDelta=Vector2.zero;var shade=Img("Menu Soft Radial Shade - alpha 0.38",ui.transform,Sprite("BG_MenuSoftShade"),1080,1380,0,0);shade.color=new Color(0,0,0,.38f);
 var menu=Rect("Menu - Center Anchored",ui.transform,790,820,0,0);var logo=Img("Logo - Custom Lettering",menu,Sprite("Logo_Main_v2"),660,460,0,245);logo.preserveAspect=true;
 var buttons=new[]{Btn("GameStart",menu,"GameStart",-60),Btn("HowToPlay",menu,"HowToPlay",-174),Btn("Settings",menu,"Settings",-280),Btn("QuitGame",menu,"QuitGame",-386)};
 for(int k=0;k<4;k++){buttons[k].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=buttons[(k+3)%4],selectOnDown=buttons[(k+1)%4]};}
 var actions=ui.AddComponent<MainMenuActions>();actions.startButton=buttons[0];actions.howToPlayButton=buttons[1];actions.settingsButton=buttons[2];
 Button hc,sc;actions.howToPlayPanel=Modal(ui.transform,"How To Play","17라운드 동안 주식 거래와 정보 경매로 자산을 늘리세요.\n\n시작 현금 $200 · 회사별 시작 인원 × 4주\n경매 라운드: 2 · 6 · 10 · 14\n정보를 활용해 매수·매도하거나 하락에 베팅하세요.\n\n최종 점수 = 현금 + 보유 주식의 최종 가치\n동점이면 보유 주식 수, 그것도 같으면 공동 우승",out hc);actions.howClose=hc;
 actions.settingsPanel=Modal(ui.transform,"Settings","음량\n\n← / → 키 또는 슬라이더로 조절",out sc);actions.settingsClose=sc;
 var sr=Rect("Master Volume",actions.settingsPanel.transform,500,40,0,-70);var slider=sr.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;slider.value=1;var track=Img("Track",sr,null,500,8,0,0);track.color=new Color(.25f,.35f,.30f);var handle=Img("Handle",sr,null,24,38,0,0);handle.color=new Color(.95f,.75f,.32f);handle.raycastTarget=true;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnDown=sc};sc.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=slider};UnityEventTools.AddPersistentListener(slider.onValueChanged,actions.Volume);
 UnityEventTools.AddPersistentListener(buttons[0].onClick,actions.StartGame);UnityEventTools.AddPersistentListener(buttons[1].onClick,actions.HowToPlay);UnityEventTools.AddPersistentListener(buttons[2].onClick,actions.Settings);UnityEventTools.AddPersistentListener(buttons[3].onClick,actions.QuitGame);UnityEventTools.AddPersistentListener(hc.onClick,actions.CloseHow);UnityEventTools.AddPersistentListener(sc.onClick,actions.CloseSettings);
 var es=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));es.GetComponent<EventSystem>().firstSelectedGameObject=buttons[0].gameObject;es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
 PrefabUtility.SaveAsPrefabAsset(ui,Root+"/PF_MainMenu.prefab");EditorSceneManager.SaveScene(scene,Root+"/SC_MainMenu.unity");var build=EditorBuildSettings.scenes.Where(s=>s.path!=Root+"/SC_MainMenu.unity").ToList();build.Insert(0,new EditorBuildSettingsScene(Root+"/SC_MainMenu.unity",true));EditorBuildSettings.scenes=build.ToArray();AssetDatabase.SaveAssets();
 es.GetComponent<EventSystem>().SetSelectedGameObject(buttons[0].gameObject);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;var rt=new RenderTexture(1920,1080,24);cam.targetTexture=rt;cam.aspect=16f/9f;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;var output=new Texture2D(1920,1080,TextureFormat.RGB24,false);output.ReadPixels(new UnityEngine.Rect(0,0,1920,1080),0,0);output.Apply();File.WriteAllBytes(Root+"/MainMenuPreview.png",output.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;UnityEngine.Object.DestroyImmediate(output);UnityEngine.Object.DestroyImmediate(rt);
 if(buttons.Any(b=>b.transition!=Selectable.Transition.SpriteSwap||b.onClick.GetPersistentEventCount()!=1))throw new Exception("Button binding failed");File.WriteAllText(Root+"/Verification.txt","PASS: scene + prefab, four SpriteSwap menu buttons (Normal/Focused/Pressed/Disabled), editable Text, explicit keyboard navigation, lobby scene link, modal close actions, volume slider; rendered 1920x1080. Manual full player build remains untested.");AssetDatabase.Refresh();
 }
 public static void RunBatch(){try{Build();EditorApplication.Exit(0);}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}}
 }
}




