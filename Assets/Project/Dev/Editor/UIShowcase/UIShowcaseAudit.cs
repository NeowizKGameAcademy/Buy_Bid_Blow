using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad] public static class UIShowcaseAudit
    {
        static UIShowcaseAudit(){EditorApplication.update+=()=>{if(!File.Exists("Library/UIShowcaseAudit.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;File.Delete("Library/UIShowcaseAudit.request");try{Run();}catch(Exception e){File.WriteAllText("Library/UIShowcaseAudit.result",e.ToString());Debug.LogException(e);}};}
        [MenuItem("Tools/Buy Bid Blow/UI/Audit UI Showcase")]
        public static void Run()
        {
            var report=new List<string>();var issues=new List<string>();
            var prefabPaths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Project/UI"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("/Shared/")||p.Contains("/Connection/")||p.Contains("/Notifications/")||p.Contains("/Pause/")||p.Contains("/Settings/")||p.Contains("/Loading/")||p.Contains("/Result/")||p.Contains("/Round/")).ToArray();
            foreach(var path in prefabPaths)
            {
                foreach(var dep in AssetDatabase.GetDependencies(path,true))if(dep.StartsWith("Assets/Project/Dev/"))issues.Add("Dev dependency: "+path+" -> "+dep);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);foreach(var tr in prefab.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject)>0)issues.Add("Missing script: "+path);
            }
            report.Add("Prefabs audited: "+prefabPaths.Length);report.Add("Runtime prefabs have no Dev dependencies/missing scripts: "+(issues.Count==0));
            var scene=EditorSceneManager.OpenScene(UIShowcaseBuilder.ScenePath,OpenSceneMode.Additive);
            try
            {
                var demo=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UIShowcase>()).Single();
                var canvas=demo.GetComponent<Canvas>();var cam=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
                string folder=UIShowcaseBuilder.Root+"/AuditPreviews";Directory.CreateDirectory(folder);
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768),new Vector2Int(1280,800)})
                {
                    var rt=new RenderTexture(size.x,size.y,24);var tex=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);var previous=RenderTexture.active;cam.targetTexture=rt;cam.aspect=(float)size.x/size.y;
                    try{for(int i=0;i<demo.Count;i++)
                    {
                        demo.ShowCase(i);Canvas.ForceUpdateCanvases();cam.Render();
                        foreach(var t in demo.GetComponentsInChildren<Text>())
                        {
                            if(t.resizeTextForBestFit)continue;
                            if(t.preferredHeight>t.rectTransform.rect.height+3)issues.Add($"Text height: {size} case {i+1} {t.name} {t.preferredHeight:F0}>{t.rectTransform.rect.height:F0}");
                        }
                        if((size.x==1366||size.x==1280)&&(i==5||i==10||i==18)){RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,size.x,size.y),0,0);tex.Apply();File.WriteAllBytes(folder+"/"+size.x+"x"+size.y+"_"+(i+1)+".png",tex.EncodeToPNG());}
                    }}finally{cam.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
                    report.Add("Rendered all 21 states: "+size.x+"x"+size.y);
                }
            }finally{EditorSceneManager.CloseScene(scene,true);AssetDatabase.Refresh();}
            report.AddRange(issues.Distinct());report.Insert(0,issues.Count==0?"PASS":"REVIEW");File.WriteAllLines(UIShowcaseBuilder.Root+"/LayoutAudit.txt",report);File.WriteAllText("Library/UIShowcaseAudit.result",issues.Count==0?"PASS":string.Join("\n",issues.Distinct()));
        }
    }
}
