using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BuyBidBlow.Props;
using BuyBidBlow.Characters;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    // Collision is authored before renderer batching destroys furniture hierarchy.
    public static class HallFurnitureCollision
    {
        const string Root = HallLevelBuilder.Root + "/Collision";
        public const string PrefabPath = Root + "/HallFurnitureCollision.prefab";
        const string GroupName = "HallFurnitureCollision";
        const string Docs = "Docs/Arts/Environments/Collision";
        [Serializable] public class Sample { public string source; public float[] point, normal; }
        [Serializable] public class Group { public string name; public string[] sources; public float[] vertices; public int[] triangles; public Sample[] samples; }
        [Serializable] public class Data { public string source; public Group[] groups; }
        static Vector3 V(float[] a) => new Vector3(a[0], a[1], a[2]);
        static Data Read() => JsonUtility.FromJson<Data>(File.ReadAllText(Docs + "/FurnitureCollision.json"));
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }

        public static void Attach(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab) throw new Exception("Furniture collision prefab missing. Run HallFurnitureCollision.Apply first.");
            var old = parent.Find(GroupName);
            if (old) Object.DestroyImmediate(old.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = GroupName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        // The pot's mesh, rather than the decorative leaves, defines the solid volume.
        public static void FitPlanter(GameObject go)
        {
            var mesh = go.GetComponentsInChildren<MeshFilter>().Single(m => m.name.Contains("Brushed_Brass"));
            var bounds = mesh.sharedMesh.bounds;
            var collider = go.GetComponent<CapsuleCollider>() ?? go.AddComponent<CapsuleCollider>();
            collider.center = bounds.center;
            collider.radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            collider.height = Mathf.Max(bounds.size.y, collider.radius * 2);
            collider.direction = 1;
        }

        static void BuildPrefab(Data data)
        {
            Directory.CreateDirectory(Root + "/Meshes"); AssetDatabase.Refresh();
            var root = new GameObject(GroupName);
            try
            {
                foreach (var group in data.groups)
                {
                    var points = new Vector3[group.vertices.Length / 3];
                    for (int i=0; i<points.Length; i++) points[i] = new Vector3(group.vertices[i*3],group.vertices[i*3+1],group.vertices[i*3+2]);
                    var bounds = new Bounds(points[0], Vector3.zero);
                    foreach (var point in points) bounds.Encapsulate(point);
                    for (int i=0; i<points.Length; i++) points[i] -= bounds.center;
                    var mesh = new Mesh { name = group.name + "_Collision", vertices = points, triangles = group.triangles };
                    mesh.RecalculateBounds();
                    string path = Root + "/Meshes/" + mesh.name + ".asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (existing) { EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh); mesh=existing; }
                    else AssetDatabase.CreateAsset(mesh,path);
                    var go = new GameObject(group.name) { layer = 6 };
                    go.transform.SetParent(root.transform,false); go.transform.localPosition = bounds.center;
                    go.AddComponent<MeshCollider>().sharedMesh = mesh;
                }
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            const string planterPath = HallLevelBuilder.Root + "/LoungeClusters/Prefabs/LoungePlanter.prefab";
            var planter = PrefabUtility.LoadPrefabContents(planterPath);
            try { FitPlanter(planter); PrefabUtility.SaveAsPrefabAsset(planter,planterPath); }
            finally { PrefabUtility.UnloadPrefabContents(planter); }
        }

        static string Coverage(Data data)
        {
            Physics.SyncTransforms(); var log = new StringBuilder();
            foreach (var g in data.groups)
            {
                int covered = g.samples.Count(s => Physics.OverlapSphere(V(s.point),.055f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Length>0);
                log.AppendLine(g.name + ": " + covered + "/" + g.samples.Length + " solid surface samples covered");
            }
            foreach(var child in GameObject.Find("UnderMezzanine_Lounge_Clusters").GetComponentsInChildren<Collider>())
                log.AppendLine("Existing lounge collider: " + child.name + " " + child.GetType().Name + " enabled=" + child.enabled + " trigger=" + child.isTrigger + " size=" + child.bounds.size);
            return log.ToString();
        }

        static string[] BlockedSeats()
        {
            return Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).Where(seat => !seat.exitPoints.Any(exit =>
            {
                if (!exit || !Physics.Raycast(exit.position + Vector3.up*.5f,Vector3.down,out var hit,1.1f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore) || hit.normal.y<.65f) return false;
                var p=hit.point+Vector3.up*.02f;
                return !Physics.CheckCapsule(p+Vector3.up*.35f,p+Vector3.up*1.5f,.27f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            })).Select(s=>s.name).ToArray();
        }

        // PhysX simulation with the actual player capsule dimensions and walking speed.
        // No teleports or trigger-only probes: the body is pushed into each counter.
        static string PushCounters(Data data, bool expectBlocking)
        {
            var log=new StringBuilder(); var prior=Physics.simulationMode;
            var bodyObject=new GameObject("CollisionVerificationPlayer");
            var body=bodyObject.AddComponent<Rigidbody>(); body.useGravity=false;
            body.constraints=RigidbodyConstraints.FreezeRotation|RigidbodyConstraints.FreezePositionY;
            var capsule=bodyObject.AddComponent<CapsuleCollider>(); capsule.height=1.8f; capsule.radius=.3f; capsule.center=Vector3.up*.9f;
            var colliderList=Object.FindObjectsByType<PlayerMove>(FindObjectsSortMode.None).SelectMany(p=>p.GetComponentsInChildren<Collider>()).Where(c=>c.enabled).ToArray();
            foreach(var c in colliderList)c.enabled=false;
            try
            {
                Physics.simulationMode=SimulationMode.Script;
                foreach(var group in data.groups.Where(g=>g.name.StartsWith("Desk_")))
                {
                    var samples=group.samples.Where(s=>s.source.EndsWith("FrontCore") && Mathf.Abs(V(s.normal).y)<.2f).ToArray();
                    Require(samples.Length>=2,"Counter has no side samples: "+group.name);
                    foreach(var sample in samples)
                    {
                        var normal=V(sample.normal).normalized; var surface=V(sample.point);
                        var start=surface+normal*2; start.y=.02f;
                        body.position=start; body.linearVelocity=Vector3.zero; Physics.SyncTransforms();
                        for(int step=0;step<45;step++) { body.linearVelocity=-normal*5; Physics.Simulate(.02f); }
                        float distance=Vector3.Dot(body.position-surface,normal);
                        bool stopped=distance>.05f;
                        log.AppendLine(group.name+" "+sample.source+" normal="+normal+" remaining="+distance.ToString("F3")+" blocked="+stopped);
                        if(expectBlocking)Require(stopped,"Player passed through counter: "+group.name);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(bodyObject); foreach(var c in colliderList)if(c)c.enabled=true;
                Physics.simulationMode=prior; Physics.SyncTransforms();
            }
            return log.ToString();
        }

        [MenuItem("BuyBidBlow/Hall/Repair Furniture Collision")]
        public static void Apply()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var data=Read(); var scene=EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            Directory.CreateDirectory("Logs/A94Collision");
            string backup="Logs/A94Collision/SC_ExchangeFurnished_before_collision.unity";
            if(!File.Exists(backup))File.Copy(HallLevelBuilder.ScenePath,backup);
            Physics.SyncTransforms();
            File.WriteAllText(Docs+"/Before.txt",Coverage(data)+"\nCounter movement reproduction:\n"+PushCounters(data,false));
            // Simulation is only a probe; reload to discard any dynamic rope/player state.
            scene=EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath); Physics.SyncTransforms();
            var blockedBefore=BlockedSeats();
            var renderers=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var lightmap=LightmapSettings.lightmaps[0].lightmapColor;
            BuildPrefab(data); Attach(GameObject.Find("AuctionHall_Furnished").transform);
            var hallPrefab=PrefabUtility.LoadPrefabContents(HallLevelBuilder.Root+"/AuctionHall_Furnished.prefab");
            try { Attach(hallPrefab.transform); PrefabUtility.SaveAsPrefabAsset(hallPrefab,HallLevelBuilder.Root+"/AuctionHall_Furnished.prefab"); }
            finally { PrefabUtility.UnloadPrefabContents(hallPrefab); }
            Physics.SyncTransforms();
            Require(Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Length==renderers.Length,"Visual geometry changed");
            Require(LightmapSettings.lightmaps[0].lightmapColor==lightmap,"Baked lighting changed");
            var blockedAfter=BlockedSeats();
            Require(!blockedAfter.Except(blockedBefore).Any(),"New collision obstructed seats: "+string.Join(",",blockedAfter.Except(blockedBefore)));
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Docs+"/After.txt",Coverage(data)+"\nPre-existing blocked seat exits: "+string.Join(",",blockedBefore)+"\n");
        }

        public static void RunBatch()
        {
            try { Apply(); Verify(); EditorApplication.Exit(0); }
            catch(Exception e) { Debug.LogException(e); File.WriteAllText("Logs/A94Collision/error.txt",e.ToString()); EditorApplication.Exit(1); }
        }
        public static void VerifyBatch()
        {
            try { Verify(); EditorApplication.Exit(0); }
            catch(Exception e) { Debug.LogException(e); File.WriteAllText("Logs/A94Collision/error.txt",e.ToString()); EditorApplication.Exit(1); }
        }
        static void Verify()
        {
            EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath); Physics.SyncTransforms();
            var data=Read(); var log=new StringBuilder();
            var root=GameObject.Find("AuctionHall_Furnished").transform.Find(GroupName);
            Require(root && root.childCount==data.groups.Length,"Missing collision groups");
            foreach(var group in data.groups)
            {
                var collider=root.Find(group.name).GetComponent<MeshCollider>();
                Require(collider.enabled&&!collider.isTrigger&&collider.sharedMesh,"Invalid collider "+group.name);
                int hits=0;
                foreach(var sample in group.samples)
                {
                    var ray=new Ray(V(sample.point)+V(sample.normal)*.12f,-V(sample.normal));
                    if(collider.Raycast(ray,out _, .24f))hits++;
                }
                Require(hits==group.samples.Length,"Missing own surface coverage: "+group.name+" "+hits+"/"+group.samples.Length);
                log.AppendLine("PASS: "+group.name+" "+hits+" own surface raycasts; "+collider.sharedMesh.triangles.Length/3+" triangles");
            }
            log.AppendLine(PushCounters(data,true));
            EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath); Physics.SyncTransforms();
            // Matchmaking stays clear at ground level. Upper floor objects do not count.
            var centerHits=Physics.OverlapBox(new Vector3(0,.95f,9.6f),new Vector3(5.85f,.85f,3.95f),Quaternion.identity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            Require(centerHits.Length==0,"Matching floor obstruction: "+string.Join(",",centerHits.Select(c=>c.name)));
            int slots=0; var blocked=BlockedSeats();
            foreach(var seat in Object.FindObjectsByType<Seat>(FindObjectsSortMode.None))
            {
                if(blocked.Contains(seat.name))continue;
                var actors=new System.Collections.Generic.List<PlayerSeating>();
                try
                {
                    for(int i=0;i<seat.sittingPoints.Length;i++)
                    {
                        var actor=new GameObject("SeatVerificationPlayer"); var c=actor.AddComponent<CapsuleCollider>(); c.height=1.8f;c.radius=.3f;c.center=Vector3.up*.9f;
                        actor.AddComponent<Rigidbody>().useGravity=false; var seating=actor.AddComponent<PlayerSeating>(); actors.Add(seating);
                        Require(seat.Interact(actor),"Seat interaction failed: "+seat.name); slots++;
                    }
                    foreach(var actor in actors)
                    {
                        Physics.SyncTransforms(); Require(actor.TryStand(),"Seat exit failed: "+seat.name);
                        Object.DestroyImmediate(actor.gameObject);
                    }
                }
                finally { foreach(var actor in actors)if(actor)Object.DestroyImmediate(actor.gameObject); }
            }
            log.AppendLine("PASS: protected matching floor remains clear; "+slots+" seat slots accept occupants and allow standing.");
            log.AppendLine("Existing blocked seats requiring separate review: "+string.Join(",",blocked));
            log.AppendLine("Colliders: "+Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length+"; existing doors="+Object.FindObjectsByType<Door>(FindObjectsSortMode.None).Length);
            File.WriteAllText(Docs+"/Verification.txt",log.ToString()); Debug.Log("A94_COLLISION_VERIFIED");
        }
    }
}
