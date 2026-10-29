using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Network;
using BuyBidBlow.Sandbox.StockGacha;
using BuyBidBlow.UI.Lobby;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Dev.Editor
{
    public static class StockGachaSandboxBuilder
    {
        public const string Root="Assets/Project/Dev/Sandbox/StockGacha";
        public const string ScenePath=Root+"/StockGachaSandbox.unity";
        const string Source="Assets/Project/Scenes/SC_ExchangeFurnished.unity";

        [MenuItem("Tools/Buy Bid Blow/StockGacha/Create and Open Sandbox")]
        public static void Build()
        {
            if(File.Exists(ScenePath))
            {
                if(!Application.isBatchMode)EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            if(!AssetDatabase.CopyAsset(Source,ScenePath))throw new Exception("Could not copy the production hall.");
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var roots=scene.GetRootGameObjects();
            var manager=roots.SelectMany(g=>g.GetComponentsInChildren<GameNetworkManager>(true)).Single();
            var config=new SerializedObject(manager);
            config.FindProperty("minPlayers").intValue=1;
            config.FindProperty("prepareSeconds").floatValue=5;
            config.FindProperty("matchSettingsFile").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/MatchSettings.json");
            config.FindProperty("stockGachaSandboxSettings").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/StockGachaSettings.json");
            config.FindProperty("stockGachaDevicePosition").vector3Value=new Vector3(0,0,5);
            config.ApplyModifiedPropertiesWithoutUndo();
            // Keep HallSession, its camera and the real spawned player. Replace only the lobby controls.
            foreach(var hud in roots.SelectMany(g=>g.GetComponentsInChildren<LobbyHud>(true)).ToArray())UnityEngine.Object.DestroyImmediate(hud);
            var session=roots.SelectMany(g=>g.GetComponentsInChildren<HallSession>(true)).Single();
            foreach(var canvas in session.lobbyPresentation.GetComponentsInChildren<Canvas>(true))canvas.gameObject.SetActive(false);

            var device=new GameObject("Stock Gacha Machine - 150 Capsules");device.transform.position=new Vector3(0,0,5);
            device.AddComponent<StockGachaDevice>();
            if(!AssetDatabase.IsValidFolder(Root+"/Materials"))AssetDatabase.CreateFolder(Root,"Materials");
            var teal=Material("Teal",new Color(.025f,.19f,.15f));
            var gold=Material("Gold",new Color(.85f,.59f,.18f));
            var cream=Material("Cream",new Color(.96f,.86f,.55f));
            Primitive(device.transform,"Pedestal",PrimitiveType.Cube,new Vector3(0,.65f,0),new Vector3(1.5f,1.3f,1),teal);
            Primitive(device.transform,"Gold Rim",PrimitiveType.Cube,new Vector3(0,1.33f,0),new Vector3(1.6f,.12f,1.1f),gold);
            Primitive(device.transform,"Drum",PrimitiveType.Sphere,new Vector3(0,1.93f,0),Vector3.one*.9f,gold);
            Primitive(device.transform,"Capsule Outlet",PrimitiveType.Cube,new Vector3(0,.85f,-.515f),new Vector3(.65f,.08f,.03f),gold);
            for(int i=0;i<7;i++)Primitive(device.transform,"Capsule Ball "+i,PrimitiveType.Sphere,
                new Vector3(Mathf.Sin(i*2.4f)*.3f,1.9f+Mathf.Cos(i*1.5f)*.3f,-.34f),Vector3.one*.14f,cream);
            var sign=new GameObject("StockGacha Sign");sign.transform.SetParent(device.transform,false);
            sign.transform.localPosition=new Vector3(0,2.7f,0);sign.transform.localRotation=Quaternion.Euler(0,180,0);
            var text=sign.AddComponent<TextMesh>();text.text="STOCK GACHA\n150 CAPSULES\n[E]";text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
            text.fontSize=56;text.characterSize=.004f;text.color=new Color(1,.86f,.4f);
            var ui=new GameObject("StockGacha Sandbox Controls").AddComponent<StockGachaSandboxPanel>();
            ui.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/Pretendard-Regular.otf");ui.device=device.transform;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("STOCK GACHA SANDBOX CREATED: "+ScenePath);
        }
        static Material Material(string name,Color color)
        {
            string path=Root+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=color;
            AssetDatabase.CreateAsset(material,path);return material;
        }
        static void Primitive(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}
