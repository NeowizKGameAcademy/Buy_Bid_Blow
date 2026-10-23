using System;
using System.IO;
using System.Linq;
using BuyBidBlow.UI.Ticker;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallTickerSpacing
    {
        const string SourcePath="Docs/Arts/Props/TickerBoard/PlacementSource.json";
        const string LayoutPath="Docs/Arts/Props/TickerBoard/DisplayLayout.json";
        const string HallPath="Assets/Project/Content/Levels/ExchangeFurnished/AuctionHall_Furnished.prefab";
        static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
        static float[] A(Vector3 v)=>new[]{v.x,v.y,v.z};

        // Fixed target: 8% closer than the original layout. Safe to run again.
        public static void Apply()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            string original=File.ReadAllText(SourcePath);
            var source=JsonUtility.FromJson<HallTickerBoardSetup.Source>(original);
            var scene=EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            var root=scene.GetRootGameObjects().Single(g=>g.name=="AuctionHall_Furnished").transform.Find("TickerBoards");
            Capture(root,"Spacing_Before.png");
            foreach(var p in source.placements.Where(p=>p.name!="TickerBoard_Center"))
            {
                var oldPosition=V(p.position);var oldRotation=Quaternion.LookRotation(V(p.forward),V(p.up));
                var mount=V(p.mountPosition);float depth=p.mountSize[2];
                var wallPoint=oldPosition+oldRotation*(mount-Vector3.forward*depth*.5f);
                var wallNormal=Vector3.ProjectOnPlane(V(p.forward),Vector3.up).normalized;
                var position=new Vector3(Mathf.Sign(oldPosition.x)*9.542999f,oldPosition.y,-16.88644f);
                var toward=new Vector3(-position.x,0,-8.663037f-position.z).normalized;
                var rotation=Quaternion.LookRotation(toward,Vector3.up)*Quaternion.Euler(7.5f,0,0);
                // Keep the front of the rear support attached to the TV, and its far end at the wall plane.
                float front=mount.z+depth*.5f;
                var frontWorld=position+rotation*new Vector3(mount.x,mount.y,front);
                float newDepth=Vector3.Dot(frontWorld-wallPoint,wallNormal)/Vector3.Dot(rotation*Vector3.forward,wallNormal);
                if(newDepth<=0)throw new Exception("Invalid ticker wall support depth.");
                mount.z=front-newDepth*.5f;
                p.position=A(position);p.forward=A(rotation*Vector3.forward);p.up=A(rotation*Vector3.up);
                p.mountPosition=A(mount);p.mountSize[2]=newDepth;
            }
            ApplyTo(root,source);Capture(root,"Spacing_After.png");EditorSceneManager.SaveScene(scene);
            var hall=PrefabUtility.LoadPrefabContents(HallPath);
            try{ApplyTo(hall.transform.Find("TickerBoards"),source);PrefabUtility.SaveAsPrefabAsset(hall,HallPath);}
            finally{PrefabUtility.UnloadPrefabContents(hall);}
            // Preserve the original extraction geometry for import verification.
            source.source="Authored TV layout";source.mountTriangleCenters=null;
            File.WriteAllText(LayoutPath,JsonUtility.ToJson(source,true));AssetDatabase.SaveAssets();
            Debug.Log("[Ticker spacing] Center unchanged; sides moved 0.925 m inward toward center, yaw +/-49.248 degrees, downward tilt 7.5 degrees. Scene, hall prefab and saved layout updated.");
        }
        public static void ApplySavedLayout(Transform root)
        {
            if(File.Exists(LayoutPath))ApplyTo(root,JsonUtility.FromJson<HallTickerBoardSetup.Source>(File.ReadAllText(LayoutPath)));
        }
        static void ApplyTo(Transform root,HallTickerBoardSetup.Source source)
        {
            var center=root.Find("TickerBoard_Center");var centerPosition=center.localPosition;var centerRotation=center.localRotation;
            foreach(var p in source.placements.Where(p=>p.name!="TickerBoard_Center"))
            {
                var board=root.Find(p.name);board.localPosition=V(p.position);board.localRotation=Quaternion.LookRotation(V(p.forward),V(p.up));
                PrefabUtility.RecordPrefabInstancePropertyModifications(board);
                var mount=board.Find("WallMountExtension");mount.localPosition=V(p.mountPosition);
                mount.localScale=new Vector3(1,1,p.mountSize[2]/source.placements[0].mountSize[2]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(mount);
            }
            if(center.localPosition!=centerPosition||center.localRotation!=centerRotation)throw new Exception("Center TV moved.");
        }
        static void Capture(Transform root,string file)
        {
            foreach(var display in root.GetComponentsInChildren<TickerBoardDisplay>())display.PresentSnapshot(new[]{18,40,42,22,36,48,21,37,46,24,35,46},30,0);
            var camera=new GameObject("Ticker spacing preview").AddComponent<Camera>();camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camera.transform.position=root.TransformPoint(new Vector3(0,5,-2));camera.transform.LookAt(root.TransformPoint(new Vector3(0,8.1f,-19)));
            camera.fieldOfView=58;camera.aspect=16f/9;camera.nearClipPlane=.1f;
            var rt=new RenderTexture(1920,1080,24);var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes("Docs/Arts/Props/TickerBoard/"+file,tex.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(camera.gameObject);}
            foreach(var display in root.GetComponentsInChildren<TickerBoardDisplay>())display.ShowWaiting();
        }
    }
}
