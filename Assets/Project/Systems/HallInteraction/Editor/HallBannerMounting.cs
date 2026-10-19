using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BuyBidBlow.Hall.Editor
{
    public static class HallBannerMounting
    {
        const string Root=HallLevelBuilder.Root;
        const string Request="Logs/banner-mount-request.txt";
        public static void Configure(GameObject root)
        {
            var renderer=root.GetComponentInChildren<SkinnedMeshRenderer>();
            var source=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/A78_Auction_Rear_Banner_17.asset");
            var mesh=UnityEngine.Object.Instantiate(source);mesh.name="Banner_Between_Beams";
            var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++)
            {
                var p=vertices[i];float u=Mathf.Clamp(p.x/8f,-1,1);
                p.x*=.7f;p.y=10.85f+(p.y-10.9f)*.8f-.48f*(1-u*u);p.z-=.6f;vertices[i]=p;
            }
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            string path=Root+"/Meshes/Banner_Between_Beams.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
            renderer.sharedMesh=mesh;renderer.localBounds=new Bounds(mesh.bounds.center,mesh.bounds.size+Vector3.one*2);
            var cloth=renderer.GetComponent<Cloth>();
            var cv=cloth.vertices;var coefficients=new ClothSkinningCoefficient[cv.Length];
            for(int i=0;i<cv.Length;i++)
            {
                bool pin=Mathf.Abs(cv[i].x)>5.45f && cv[i].y>10.76f;
                coefficients[i].maxDistance=pin?0:.18f+Mathf.Clamp01((10.85f-cv[i].y)/2.8f)*.32f;
                coefficients[i].collisionSphereDistance=0;
            }
            cloth.coefficients=coefficients;cloth.collisionMassScale=.2f;cloth.friction=.5f;
            var old=root.transform.Find("BeamMounts");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var mounts=new GameObject("BeamMounts");mounts.transform.SetParent(root.transform,false);
            var gold=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/RB_Satin_Gold.mat");
            var supports=new CapsuleCollider[2];
            for(int i=0;i<2;i++)
            {
                float sign=i==0?-1:1;
                var beam=new Vector3(sign*6.15f,10.98f,15.02f);
                var corner=new Vector3(sign*5.60f,10.85f,14.75f);
                Cylinder(mounts.transform,"MountingTie_"+i,beam,corner,.024f,gold);
                var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name="BeamBracket_"+i;
                plate.transform.SetParent(mounts.transform,false);plate.transform.localPosition=beam;
                plate.transform.localScale=new Vector3(.12f,.18f,.08f);plate.GetComponent<Renderer>().sharedMaterial=gold;
                UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>());
                var proxy=new GameObject("BeamClothCollision_"+i);proxy.transform.SetParent(mounts.transform,false);
                Vector3 a=new Vector3(sign*6.65f,8.35f,16.05f),b=new Vector3(sign*6.0f,11.65f,14.82f);
                proxy.transform.localPosition=(a+b)*.5f;proxy.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
                var collider=proxy.AddComponent<CapsuleCollider>();collider.radius=.43f;collider.height=Vector3.Distance(a,b)+.86f;collider.isTrigger=true;supports[i]=collider;
            }
            var wind=renderer.GetComponent<HallBannerWind>();wind.supportColliders=supports;wind.SetContacts(Array.Empty<CapsuleCollider>());
            if(coefficients.Count(c=>c.maxDistance==0)<2)throw new Exception("Banner pins missing");
        }
        static void Cylinder(Transform parent,string name,Vector3 a,Vector3 b,float radius,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=(a+b)*.5f;go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
            go.transform.localScale=new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2);
            go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        [InitializeOnLoadMethod] static void OnLoad(){EditorApplication.update-=ApplyRequest;EditorApplication.update+=ApplyRequest;}
        static void ApplyRequest()
        {
            if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(Request);EditorApplication.update-=ApplyRequest;
            try { Apply();File.WriteAllText("Logs/banner-mount-result.txt","PASS: mounted banner prefab updated; two support capsules retained."); }
            catch(Exception e){File.WriteAllText("Logs/banner-mount-result.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("BuyBidBlow/Hall/Mount Banner Between Beams")]
        public static void Apply()
        {
            string path=Root+"/Prefabs/A78_Auction_Rear_Banner.prefab";
            var contents=PrefabUtility.LoadPrefabContents(path);
            try { Configure(contents);PrefabUtility.SaveAsPrefabAsset(contents,path); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            AssetDatabase.SaveAssets();
            var banner=UnityEngine.Object.FindFirstObjectByType<HallBannerWind>();
            if(banner)
            {
                Selection.activeGameObject=banner.gameObject;
                var view=SceneView.lastActiveSceneView;
                if(view)view.LookAt(new Vector3(0,9.7f,14.8f),Quaternion.Euler(-8,0,0),10);
            }
        }
    }
}
