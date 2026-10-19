using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using BuyBidBlow.Characters;
using BuyBidBlow.Props;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallLevelVerification
    {
        static void Check(bool pass,string message){if(!pass)throw new Exception(message);}
        public static void BuildAndVerify() { HallLevelBuilder.Build(); RunBatch(); }
        public static void FinishMaterials()
        {
            var data=JsonUtility.FromJson<HallLevelBuilder.Manifest>(File.ReadAllText("Logs/HallImport/manifest.json"));
            foreach(var src in data.materials)
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(HallLevelBuilder.Root+"/Materials/"+src.name+".mat");
                if(!mat)continue;
                mat.SetColor("_BaseColor",new Color(src.color[0],src.color[1],src.color[2],src.color[3]).gamma);
                if(src.name.Contains("Frond") || src.name.Contains("Leaf")){mat.SetFloat("_SpecularHighlights",0);mat.SetFloat("_EnvironmentReflections",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");}
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();RenderPreview();
        }
        public static void RunBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
                var log=new StringBuilder();
                var doors=UnityEngine.Object.FindObjectsByType<Door>(FindObjectsSortMode.None);
                var seats=UnityEngine.Object.FindObjectsByType<Seat>(FindObjectsSortMode.None);
                var ropes=UnityEngine.Object.FindObjectsByType<RopeBarrier>(FindObjectsSortMode.None);
                var cloths=UnityEngine.Object.FindObjectsByType<Cloth>(FindObjectsSortMode.None);
                Check(doors.Length==6 && seats.Length==15 && ropes.Length==16 && cloths.Length==1,"Unexpected interactive object counts");
                foreach(var door in doors)
                {
                    var original=door.transform.localRotation;door.SetOpen(true);for(int i=0;i<100;i++)door.Simulate(.02f);
                    Check(Quaternion.Angle(original,door.transform.localRotation)>90,"Door did not open: "+door.name);
                    door.SetOpen(false);for(int i=0;i<100;i++)door.Simulate(.02f);
                    Check(Quaternion.Angle(original,door.transform.localRotation)<.01f,"Door did not close: "+door.name);
                    Check(PrefabUtility.IsPartOfPrefabInstance(door),"Door lost prefab link");
                }
                log.AppendLine("PASS: 6 hinged doors open and close, with linked prefabs.");
                Physics.SyncTransforms();
                int seatCount=0;
                foreach(var seat in seats)
                {
                    var actors=new PlayerSeating[seat.sittingPoints.Length];
                    for(int i=0;i<actors.Length;i++)
                    {
                        var go=new GameObject("SeatTestActor");go.transform.position=seat.exitPoints[i].position;
                        var body=go.AddComponent<Rigidbody>();body.useGravity=false;go.AddComponent<CapsuleCollider>();actors[i]=go.AddComponent<PlayerSeating>();
                        Check(seat.Interact(actors[i].gameObject) && actors[i].IsSeated,"Cannot sit: "+seat.name);
                        Check(body.isKinematic,"Seated actor physics not suspended");seatCount++;
                    }
                    var spare=new GameObject("SpareActor").AddComponent<PlayerSeating>();Check(!seat.Interact(spare.gameObject),"Occupied seat accepted extra actor");UnityEngine.Object.DestroyImmediate(spare.gameObject);
                    foreach(var actor in actors)
                    {
                        Physics.SyncTransforms();
                        if(!actor.TryStand()) {
                            var detail=string.Join("\n",seat.exitPoints.Select(e=>e.position+": "+string.Join(",",Physics.OverlapCapsule(e.position+Vector3.up*.35f,e.position+Vector3.up*1.5f,.27f).Select(c=>c.name+" "+c.bounds))));
                            throw new Exception("No safe exit: "+seat.name+"\n"+detail);
                        }
                        Check(!actor.IsSeated && !actor.GetComponent<Rigidbody>().isKinematic,"Actor failed to restore physics");
                        UnityEngine.Object.DestroyImmediate(actor.gameObject);
                    }
                }
                log.AppendLine("PASS: "+seatCount+" sitting places accept occupants, reject overbooking and allow safe standing.");
                foreach(var rope in ropes)
                {
                    rope.Initialize();for(int i=0;i<600;i++)rope.Simulate(.02f);
                    Check(rope.PinError<.0001f && rope.SegmentError<.015f,"Rope constraints failed: "+rope.name+" "+rope.SegmentError);
                    var mid=rope.Midpoint;Check(float.IsFinite(mid.sqrMagnitude),"Nonfinite rope");
                    var contact=new GameObject("RopeContact").AddComponent<CapsuleCollider>();contact.height=1.4f;contact.radius=.25f;contact.transform.position=mid+Vector3.forward*.1f;
                    rope.contacts=new[]{contact};for(int i=0;i<30;i++)rope.Simulate(.02f);
                    Check(Vector3.Distance(mid,rope.Midpoint)>.05f,"Rope did not react to contact");
                    rope.contacts=Array.Empty<CapsuleCollider>();UnityEngine.Object.DestroyImmediate(contact.gameObject);
                }
                log.AppendLine("PASS: 16 rope spans remain pinned after 12 seconds and respond to capsule contact.");
                foreach(var cloth in cloths)
                {
                    Check(cloth.coefficients.Count(c=>c.maxDistance==0)>=2,"Banner anchors missing");
                    Check(cloth.coefficients.Any(c=>c.maxDistance>.1f),"Banner cannot move");
                    Check(cloth.GetComponent<BannerWind>()!=null,"Banner wind missing");
                }
                log.AppendLine("PASS: banner Cloth has fixed anchors and movable fabric.");
                foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))Check(r.sharedMaterials.All(m=>m && m.shader),"Missing material: "+r.name);
                Check(Physics.Raycast(new Vector3(0,2,-5),Vector3.down,out var hit,5),"Lobby floor collision missing");
                log.AppendLine("PASS: materials resolved and lobby floor collision exists.");
                Directory.CreateDirectory("Docs/Arts/Environments/UnityVerification");File.WriteAllText("Docs/Arts/Environments/UnityVerification/Hall_Verification.txt",log.ToString());
                Debug.Log("HALL_VERIFY_OK\n"+log);EditorApplication.Exit(0);
            }
            catch(Exception e){File.WriteAllText("Logs/hall-verify-error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void RenderPreview()
        {
            try
            {
                EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
                var camera=Camera.main;camera.transform.SetParent(null);
                Capture(camera,new Vector3(0,7,1),new Vector3(0,1,-12),"Reception");
                Capture(camera,new Vector3(0,5,-4),new Vector3(0,5.5f,12),"Auction");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        internal static void Capture(Camera camera,Vector3 position,Vector3 target,string name)
        {
            ShaderUtil.allowAsyncCompilation=false;
            camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=70;
            var rt=new RenderTexture(1400,900,24);camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1400,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();
            Directory.CreateDirectory("Docs/Arts/Environments/UnityVerification");File.WriteAllBytes("Docs/Arts/Environments/UnityVerification/"+name+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
