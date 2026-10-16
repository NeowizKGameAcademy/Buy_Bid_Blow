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
        public static void BuildSecondFloorExample()
        {
            string path=Root+"/SC_AuctionSecondFloorInterface.unity";
            if(File.Exists(path))throw new Exception("Preserving existing scene: "+path);
            if(!AssetDatabase.CopyAsset(ScenePath,path))throw new Exception("Scene copy failed.");
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try {
                var roots=scene.GetRootGameObjects();var camera=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();
                camera.transform.position=new Vector3(0,5.65f,-5.6f);camera.transform.LookAt(new Vector3(0,5.35f,-12.6f));camera.fieldOfView=70;
                var hud=roots.Single(g=>g.name=="HUD - Static Editable Example");AuctionVariant(hud.transform,false);
                font=hud.GetComponentInChildren<Text>(true).font;
                var stage=new GameObject("Auction Stage - Editable Example");SceneManager.MoveGameObjectToScene(stage,scene);
                var wood=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Wood.mat");
                var gold=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Brass.mat");
                Cube("Auctioneer Podium",stage.transform,new Vector3(0,4.55f,-12),new Vector3(2.1f,1.1f,.7f),wood);
                Cube("Podium Top",stage.transform,new Vector3(0,5.14f,-12),new Vector3(2.25f,.12f,.85f),gold);
                var pig=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Content/Characters/Pig/PF_Pig_Black_Model.prefab"),scene);
                pig.name="Pig Auctioneer - Static Model";pig.transform.SetParent(stage.transform);pig.transform.position=new Vector3(0,4,-12.8f);pig.transform.rotation=Quaternion.identity;
                var wolfAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Content/Characters/Wolf/PF_Wolf_Model.prefab");
                foreach(float x in new[]{-2.7f,2.7f}) {
                    var wolf=(GameObject)PrefabUtility.InstantiatePrefab(wolfAsset,scene);wolf.name="Wolf Bidder - Static Example";wolf.transform.SetParent(stage.transform);wolf.transform.position=new Vector3(x,4,-8.5f);wolf.transform.rotation=Quaternion.Euler(0,x<0?205:155,0);
                }
                var sign=new GameObject("Auction Stage Sign",typeof(RectTransform),typeof(Canvas));sign.transform.SetParent(stage.transform);sign.transform.position=new Vector3(0,7.4f,-14.5f);sign.transform.rotation=Quaternion.identity;sign.transform.localScale=Vector3.one*.006f;
                sign.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;sign.GetComponent<RectTransform>().sizeDelta=new Vector2(900,120);
                RoundedDim(sign.transform,Icon("BG_RoundedDim"),900,120,0,0,.65f);Text("Title",sign.transform,"INFORMATION AUCTION",42,900,120,0,0,TextAnchor.MiddleCenter).color=AuctionGold;
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Capture(camera,hud.GetComponent<Canvas>(),"AuctionSecondFloorPreview.png");
                File.WriteAllText("Library/InterfaceSecondFloor.result","PASS: saved second floor scene and 1920x1080 preview. Camera "+camera.transform.position+"; model-only pig and wolf prefabs; editable auction HUD.");
            }finally{EditorSceneManager.CloseScene(scene,true);AssetDatabase.Refresh();}
        }
        [InitializeOnLoadMethod] static void SecondFloorRequest()
        {
            if(!File.Exists("Library/InterfaceSecondFloor.request"))return;
            EditorApplication.delayCall+=()=>{File.Delete("Library/InterfaceSecondFloor.request");try{BuildSecondFloorExample();}catch(Exception ex){File.WriteAllText("Library/InterfaceSecondFloor.result",ex.ToString());Debug.LogException(ex);}};
        }
        [InitializeOnLoadMethod] static void SecondFloorRefineRequest()
        {
            if(!File.Exists("Library/InterfaceSecondFloorRefine.request"))return;
            EditorApplication.delayCall+=()=>{
                File.Delete("Library/InterfaceSecondFloorRefine.request");
                var scene=EditorSceneManager.OpenScene(Root+"/SC_AuctionSecondFloorInterface.unity",OpenSceneMode.Additive);
                try {
                    var roots=scene.GetRootGameObjects();var stage=roots.Single(g=>g.name=="Auction Stage - Editable Example");
                    var pig=stage.transform.Find("Pig Auctioneer - Static Model");pig.localScale=Vector3.one*3; pig.position=new Vector3(0,4.15f,-12.8f);pig.rotation=Quaternion.identity;
                    stage.transform.Find("Auction Stage Sign").rotation=Quaternion.Euler(0,180,0);stage.transform.Find("Auction Stage Sign").position=new Vector3(0,8.1f,-14.5f);
                    foreach(Transform t in stage.transform)if(t.name=="Wolf Bidder - Static Example") {t.position=new Vector3(t.position.x,4,-9.3f);t.rotation=Quaternion.Euler(0,t.position.x<0?265:95,0);}
                    EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                    Capture(roots.SelectMany(g=>g.GetComponentsInChildren<Camera>()).First(),roots.Single(g=>g.name=="HUD - Static Editable Example").GetComponent<Canvas>(),"AuctionSecondFloorPreview.png");
                    File.WriteAllText("Library/InterfaceSecondFloorRefine.result","PASS");
                }catch(Exception ex){File.WriteAllText("Library/InterfaceSecondFloorRefine.result",ex.ToString());Debug.LogException(ex);}
                finally{EditorSceneManager.CloseScene(scene,true);AssetDatabase.Refresh();}
            };
        }
    }
}
