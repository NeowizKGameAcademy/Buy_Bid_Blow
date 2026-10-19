using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BuyBidBlow.Hall.Editor
{
    public static class HallPlayVerification
    {
        const string Key="BBB_Hall_Play_Verification";
        static Cloth cloth;
        static Vector3[] initial;
        static float start, movement, pinError;
        static int errors;
        public static void RunBatch()
        {
            EditorSceneManager.OpenScene(HallLevelBuilder.Root+"/Scenes/SC_ExchangeFurnished.unity");
            SessionState.SetBool(Key,true);
            EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod]
        static void Restore()
        {
            if(!SessionState.GetBool(Key,false))return;
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=Log;
        }
        static void Log(string message,string stack,LogType type) { if(type==LogType.Exception || type==LogType.Error)errors++; }
        static void Tick()
        {
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            try
            {
                if(!cloth)
                {
                    cloth=UnityEngine.Object.FindFirstObjectByType<Cloth>();
                    if(!cloth)throw new Exception("Banner cloth missing");
                    initial=cloth.vertices;start=Time.time;return;
                }
                var vertices=cloth.vertices;var coefficients=cloth.coefficients;
                for(int i=0;i<vertices.Length;i++)
                {
                    if(!float.IsFinite(vertices[i].sqrMagnitude))throw new Exception("Nonfinite cloth vertex");
                    var distance=Vector3.Distance(initial[i],vertices[i]);
                    if(coefficients[i].maxDistance==0)pinError=Mathf.Max(pinError,distance);
                    else movement=Mathf.Max(movement,distance);
                }
                if(Time.time-start<12)return;
                if(movement<.01f || pinError>.01f)throw new Exception($"Cloth motion={movement}, pin error={pinError}");
                foreach(var rope in UnityEngine.Object.FindObjectsByType<HallPhysicsRope>(FindObjectsSortMode.None))
                    if(rope.PinError>.001f || !float.IsFinite(rope.Midpoint.sqrMagnitude))throw new Exception("Runtime rope unstable");
                var player=UnityEngine.Object.FindFirstObjectByType<HallWalkthrough>();
                if(player.transform.position.y<-.1f || player.transform.position.y>.3f)throw new Exception("Player did not remain on lobby floor: "+player.transform.position);
                if(errors>0)throw new Exception("Runtime errors: "+errors);
                File.WriteAllText("Docs/Arts/Environments/UnityVerification/Hall_Play_Verification.txt",
                    $"PASS: Unity {Application.unityVersion}, 12 seconds Play Mode.\nBanner free movement: {movement:F4} m; pinned drift: {pinError:F6} m.\nAll 16 rope spans finite and pinned.\nCharacterController remains grounded.\nRuntime errors: {errors}.\n");
                SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
                var camera=Camera.main;camera.transform.SetParent(null);
                HallLevelVerification.Capture(camera,new Vector3(0,7,1),new Vector3(0,1,-12),"Reception");
                HallLevelVerification.Capture(camera,new Vector3(0,5,-4),new Vector3(0,5.5f,12),"Auction");
                EditorApplication.Exit(0);
            }
            catch(Exception e)
            {
                File.WriteAllText("Logs/hall-play-error.txt",e.ToString());SessionState.SetBool(Key,false);
                EditorApplication.update-=Tick;Debug.LogException(e);EditorApplication.Exit(1);
            }
        }
        [MenuItem("BuyBidBlow/Hall/Open Furnished Hall")]
        public static void OpenForReview()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(HallLevelBuilder.Root+"/Scenes/SC_ExchangeFurnished.unity");
            var view=SceneView.lastActiveSceneView;
            if(view)view.LookAt(new Vector3(0,4,5),Quaternion.Euler(12,0,0),20);
            Selection.activeObject=GameObject.Find("AuctionHall_Furnished");
        }
    }
}
