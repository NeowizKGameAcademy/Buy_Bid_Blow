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
 const string PremiumRoot=Root+"/PremiumSprites";
 static readonly string[] PremiumIds={"BG_Assets_Luxe","BG_Header_Luxe","BG_Timer_Luxe","BG_Bid_Luxe","BG_Lot_Luxe","BTN_BidStates_Luxe","BG_Keycap_Luxe","IC_Cash_Luxe","IC_Straw_Luxe","IC_Timber_Luxe","IC_Brick_Luxe"};
 static Sprite PremiumSprite(string id,string state=null)=>AssetDatabase.LoadAllAssetsAtPath(PremiumRoot+"/"+id+".png").OfType<Sprite>().Single(s=>state==null||s.name==state);
 static void ImportPremium(string id){
 string path=PremiumRoot+"/"+id+".png";var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!tex.LoadImage(File.ReadAllBytes(path)))throw new Exception("Invalid PNG: "+id);
 int w=tex.width,h=tex.height;var pix=tex.GetPixels32();if(pix[0].a>8)throw new Exception("Non-transparent corner: "+id);int count=id=="BTN_BidStates_Luxe"?4:1;var slices=new SpriteMetaData[count];
 for(int k=0;k<count;k++){int x0=count==4?k%2*w/2:0,y0=count==4?(k<2?h/2:0):0,x1=count==4?x0+w/2:w,y1=count==4?y0+h/2:h;int left=x1,right=x0,bottom=y1,top=y0;
 for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pix[y*w+x].a>=24){left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
 if(left>=right||bottom>=top)throw new Exception("Empty sprite "+id);left=Math.Max(x0,left-3);bottom=Math.Max(y0,bottom-3);right=Math.Min(x1-1,right+3);top=Math.Min(y1-1,top+3);
 slices[k]=new SpriteMetaData{name=count==4?new[]{"Normal","Focused","Pressed","Disabled"}[k]:id,rect=new Rect(left,bottom,right-left+1,top-bottom+1),alignment=0,pivot=new Vector2(.5f,.5f)};
 }
 UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.wrapMode=TextureWrapMode.Clamp;
#pragma warning disable CS0618
 importer.spritesheet=slices;
#pragma warning restore CS0618
 importer.SaveAndReimport();
 }
 static void ApplyAuctionPremiumAssets(GameObject hud){
 if(!PremiumIds.All(id=>File.Exists(PremiumRoot+"/"+id+".png")))return;
 foreach(var id in PremiumIds)ImportPremium(id);
 string[] paths={"Assets - Top Left","Auction Status - Top Center","Round Timer - Top Right","Bid Controls - Bottom Center"};string[] ids={"BG_Assets_Luxe","BG_Header_Luxe","BG_Timer_Luxe","BG_Bid_Luxe"};
 for(int k=0;k<paths.Length;k++){var im=hud.transform.Find(paths[k]+"/Background - editable opacity").GetComponent<Image>();im.sprite=PremiumSprite(ids[k]);im.color=new Color(1,1,1,.84f);im.type=UnityEngine.UI.Image.Type.Simple;im.preserveAspect=false;}
 var assets=hud.transform.Find(paths[0]);assets.Find("Total Label").GetComponent<RectTransform>().anchoredPosition=new Vector2(28,-34);assets.Find("Total Value").GetComponent<RectTransform>().anchoredPosition=new Vector2(305,-34);assets.Find("Reserved Cash").GetComponent<RectTransform>().anchoredPosition=new Vector2(27,-169);var bidPanel=hud.transform.Find(paths[3]);bidPanel.Find("Amount Label").GetComponent<RectTransform>().anchoredPosition=new Vector2(33,-34);bidPanel.Find("Cash After").GetComponent<RectTransform>().anchoredPosition=new Vector2(33,-172);bidPanel.Find("Rule").GetComponent<RectTransform>().anchoredPosition=new Vector2(430,-172);assets.Find("Cash Icon").GetComponent<Image>().sprite=PremiumSprite("IC_Cash_Luxe");for(int k=0;k<3;k++)assets.Find("Holding Icon "+k).GetComponent<Image>().sprite=PremiumSprite(new[]{"IC_Straw_Luxe","IC_Timber_Luxe","IC_Brick_Luxe"}[k]);
 foreach(var im in hud.GetComponentsInChildren<Image>(true).Where(i=>i.name.StartsWith("Keycap "))){im.sprite=PremiumSprite("BG_Keycap_Luxe");im.color=Color.white;im.type=UnityEngine.UI.Image.Type.Simple;im.preserveAspect=false;}
 var submit=hud.transform.Find(paths[3]+"/Submit - Visual Only").GetComponent<Image>();submit.sprite=PremiumSprite("BTN_BidStates_Luxe","Normal");submit.color=Color.white;submit.type=UnityEngine.UI.Image.Type.Simple;submit.preserveAspect=false;
 var button=submit.GetComponent<Button>();if(!button)button=submit.gameObject.AddComponent<Button>();button.targetGraphic=submit;button.transition=Selectable.Transition.SpriteSwap;button.spriteState=new SpriteState{highlightedSprite=PremiumSprite("BTN_BidStates_Luxe","Focused"),selectedSprite=PremiumSprite("BTN_BidStates_Luxe","Focused"),pressedSprite=PremiumSprite("BTN_BidStates_Luxe","Pressed"),disabledSprite=PremiumSprite("BTN_BidStates_Luxe","Disabled")};button.navigation=new Navigation{mode=Navigation.Mode.None};submit.raycastTarget=false;
 var reserve=hud.transform.Find("World Lot Label - Reserved For Later");if(reserve&&!reserve.Find("Lot Background - Ready For Later")){var bg=Image("Lot Background - Ready For Later",reserve,PremiumSprite("BG_Lot_Luxe"),320,200,0,0,new Color(1,1,1,.84f));bg.preserveAspect=false;}if(reserve)reserve.gameObject.SetActive(false);
 }
 [MenuItem("Tools/BuyBidBlow/Interface/Apply Premium Auction Sprites")]
 public static void ApplyPremiumAuction(){
 if(!PremiumIds.All(id=>File.Exists(PremiumRoot+"/"+id+".png")))throw new Exception("Missing premium sprite files");
 string path=Root+"/SC_AuctionSecondFloorInterface.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);var hidden=new System.Collections.Generic.List<GameObject>();
 try{var hud=scene.GetRootGameObjects().Single(g=>g.name=="Auction HUD - Editable v2");int count=hud.GetComponentsInChildren<Text>(true).Length;ApplyAuctionPremiumAssets(hud);if(hud.GetComponentsInChildren<Text>(true).Length!=count)throw new Exception("Text structure changed");PrefabUtility.SaveAsPrefabAsset(hud,Root+"/PF_AuctionHUD_Example.prefab");EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 for(int k=0;k<SceneManager.sceneCount;k++){var other=SceneManager.GetSceneAt(k);if(other!=scene&&other.isLoaded)foreach(var g in other.GetRootGameObjects())if(g.activeSelf){hidden.Add(g);g.SetActive(false);}}
 var cam=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();Capture(cam,hud.GetComponent<Canvas>(),"AuctionSecondFloorPreview.png");
 File.WriteAllText(Root+"/AuctionPremiumVerification.txt","PASS: 11 generated transparent PNGs / 14 sprite subassets; editable Text count preserved ("+count+"); four panel backgrounds, cash/company icons, keycaps, 4-state SpriteSwap bid button. Scene and prefab saved; isolated 1920x1080 render. Central product panel remains inactive. No gameplay binding added.");
 }finally{foreach(var g in hidden)if(g)g.SetActive(true);if(opened)EditorSceneManager.CloseScene(scene,true);AssetDatabase.Refresh();}
 }
 [InitializeOnLoadMethod] static void PremiumRequest(){EditorApplication.update+=()=>{if(!File.Exists("Library/AuctionPremium.request")||EditorApplication.isCompiling||EditorApplication.isUpdating)return;File.Delete("Library/AuctionPremium.request");try{ApplyPremiumAuction();File.WriteAllText("Library/AuctionPremium.result","PASS");}catch(Exception ex){File.WriteAllText("Library/AuctionPremium.result",ex.ToString());Debug.LogException(ex);}};}
 }
}


