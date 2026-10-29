using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallPASpeakers
    {
        const string Root = "Assets/Project/Content/Props/PASpeaker";
        const string Group = "PA_Speakers_Approved";
        const string PrefabPath = Root + "/Prefabs/PF_PA_Speaker.prefab";
        const string Report = "Docs/Arts/Environments/UnityVerification/PA_Speakers";
        [Serializable] public class Part { public string file, name; public bool dark; }
        [Serializable] public class Placement { public string name; public float[] position, forward, up; }
        [Serializable] public class Layout { public Part[] parts; public Placement[] placements; }
        static Vector3 V(float[] p) => new Vector3(p[0], p[1], p[2]);
        static T Save<T>(T obj, string path) where T : UnityEngine.Object
        {
            var old = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!old) { AssetDatabase.CreateAsset(obj, path); return obj; }
            EditorUtility.CopySerialized(obj, old); UnityEngine.Object.DestroyImmediate(obj); return old;
        }
        static Mesh ReadMesh(string path)
        {
            using var reader = new BinaryReader(File.OpenRead(path));
            int count = reader.ReadInt32(), indices = reader.ReadInt32();
            var vertices = new Vector3[count]; var normals = new Vector3[count]; var uv = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                uv[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }
            var triangles = new int[indices]; for (int i = 0; i < indices; i++) triangles[i] = reader.ReadInt32();
            var mesh = new Mesh { name = Path.GetFileNameWithoutExtension(path), indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateBounds(); return mesh;
        }
        static Material Material(string name, Color linear)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new Exception("URP Lit shader missing");
            var mat = new Material(shader) { name = name };
            mat.SetColor("_BaseColor", linear.gamma); mat.SetFloat("_Smoothness", .17f); mat.SetFloat("_Metallic", 0);
            return Save(mat, Root + "/Materials/" + name + ".mat");
        }
        static void Apply()
        {
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Root + "/Source/layout.json"));
            if (layout.placements.Length != 7) throw new Exception("Expected seven approved positions");
            foreach (var sub in new[] { "Meshes", "Materials", "Prefabs" }) Directory.CreateDirectory(Root + "/" + sub);
            AssetDatabase.Refresh();
            var paint = Material("PA_Matte_GrayGreen", new Color(.05f,.066f,.049f,1));
            var dark = Material("PA_Inner_Shadow", new Color(.009f,.013f,.010f,1));
            var source = new GameObject("PF_PA_Speaker");
            try
            {
                foreach (var part in layout.parts)
                {
                    var child = new GameObject(part.name); child.transform.SetParent(source.transform, false);
                    child.AddComponent<MeshFilter>().sharedMesh = Save(ReadMesh(Root + "/Source/" + part.file), Root + "/Meshes/" + part.name + ".asset");
                    child.AddComponent<MeshRenderer>().sharedMaterial = part.dark ? dark : paint;
                }
                var collider = source.AddComponent<BoxCollider>(); collider.center = new Vector3(0,0,.00825f); collider.size = new Vector3(.375f,.51f,.1815f);
                PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
            var scene = EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            var audioCount = UnityEngine.Object.FindObjectsByType<BuyBidBlow.Audio.HallSpeaker>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            var existing = GameObject.Find(Group); if (existing) UnityEngine.Object.DestroyImmediate(existing);
            var group = new GameObject(Group); var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            foreach (var p in layout.placements)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform); instance.name = p.name;
                instance.transform.localPosition = V(p.position); instance.transform.localRotation = Quaternion.LookRotation(V(p.forward), V(p.up));
            }
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            group = GameObject.Find(Group);
            if (!group || group.transform.childCount != 7) throw new Exception("Saved placement count mismatch");
            foreach (Transform child in group.transform)
            {
                var expected = layout.placements.Single(p => p.name == child.name);
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) != PrefabPath) throw new Exception("Disconnected prefab: " + child.name);
                if (Vector3.Distance(child.position,V(expected.position)) > .0001f || Quaternion.Angle(child.rotation,Quaternion.LookRotation(V(expected.forward),V(expected.up))) > .01f) throw new Exception("Placement mismatch: " + child.name);
                if (child.GetComponentsInChildren<MeshRenderer>().Any(r => !r.sharedMaterial || !r.sharedMaterial.shader)) throw new Exception("Missing material");
            }
            if (UnityEngine.Object.FindObjectsByType<BuyBidBlow.Audio.HallSpeaker>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != audioCount) throw new Exception("Audio controller count changed");
            Directory.CreateDirectory(Report);
            File.WriteAllText(Report + "/Validation.txt", "PASS: seven saved, linked prefab instances. Position and rotation match approved Blender layout. 1.5x size baked into shared mesh; instance scales are 1. Materials resolved. Existing audio controllers preserved. Rear information speaker excluded.\n" + string.Join("\n",layout.placements.Select(p=>p.name+" "+V(p.position))));
        }
        static void Capture(string name, Vector3 eye, Vector3 target)
        {
            var go = new GameObject("PA Preview"); var camera = go.AddComponent<Camera>();
            var rt = new RenderTexture(1440,1000,24); var image = new Texture2D(1440,1000,TextureFormat.RGB24,false); var old=RenderTexture.active;
            try
            {
                camera.transform.position=eye; camera.transform.LookAt(target); camera.fieldOfView=53; camera.nearClipPlane=.1f;
                camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; image.ReadPixels(new Rect(0,0,1440,1000),0,0); image.Apply(); File.WriteAllBytes(Report+"/"+name+".png",image.EncodeToPNG());
            }
            finally { RenderTexture.active=old; camera.targetTexture=null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(go); }
        }
        [MenuItem("BuyBidBlow/Hall/Apply Approved PA Speakers")]
        static void Menu() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Apply(); }
        public static void RunBatch()
        {
            try
            {
                Apply();
                Capture("Unity_Upper_Speaker",new Vector3(7,5.3f,-7),new Vector3(13.86f,7.55f,-11));
                Capture("Unity_Lounge_Speaker",new Vector3(-5.5f,1.7f,7.1f),new Vector3(-12.64f,1.7f,10.55f));
                Debug.Log("PA_SPEAKERS_IMPORT_OK"); EditorApplication.Exit(0);
            }
            catch(Exception e) { Directory.CreateDirectory(Report); File.WriteAllText(Report+"/Error.txt",e.ToString()); Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
