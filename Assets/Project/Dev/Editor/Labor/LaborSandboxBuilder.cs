using System.IO;
using System.Linq;
using BuyBidBlow.Sandbox.Labor;
using BuyBidBlow.Network;
using BuyBidBlow.Sandbox.StockGacha;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad]
    public static class LaborSandboxBuilder
    {
        public const string ScenePath="Assets/Project/Dev/Sandbox/Labor/LaborSandbox.unity";
        static LaborSandboxBuilder(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(!File.Exists("Library/LaborBuild.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete("Library/LaborBuild.request");
            try{Build();File.WriteAllText("Library/LaborBuild.result","OK");}
            catch(System.Exception e){File.WriteAllText("Library/LaborBuild.result",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Tools/Buy Bid Blow/Labor/Create and Open Sandbox")]
        public static void Build()
        {
            if(File.Exists("Library/LaborRebase.request"))
            {
                File.Delete("Library/LaborRebase.request");
                if(File.Exists(ScenePath))File.Copy(ScenePath,"Library/LaborBeforeRepair.unity",true);
                File.Copy("Assets/Project/Dev/Sandbox/StockGacha/StockGachaSandbox.unity",ScenePath,true);
                AssetDatabase.ImportAsset(ScenePath,ImportAssetOptions.ForceUpdate);
                EditorSceneManager.OpenScene(ScenePath);
            }
            var scene=SceneManager.GetActiveScene();
            if(scene.name=="LaborSandbox"&&scene.path!=ScenePath)
                EditorSceneManager.SaveScene(scene,ScenePath);
            if(scene.path!=ScenePath)
            {
                // Preserve an unsaved working copy instead of discarding the user's scene edits.
                if(scene.isDirty)EditorSceneManager.SaveScene(scene,"Assets/Project/Dev/Sandbox/Labor/BeforeLaborSetup.unity",true);
                if(!File.Exists(ScenePath))AssetDatabase.CopyAsset("Assets/Project/Dev/Sandbox/StockGacha/StockGachaSandbox.unity",ScenePath);
                scene=EditorSceneManager.OpenScene(ScenePath);
            }
            var manager=Object.FindFirstObjectByType<GameNetworkManager>();
            var config=new SerializedObject(manager);
            config.FindProperty("minPlayers").intValue=1;
            config.FindProperty("prepareSeconds").floatValue=5;
            config.FindProperty("matchSettingsFile").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Project/Dev/Sandbox/StockGacha/MatchSettings.json");
            config.FindProperty("stockGachaSandboxSettings").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Project/Dev/Sandbox/StockGacha/StockGachaSettings.json");
            config.ApplyModifiedPropertiesWithoutUndo();
            if(!Object.FindFirstObjectByType<StockGachaSandboxPanel>())
            {
                var panel=new GameObject("Sandbox Host Controls").AddComponent<StockGachaSandboxPanel>();
                panel.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/Pretendard-Regular.otf");
            }
            if(!Object.FindFirstObjectByType<LaborRoom>())
            {
                var room=new GameObject("Labor Workshop and Door Portal").AddComponent<LaborRoom>();
                room.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/Pretendard-Regular.otf");
                var doors=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true))
                    .Where(t=>t.name=="Entry_Left_Door_Pivot_004"||t.name=="Entry_Right_Door_Pivot_004").ToArray();
                if(doors.Length<2)throw new System.Exception("Lobby door pair missing.");
                var bounds=new Bounds(doors[0].position,Vector3.zero);
                foreach(var door in doors)foreach(var renderer in door.GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);
                room.doorPosition=new Vector3(bounds.center.x,bounds.min.y+1.75f,bounds.center.z);
                // The hall center is the origin in the furnished scene. Exit on its side of the doorway.
                var towardHall=(new Vector3(0,0,0)-new Vector3(bounds.center.x,0,bounds.center.z)).normalized;
                room.hallExit=new Vector3(bounds.center.x,bounds.min.y+.2f,bounds.center.z)+towardHall*3;
                File.WriteAllText("Library/LaborDoor.txt",string.Join("\n",doors.Select(d=>d.name+" "+d.position))+"\nBounds "+bounds+"\nPortal "+room.doorPosition+"\nExit "+room.hallExit);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
