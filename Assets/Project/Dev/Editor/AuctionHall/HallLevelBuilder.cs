using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using BuyBidBlow.Props;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallLevelBuilder
    {
        public const string Root="Assets/Project/Content/Levels/ExchangeFurnished";
        public const string ScenePath="Assets/Project/Scenes/SC_ExchangeFurnished.unity";
        const string Data="Logs/HallImport";
        [Serializable] public class Manifest { public string source; public int sourceObjects,vertices,triangles; public Mat[] materials; public Group[] groups; }
        [Serializable] public class Mat { public string name,baseMap,normalMap; public float[] color,emission,scale; public float metallic,roughness; public bool alphaClip; }
        [Serializable] public class Part { public string file; public int material; }
        [Serializable] public class Box { public float[] center,size; }
        [Serializable] public class Rope { public float[][] points; public float length; }
        // JsonUtility does not support jagged arrays: rope paths are loaded from flattened data.
        [Serializable] public class PathData { public float[] points; public float length; }
        [Serializable] public class Group { public string name,kind; public float[] position,forward; public Part[] parts; public Box[] boxes; public float angle; public int slots; public PathData[] paths; public float[] postPositions; }
        static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
        static Color C(float[] a)=>new Color(a[0],a[1],a[2],a.Length>3?a[3]:1);
        static T Save<T>(T obj,string path) where T:UnityEngine.Object
        {
            var old=AssetDatabase.LoadAssetAtPath<T>(path);
            if(old){EditorUtility.CopySerialized(obj,old);UnityEngine.Object.DestroyImmediate(obj);return old;}
            AssetDatabase.CreateAsset(obj,path);return obj;
        }
        static Mesh ReadMesh(string path,bool collider=false)
        {
            using var r=new BinaryReader(File.OpenRead(path));int count=r.ReadInt32(),indices=r.ReadInt32();
            var verts=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];
            for(int i=0;i<count;i++){verts[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());if(!collider){normals[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());}}
            var tris=new int[indices];for(int i=0;i<indices;i++)tris[i]=r.ReadInt32();
            var mesh=new Mesh{name=Path.GetFileNameWithoutExtension(path),indexFormat=IndexFormat.UInt32};mesh.vertices=verts;mesh.triangles=tris;
            if(!collider){mesh.normals=normals;mesh.uv=uv;mesh.RecalculateTangents();}mesh.RecalculateBounds();return mesh;
        }
        [MenuItem("BuyBidBlow/Hall/Import Furnished Hall")]
        public static void Build()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            foreach(string dir in new[]{"Meshes","Materials","Prefabs"})Directory.CreateDirectory(Root+"/"+dir);
            AssetDatabase.Refresh();
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Data+"/manifest.json"));
            var mats=new Material[manifest.materials.Length];
            for(int i=0;i<mats.Length;i++)
            {
                var src=manifest.materials[i];var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=src.name};
                mat.SetColor("_BaseColor",C(src.color).gamma);mat.SetFloat("_Metallic",Mathf.Clamp01(src.metallic));mat.SetFloat("_Smoothness",1-Mathf.Clamp01(src.roughness));
                mat.SetFloat("_Cull",0);
                if(!string.IsNullOrEmpty(src.baseMap))mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+src.baseMap));
                if(src.scale!=null)mat.SetTextureScale("_BaseMap",new Vector2(src.scale[0],src.scale[1]));
                if(!string.IsNullOrEmpty(src.normalMap))
                {
                    string p=Root+"/"+src.normalMap;var importer=(TextureImporter)AssetImporter.GetAtPath(p);
                    if(importer.textureType!=TextureImporterType.NormalMap){importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();}
                    mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(p));mat.SetFloat("_BumpScale",.35f);mat.EnableKeyword("_NORMALMAP");
                }
                if(src.alphaClip){mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.4f);mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;}
                if(src.name.Contains("Frond") || src.name.Contains("Leaf")){mat.SetFloat("_SpecularHighlights",0);mat.SetFloat("_EnvironmentReflections",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");}
                if(src.emission.Any(x=>x>.01f)){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",C(src.emission));}
                string materialRoot=src.name.StartsWith("BBB_TV_",StringComparison.Ordinal)?HallTickerBoardSetup.Root:Root;
                Directory.CreateDirectory(materialRoot+"/Materials");
                mats[i]=Save(mat,materialRoot+"/Materials/"+src.name+".mat");
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var hall=new GameObject("AuctionHall_Furnished");
            foreach(var group in manifest.groups)
            {
                var go=new GameObject(group.name);go.transform.SetParent(hall.transform);go.transform.localPosition=V(group.position);
                foreach(var part in group.parts)
                {
                    var mesh=Save(ReadMesh(Data+"/"+part.file),Root+"/Meshes/"+Path.GetFileNameWithoutExtension(part.file)+".asset");
                    var child=group.kind=="banner"
                        ? (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Content/Props/ChampionshipBanner/ChampionshipBanner.prefab"))
                        : new GameObject(mesh.name);
                    child.transform.SetParent(go.transform,false);
                    if(group.kind=="banner")
                    {
                        var skin=child.GetComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.updateWhenOffscreen=true;skin.localBounds=new Bounds(mesh.bounds.center,mesh.bounds.size+Vector3.one*3);
                        var cloth=child.GetComponent<Cloth>();
                        var coeff=new ClothSkinningCoefficient[cloth.vertices.Length];var bounds=mesh.bounds;
                        for(int v=0;v<coeff.Length;v++){var p=cloth.vertices[v];bool pin=p.y>bounds.max.y-.15f && (p.x<bounds.min.x+.25f || p.x>bounds.max.x-.25f);coeff[v].maxDistance=pin?0:.3f+(bounds.max.y-p.y)*.3f;}
                        cloth.coefficients=coeff;child.AddComponent<BannerWind>();
                    }
                    else{child.AddComponent<MeshFilter>().sharedMesh=mesh;child.AddComponent<MeshRenderer>().sharedMaterial=mats[part.material];}
                }
                if(group.boxes!=null)foreach(var box in group.boxes){var col=go.AddComponent<BoxCollider>();col.center=V(box.center);col.size=Vector3.Max(V(box.size),Vector3.one*.02f);}
                if(group.kind=="door" || group.kind=="seat")
                {
                    var bounds=LocalBounds(go);var col=go.AddComponent<BoxCollider>();col.center=bounds.center;col.size=bounds.size;
                    if(group.kind=="door")go.AddComponent<Door>().openAngle=group.angle;
                    else if (!group.name.Contains("Staff_Chair"))
                    {
                        var seat=go.AddComponent<Seat>();seat.auction=group.name.StartsWith("A41_");var forward=V(group.forward).normalized;var right=Vector3.Cross(Vector3.up,forward);
                        float width=Mathf.Abs(right.x)*bounds.size.x+Mathf.Abs(right.z)*bounds.size.z;
                        float depth=Mathf.Abs(forward.x)*bounds.size.x+Mathf.Abs(forward.z)*bounds.size.z;
                        int slots=Mathf.Max(1,group.slots);seat.sittingPoints=new Transform[slots];seat.exitPoints=new Transform[slots+10];
                        for(int k=0;k<slots;k++)
                        {
                            float spread=slots==1?0:(k-(slots-1)*.5f)*Mathf.Min(.6f,width/(slots+.3f));
                            var pos=bounds.center+right*spread;pos.y=bounds.min.y+Mathf.Min(.52f,bounds.size.y*.47f);
                            seat.sittingPoints[k]=Marker(go,"Seat_"+k,pos,forward);
                            var exit=bounds.center+right*spread+forward*(depth*.5f+.65f);exit.y=bounds.min.y+.02f;seat.exitPoints[k]=Marker(go,"Exit_"+k,exit,forward);
                        }
                        for(int k=0;k<2;k++){var exit=bounds.center+right*(k==0?-1:1)*(width*.5f+.65f);exit.y=bounds.min.y+.02f;seat.exitPoints[slots+k]=Marker(go,"SideExit_"+k,exit,forward);}
                        for(int k=0;k<8;k++)
                        {
                            float angle=k*Mathf.PI/4;
                            var offset=(right*Mathf.Cos(angle)+forward*Mathf.Sin(angle))*(Mathf.Max(width,depth)*.5f+.65f);
                            var exit=bounds.center+offset;exit.y=bounds.min.y+.02f;
                            seat.exitPoints[slots+2+k]=Marker(go,"AlternativeExit_"+k,exit,forward);
                        }
                    }
                }
                if(group.kind=="barrier")
                {
                    if(group.postPositions!=null)for(int p=0;p<group.postPositions.Length;p+=3){var col=go.AddComponent<CapsuleCollider>();col.center=new Vector3(group.postPositions[p],group.postPositions[p+1]+.55f,group.postPositions[p+2]);col.height=1.1f;col.radius=.12f;}
                    int ri=0;foreach(var path in group.paths)
                    {
                        var ropeGo=new GameObject("PhysicsRope_"+ri++);ropeGo.transform.SetParent(go.transform,false);var rope=ropeGo.AddComponent<RopeBarrier>();
                        rope.restPoints=new Vector3[path.points.Length/3];for(int k=0;k<rope.restPoints.Length;k++)rope.restPoints[k]=new Vector3(path.points[k*3],path.points[k*3+1],path.points[k*3+2]);
                        rope.ropeLength=path.length;rope.startAnchor=Marker(go,"Anchor_Start_"+ri,rope.restPoints[0],Vector3.forward);rope.endAnchor=Marker(go,"Anchor_End_"+ri,rope.restPoints[rope.restPoints.Length-1],Vector3.forward);
                        ropeGo.GetComponent<MeshRenderer>().sharedMaterial=mats[Array.FindIndex(manifest.materials,m=>m.name.Contains("RB_Burgundy"))];
                        // Persist a rest mesh so prefab instances also display outside play mode.
                        rope.Initialize();var rest=UnityEngine.Object.Instantiate(ropeGo.GetComponent<MeshFilter>().sharedMesh);ropeGo.GetComponent<MeshFilter>().sharedMesh=Save(rest,Root+"/Meshes/"+group.name+"_Rope_"+ri+".asset");
                    }
                }
                if(group.kind!="static")
                {
                    if(group.kind=="banner")HallBannerMounting.Configure(go);
                    Vector3 position=go.transform.position;go.transform.SetParent(null);go.transform.position=Vector3.zero;
                    var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+group.name+".prefab");UnityEngine.Object.DestroyImmediate(go);
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,hall.transform);instance.transform.localPosition=position;
                }
            }
            var colliderMesh=Save(ReadMesh(Data+"/collision.bin",true),Root+"/Meshes/Hall_Structure_Collision.asset");
            var structure=new GameObject("StructuralCollision");structure.transform.SetParent(hall.transform,false);structure.layer=6;structure.AddComponent<MeshCollider>().sharedMesh=colliderMesh;
            HallFurnitureCollision.Attach(hall.transform);
            HallTickerBoardSetup.ReplaceMergedBoards(hall);
            PrefabUtility.SaveAsPrefabAsset(hall,Root+"/AuctionHall_Furnished.prefab");
            AddLighting();AddPlayer();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);AssetDatabase.SaveAssets();
            File.WriteAllText(Root+"/Import_Report.json",JsonUtility.ToJson(manifest,true));
            Debug.Log("HALL_BUILD_OK groups="+manifest.groups.Length+" triangles="+manifest.triangles);
        }
        static Transform Marker(GameObject parent,string name,Vector3 pos,Vector3 forward){var o=new GameObject(name).transform;o.SetParent(parent.transform,false);o.localPosition=pos;o.localRotation=Quaternion.LookRotation(forward,Vector3.up);return o;}
        static Bounds LocalBounds(GameObject go){var meshes=go.GetComponentsInChildren<MeshFilter>();var b=new Bounds();bool first=true;foreach(var m in meshes){var v=m.sharedMesh.bounds;if(first){b=v;first=false;}else b.Encapsulate(v);}return b;}
        static void AddLighting()
        {
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.58f,.61f,.65f);RenderSettings.ambientIntensity=1;
            // Indoor environment reflection keeps metallic fittings readable without a skybox.
            var environment=new Cubemap(32,TextureFormat.RGBAHalf,true);
            for(int face=0;face<6;face++)
            {
                var pixels=new Color[32*32];
                for(int y=0;y<32;y++)for(int x=0;x<32;x++) {
                    float value=face==2?.9f:face==3?.22f:.38f+.3f*Mathf.Pow(Mathf.Sin((x+.5f)/32*Mathf.PI),8);
                    pixels[y*32+x]=new Color(value,value*.9f,value*.74f,1);
                }
                environment.SetPixels(pixels,(CubemapFace)face);
            }
            environment.Apply(true);RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;
            RenderSettings.customReflection=Save(environment,Root+"/Materials/Hall_Indoor_Reflection.asset");
            var root=new GameObject("Hall Lighting");
            for(int i=0;i<3;i++){var l=new GameObject("Warm chandelier "+i).AddComponent<Light>();l.transform.SetParent(root.transform);l.type=LightType.Point;l.transform.position=new Vector3(0,7,-12+i*12);l.range=23;l.intensity=6;l.color=new Color(1,.83f,.65f);}
            var fill=new GameObject("Soft Fill").AddComponent<Light>();fill.transform.SetParent(root.transform);fill.type=LightType.Directional;fill.intensity=.7f;fill.transform.rotation=Quaternion.Euler(65,-20,0);fill.shadows=LightShadows.Soft;
        }
        static void AddPlayer()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Characters/Player/Player.prefab");
            var player=(GameObject)PrefabUtility.InstantiatePrefab(prefab);player.transform.position=new Vector3(0,.05f,-5);
        }
        public static void RunBatch(){try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/hall-build-error.txt",e.ToString());EditorApplication.Exit(1);}}
    }
}
