using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BuyBidBlow.Dev.RopeBarrier;

namespace BuyBidBlow.Dev.Editor
{
    public static class RopeBarrierTestBuilder
    {
        const string Output="Docs/Arts/Props/Models/RopeBarriers";
        [MenuItem("BuyBidBlow/Rope Barrier/Build Test Scene")]
        public static void Build()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            AssetDatabase.Refresh();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            for(int i=0;i<2;i++) {
                string kind=i==0?"A":"B",path="Assets/Project/Content/Props/RopeBarriers/RopeBarrier_"+kind+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.name="Barrier_"+kind+"_Test";go.transform.position=new Vector3(0,0,i*2.5f);
                foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;skin.localBounds=new Bounds(Vector3.zero,Vector3.one*10);}
                go.AddComponent<RopeBarrierDemo>();
            }
            var cam=new GameObject("Test Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(4,3.8f,-7);cam.transform.LookAt(new Vector3(0,.6f,1.2f));cam.backgroundColor=new Color(.23f,.25f,.27f);cam.clearFlags=CameraClearFlags.SolidColor;
            var light=new GameObject("Preview Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(45,-30,0);
            RenderSettings.ambientLight=Color.gray;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Project/Dev/Sandbox/RopeBarrier/SC_RopeBarrierTest.unity");
        }
        public static void RunBatch()
        {
            try {
                Build();var sb=new StringBuilder("Unity "+Application.unityVersion+" — deterministic runtime solver tests, 600 x 0.02s per case\n");
                foreach(var demo in UnityEngine.Object.FindObjectsByType<RopeBarrierDemo>(FindObjectsSortMode.None)) {
                    demo.Initialize();
                    foreach(var pair in new[]{new Vector2(1.2f,1.4f),new Vector2(1.6f,1.65f),new Vector2(2.4f,2.8f),new Vector2(2.4f,1.2f)}) {
                        demo.postDistance=pair.x;demo.ropeLength=pair.y;
                        for(int i=0;i<600;i++)demo.Step(.02f);
                        if(demo.MaxPinError>.001f || demo.MaxSegmentError>.02f || demo.Sag>2)throw new Exception("Constraint verification failed");
                        sb.AppendLine($"PASS {demo.name}: spacing={pair.x:F2}, requested length={pair.y:F2}, effective={demo.EffectiveLength:F3}, pin error={demo.MaxPinError:F6}, segment error={demo.MaxSegmentError:F6}, sag={demo.Sag:F3}");
                    }
                    demo.postDistance=1.6f;demo.ropeLength=1.65f;for(int i=0;i<300;i++)demo.Step(.02f);
                }
                var camera=Camera.main;var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Output+"/Unity_Rope_Test.png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;
                File.WriteAllText(Output+"/Unity_Verification.txt",sb.ToString());EditorApplication.Exit(0);
            } catch(Exception e){File.WriteAllText(Output+"/Unity_Verification.txt","FAIL\n"+e);Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
