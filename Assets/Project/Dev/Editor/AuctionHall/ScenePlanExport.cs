using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BuyBidBlow.Dev
{
    public static class ScenePlanExport
    {
        public static void Export()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Project/Scenes/SC_ExchangeFurnished.unity", OpenSceneMode.Single);
            const string folder = "Docs/Arts/Environments/ScenePlan";
            Directory.CreateDirectory(folder);
            int count = 0, meshes = 0;
            using (var file = new BinaryWriter(File.Create(folder + "/unity_triangles.bin")))
            using (var report = new StreamWriter(folder + "/UnityExport.txt"))
            {
                report.WriteLine("Source: " + scene.path);
                foreach (var mf in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshFilter>(false)))
                {
                    var renderer = mf.GetComponent<MeshRenderer>();
                    var mesh = mf.sharedMesh;
                    if (!renderer || !renderer.enabled || !mesh) continue;
                    // Only the most detailed LOD contributes geometry.
                    var group = mf.GetComponentInParent<LODGroup>();
                    if (group && group.GetLODs().Length > 0 && !group.GetLODs()[0].renderers.Contains(renderer)) continue;
                    var verts = mesh.vertices;
                    var matrix = mf.transform.localToWorldMatrix;
                    for (int i = 0; i < verts.Length; i++) verts[i] = matrix.MultiplyPoint3x4(verts[i]);
                    var mats = renderer.sharedMaterials;
                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    {
                        if (mesh.GetTopology(sub) != MeshTopology.Triangles) continue;
                        var mat = mats.Length > sub ? mats[sub] : null;
                        var key = mat ? mat.name.ToLowerInvariant() : "";
                        Color32 col = new Color32(176,147,108,255);
                        if (new[]{"floor","plaster","stone","ceiling"}.Any(key.Contains)) col=new Color32(226,225,213,255);
                        if (new[]{"teal","velvet","green"}.Any(key.Contains)) col=new Color32(78,119,111,255);
                        if (new[]{"red","burgundy","runner"}.Any(key.Contains)) col=new Color32(158,83,77,255);
                        if (new[]{"brass","gold"}.Any(key.Contains)) col=new Color32(183,153,83,255);
                        if (new[]{"black","metal","iron"}.Any(key.Contains)) col=new Color32(76,83,81,255);
                        var indices = mesh.GetTriangles(sub);
                        for (int i=0;i<indices.Length;i+=3)
                        {
                            for (int j=0;j<3;j++) { var v=verts[indices[i+j]]; file.Write(v.x);file.Write(v.y);file.Write(v.z); }
                            file.Write(col.r);file.Write(col.g);file.Write(col.b);count++;
                        }
                    }
                    report.WriteLine(mf.name+" | "+renderer.bounds+" | "+AssetDatabase.GetAssetPath(mesh)); meshes++;
                }
                report.WriteLine("Meshes: "+meshes+"; triangles: "+count);
            }
            Debug.Log("SCENE_PLAN_EXPORT_SUCCESS meshes="+meshes+" triangles="+count);
        }
    }
}
