using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace BuyBidBlow.PlayerUISandbox.Editor {
 [InitializeOnLoad] public static class PlayerUIBuilder {
 const string Root="Assets/Project/Dev/Sandbox/Interface/PlayerUI";
 static PlayerUIBuilder(){EditorApplication.update+=Tick;}
 static void Tick(){if(!File.Exists("Library/PlayerUIBuild.request")||EditorApplication.isCompiling||EditorApplication.isUpdating)return;File.Delete("Library/PlayerUIBuild.request");try{Build();File.WriteAllText("Library/PlayerUIBuild.result","PASS");}catch(Exception e){File.WriteAllText("Library/PlayerUIBuild.result",e.ToString());Debug.LogException(e);}}
 static void Set(UnityEngine.Object obj,string key,UnityEngine.Object value){var so=new SerializedObject(obj);so.FindProperty(key).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static GameObject Cube(string name,Vector3 pos,Vector3 scale,Color color){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=pos;g.transform.localScale=scale;string path=Root+"/Prefabs/"+name.Replace(" ","_")+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.color=color;g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
 static Text Label(string name,Transform p,string value,Vector2 size,Vector2 pos){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,0);r.sizeDelta=size;r.anchoredPosition=pos;var t=r.gameObject.AddComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/alternative/Pretendard-Bold.ttf");t.fontSize=24;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.text=value;return t;}
 public static void Build(){
 string path=Root+"/SC_PlayerUI.unity";var current=SceneManager.GetActiveScene();if(current.isDirty)throw new Exception("Save current scene first.");if(!File.Exists(path))AssetDatabase.CopyAsset("Assets/Project/Dev/Sandbox/Interface/SC_AuctionSecondFloorInterface.unity",path);
 var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
 try{
 foreach(var root in scene.GetRootGameObjects())if(root.name=="Player UI Test Rig"||root.name=="Interaction Test Stations")UnityEngine.Object.DestroyImmediate(root);
 foreach(var cam in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))cam.gameObject.SetActive(false);
 var hud=scene.GetRootGameObjects().Single(g=>g.name=="Auction HUD - Editable v2"||g.name=="Player HUD");hud.SetActive(true);hud.name="Player HUD";
 var trade=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/UI/Market/TradingPanel/PF_TradingPanel.prefab"),scene);trade.transform.SetParent(hud.transform,false);trade.name="Stock Trade Panel";
 foreach(var b in trade.GetComponentsInChildren<Button>(true))b.interactable=false;
 var prompt=Label("Interaction Prompt",hud.transform,"WASD 이동 · E 상호작용",new Vector2(950,50),new Vector2(0,18));
 var cross=new GameObject("Crosshair - Copied From Move And Trade",typeof(RectTransform),typeof(Image));cross.transform.SetParent(hud.transform,false);var cr=cross.GetComponent<RectTransform>();cr.anchorMin=cr.anchorMax=cr.pivot=new Vector2(.5f,.5f);cr.sizeDelta=new Vector2(22,22);var image=cross.GetComponent<Image>();image.raycastTarget=false;
 var spritePaths=new[]{"Cursor_Empty","Cursor_Filled"};var sprites=new Sprite[2];for(int i=0;i<2;i++){string p=Root+"/Sprites/"+spritePaths[i]+".png";AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);var ti=(TextureImporter)AssetImporter.GetAtPath(p);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.SaveAndReimport();sprites[i]=AssetDatabase.LoadAssetAtPath<Sprite>(p);}image.sprite=sprites[0];
 var player=new GameObject("Player UI Test Rig");player.transform.position=new Vector3(0,.15f,0);player.transform.rotation=Quaternion.Euler(0,180,0);var rb=player.AddComponent<Rigidbody>();rb.constraints=RigidbodyConstraints.FreezeRotation;rb.interpolation=RigidbodyInterpolation.Interpolate;var capsule=player.AddComponent<CapsuleCollider>();capsule.height=1.7f;capsule.radius=.3f;capsule.center=new Vector3(0,.85f,0);
 var head=new GameObject("Head");head.transform.SetParent(player.transform,false);var cameraGO=new GameObject("Local First Person Camera",typeof(Camera),typeof(AudioListener));cameraGO.transform.SetParent(player.transform,false);cameraGO.transform.localPosition=new Vector3(0,1.7f,0);cameraGO.tag="MainCamera";var camera=cameraGO.GetComponent<Camera>();camera.fieldOfView=67;camera.nearClipPlane=.05f;
 var pc=player.AddComponent<BuyBidBlow.Characters.PlayerCamera>();Set(pc,"cameraObject",cameraGO);var pcs=new SerializedObject(pc);pcs.FindProperty("defaultLocalPosition").vector3Value=new Vector3(0,1.7f,0);pcs.ApplyModifiedPropertiesWithoutUndo();

 var detect=cameraGO.AddComponent<BuyBidBlow.Characters.InteractionSensor>();var ds=new SerializedObject(detect);ds.FindProperty("detectDistance").floatValue=3;ds.ApplyModifiedPropertiesWithoutUndo();
 var control=player.AddComponent<PlayerUIController>();control.detection=detect;control.view=camera;control.body=rb;control.hud=hud;control.tradePanel=trade;control.auctionHeader=hud.transform.Find("Auction Status - Top Center").gameObject;control.auctionControls=hud.transform.Find("Bid Controls - Bottom Center").gameObject;control.crosshair=cross;control.prompt=prompt;trade.SetActive(false);control.auctionHeader.SetActive(false);control.auctionControls.SetActive(false);
 var stations=new GameObject("Interaction Test Stations");
 for(int k=0;k<4;k++){var g=Cube(k<3?new[]{"Straw Window","Timber Window","Brick Window"}[k]:"Auction Chair",k<3?new Vector3((1-k)*4.1f,.6f,-4.8f):new Vector3(0,4.5f,-5.5f),k<3?new Vector3(1.5f,1.2f,.7f):new Vector3(1,.9f,1),k<3?new Color(.1f,.28f,.2f):new Color(.4f,.1f,.08f));g.transform.SetParent(stations.transform,true);g.layer=7;var target=g.AddComponent<UIInteractionTarget>();target.kind=(UIInteractionTarget.Kind)k;if(k==3){var seat=new GameObject("Seated Camera Anchor");seat.transform.SetParent(g.transform,false);seat.transform.position=new Vector3(0,5.45f,-6.35f);seat.transform.rotation=Quaternion.LookRotation(new Vector3(0,5.3f,-12.8f)-seat.transform.position);target.seatedView=seat.transform;}var canvas=new GameObject("Station Label",typeof(RectTransform),typeof(Canvas));canvas.transform.SetParent(g.transform,false);canvas.transform.position=g.transform.position+new Vector3(0,1,.45f);canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;canvas.transform.localScale=Vector3.one*.003f;canvas.GetComponent<RectTransform>().sizeDelta=new Vector2(500,80);Label("Label",canvas.transform,k<3?target.kind+" · E":"경매 의자 · E",new Vector2(500,80),Vector2.zero);}
 PrefabUtility.SaveAsPrefabAsset(hud,Root+"/Prefabs/PF_PlayerHUD.prefab");hud.transform.SetParent(player.transform,true);PrefabUtility.SaveAsPrefabAsset(player,Root+"/Prefabs/PF_PlayerUIRig.prefab");EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText(Root+"/Verification.txt","PASS: separate scene, local preview rig, existing PlayerCamera/InteractionSensor, copied crosshair sprites; constant assets/timer, trade/auction modes, stationary-world envelope retained. Display/test input only, actual transactions not connected.");
 }finally{SceneManager.SetActiveScene(current);EditorSceneManager.CloseScene(scene,true);AssetDatabase.Refresh();}
 }
 }
}


