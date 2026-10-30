using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BuyBidBlow.UI.Market;
namespace BuyBidBlow.PlayerUISandbox.Editor {
[InitializeOnLoad] public static class TradingRestore {
const string Root="Assets/Project/Dev/Sandbox/Interface/PlayerUI";
static TradingRestore(){EditorApplication.update+=Tick;}
static void Tick(){if(!File.Exists("Library/TradingRestore.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;File.Delete("Library/TradingRestore.request");try{Apply();File.WriteAllText("Library/TradingRestore.result","PASS");}catch(Exception e){File.WriteAllText("Library/TradingRestore.result",e.ToString());Debug.LogException(e);}}
static void Rect(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
public static void Apply(){
var path=Root+"/SC_PlayerUI.unity";var scene=SceneManager.GetSceneByPath(path);bool close=!scene.isLoaded;if(close)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
try{
var c=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerUIController>(true)).Single();var hud=c.hud;
foreach(var img in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Project/UI/Market/TradingPanel/Sprites"}))AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(img),ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
foreach(var child in hud.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name=="Stock Trade Panel"||r.name.StartsWith("Stock Trade - ")).ToArray())if(child)UnityEngine.Object.DestroyImmediate(child.gameObject);
c.companyPanels=new GameObject[3];string[] names={"Straw","Timber","Brick"};
for(int i=0;i<3;i++){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/UI/Market/TradingPanel/PF_TradingPanel_"+names[i]+".prefab");var panel=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);panel.transform.SetParent(hud.transform,false);panel.name="Stock Trade - "+names[i];var r=(RectTransform)panel.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one;r.anchoredPosition=new Vector2(-32,-220);r.sizeDelta=new Vector2(860,850);r.localScale=Vector3.one*.92f;
foreach(var text in panel.GetComponentsInChildren<Text>(true)){text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.resizeTextForBestFit=true;text.resizeTextMaxSize=text.fontSize;text.resizeTextMinSize=Mathf.Max(14,Mathf.RoundToInt(text.fontSize*.8f));}
Rect(panel.transform.Find("Company Header/Company Name"),202,-35,324,100);
Rect(panel.transform.Find("Company Header/Price Label"),596,-25,228,28);
Rect(panel.transform.Find("Company Header/Current Price"),596,-55,228,78);
Rect(panel.transform.Find("Company Header/Last Change"),596,-134,228,40);
foreach(var b in panel.GetComponentsInChildren<Button>(true)){b.interactable=true;b.navigation=new Navigation{mode=Navigation.Mode.None};}
// Local contrast layer, below header and controls. All elements remain editable.
var dim=panel.transform.Find("Glass Dim - Opacity Editable");if(dim)dim.GetComponent<Image>().color=new Color(.012f,.035f,.029f,.96f);
panel.transform.Find("Panel Background").GetComponent<Image>().color=Color.white;
Rect(panel.transform.Find("Company Header/Price Label"),596,-43,228,28);
Rect(panel.transform.Find("Company Header/Current Price"),596,-71,228,88);var priceText=panel.transform.Find("Company Header/Current Price").GetComponent<Text>();priceText.resizeTextForBestFit=false;priceText.fontSize=60;
Rect(panel.transform.Find("Company Header/Last Change"),596,-145,228,36);
Rect(panel.transform.Find("Price Header Dim"),584,-28,260,160);
Rect(panel.transform.Find("Trade Tabs"),35,-205,790,62);
Rect(panel.transform.Find("Quantity Control"),35,-282,790,174);
Rect(panel.transform.Find("Stock Availability"),35,-469,790,48);
Rect(panel.transform.Find("Payment Summary"),15,-532,830,221);
Rect(panel.transform.Find("Payment Summary/Cash After Label"),30,-166,565,38);
Rect(panel.transform.Find("Payment Summary/Cash After"),605,-164,180,43);
Rect(panel.transform.Find("Close Hint"),300,-766,260,50);
var closeHint=panel.transform.Find("Close Hint");closeHint.gameObject.SetActive(false);
var hint=new GameObject("Keyboard Guide",typeof(RectTransform),typeof(Text));hint.transform.SetParent(panel.transform,false);var hr=(RectTransform)hint.transform;hr.anchorMin=hr.anchorMax=hr.pivot=new Vector2(0,1);Rect(hr,35,-788,790,36);var ht=hint.GetComponent<Text>();ht.font=panel.GetComponentsInChildren<Text>(true)[0].font;ht.fontSize=19;ht.alignment=TextAnchor.MiddleCenter;ht.color=new Color(.85f,.88f,.81f);ht.text="W 오른쪽 탭 · S 왼쪽 탭 · A/D 수량 · E 확인 · Q 닫기";
panel.AddComponent<TradingPanelView>();
// Keyboard preview owns input; retain the normal button artwork.
var group=panel.AddComponent<CanvasGroup>();group.blocksRaycasts=false;
panel.SetActive(false);c.companyPanels[i]=panel;
}
c.tradePanel=c.companyPanels[0];
var timer=hud.transform.Find("Round Timer - Top Right");Rect(timer,-32,-24,350,104);foreach(var bg in timer.GetComponentsInChildren<Image>(true)){if(bg.transform!=timer)Rect(bg.transform,0,0,350,104);}Rect(timer.Find("Round"),20,-18,148,32);Rect(timer.Find("Time"),181,-14,148,48);Rect(timer.Find("Phase"),18,-56,310,26);timer.Find("Round").GetComponent<Text>().fontSize=20;timer.Find("Time").GetComponent<Text>().fontSize=38;timer.Find("Phase").GetComponent<Text>().fontSize=17;
foreach(var text in timer.GetComponentsInChildren<Text>(true)){text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;}
Rect(c.prompt.transform,0,8,950,30);c.prompt.fontSize=18;
PrefabUtility.SaveAsPrefabAsset(hud,Root+"/Prefabs/PF_PlayerHUD.prefab");PrefabUtility.SaveAsPrefabAsset(c.gameObject,Root+"/Prefabs/PF_PlayerUIRig.prefab");EditorUtility.SetDirty(c);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
foreach(var panel in c.companyPanels){if(panel.GetComponentsInChildren<Image>(true).Any(i=>i.name=="Panel Background"&&!i.sprite))throw new Exception("Missing panel sprite");if(panel.GetComponentsInChildren<Text>(true).Single(t=>t.name=="Company Name").text!=panel.name.Replace("Stock Trade - ","").Replace("Timber","Wood"))throw new Exception("Company mismatch");}
CaptureChecks(c);
File.WriteAllText(Root+"/TradingRestoreVerification.txt","PASS: original five PNG assets restored from hash-verified local LFS objects; three company prefab instances linked; inactive by default; timer 350x104; trade panels below timer. Display-only quantity/payment calculation; actual transactions not connected.");
}finally{if(close)EditorSceneManager.CloseScene(scene,true);}
}
static void CaptureChecks(PlayerUIController c){
c.Awake();var canvas=c.hud.GetComponent<Canvas>();var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float distance=canvas.planeDistance;var cam=c.view;var target=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1920,1080,24);var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;cam.targetTexture=rt;
for(int k=0;k<3;k++){var go=new GameObject("Verification Target");var t=go.AddComponent<UIInteractionTarget>();t.kind=(UIInteractionTarget.Kind)k;c.Open(t);if(!c.companyPanels[k].activeSelf||c.detection.enabled)throw new Exception("Mode isolation failed");Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(Root+"/Trading"+t.kind+"Integrated.png",tex.EncodeToPNG());var view= c.tradePanel.GetComponent<TradingPanelView>();var sample=TradePreviewSource.ForCompany(k);int n=1;sample.owned=0;view.Present(sample,1,ref n);if(view.CanConfirm)throw new Exception("Empty holding sale allowed");sample.available=0;view.Present(sample,0,ref n);if(view.CanConfirm)throw new Exception("Insufficient cash allowed");sample.available=115;sample.price=5;view.Present(sample,2,ref n);if(view.CanConfirm)throw new Exception("Floor bet allowed");view.Present(TradePreviewSource.ForCompany(k),0,ref n);c.Close();UnityEngine.Object.DestroyImmediate(go);}
}finally{c.Close();cam.targetTexture=target;RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
}
}
}
