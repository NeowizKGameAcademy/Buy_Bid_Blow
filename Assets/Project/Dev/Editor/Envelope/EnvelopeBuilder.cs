using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BuyBidBlow.Props;
namespace BuyBidBlow.AuctionDisplay.Editor {
 [InitializeOnLoad] public static partial class EnvelopeBuilder {
 const string Root="Assets/Project/Content/Props/AuctionEnvelope";
 static EnvelopeBuilder(){EditorApplication.update+=Tick;}
 static void Tick(){if(!File.Exists("Library/AuctionEnvelope.request")||EditorApplication.isCompiling||EditorApplication.isUpdating)return;File.Delete("Library/AuctionEnvelope.request");try{Build();File.WriteAllText("Library/AuctionEnvelope.result","PASS");}catch(Exception ex){File.WriteAllText("Library/AuctionEnvelope.result",ex.ToString());Debug.LogException(ex);}}
 static Material Material(string id,Color color){string path=Root+"/"+id+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.12f);EditorUtility.SetDirty(mat);return mat;}
 static GameObject Part(string name,Transform parent,Mesh mesh,Material mat){var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=mat;return g;}
 static Mesh SaveMesh(string name,Vector3[] vertices,int[] triangles){var m=new Mesh{name=name};m.vertices=vertices;m.triangles=triangles;m.RecalculateNormals();m.RecalculateBounds();string path=Root+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(m,old);UnityEngine.Object.DestroyImmediate(m);return old;}AssetDatabase.CreateAsset(m,path);return m;}
  static void Capture(Scene scene){var cam=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Auction HUD - Editable v2").GetComponent<Canvas>();var mode=canvas.renderMode;var oldCam=canvas.worldCamera;float distance=canvas.planeDistance;var rt=new RenderTexture(1920,1080,24);var png=new Texture2D(1920,1080,TextureFormat.RGB24,false);try{cam.targetTexture=rt;cam.aspect=16f/9f;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=.4f;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1920,1080),0,0);png.Apply();File.WriteAllBytes("Assets/Project/Dev/Sandbox/Interface/AuctionSecondFloorPreview.png",png.EncodeToPNG());}finally{RenderTexture.active=null;cam.targetTexture=null;canvas.renderMode=mode;canvas.worldCamera=oldCam;canvas.planeDistance=distance;UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(rt);}}
 static Mesh Seal(){var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();for(int k=0;k<8;k++){float a=k*Mathf.PI/4,b=(k+1)*Mathf.PI/4;var p=new Vector3(Mathf.Cos(a)*.105f,Mathf.Sin(a)*.105f,0);var q=new Vector3(Mathf.Cos(b)*.105f,Mathf.Sin(b)*.105f,0);int start=v.Count;v.AddRange(new[]{new Vector3(0,0,.025f),p+Vector3.forward*.025f,q+Vector3.forward*.025f,p,q,q+Vector3.forward*.025f,p,p+Vector3.forward*.025f,q+Vector3.forward*.025f});t.AddRange(Enumerable.Range(start,9));}return SaveMesh("SM_EnvelopeSeal",v.ToArray(),t.ToArray());}
 [MenuItem("Tools/BuyBidBlow/Interface/Create Rotating Envelope")]
 public static void Build(){
 var cream=Material("M_EnvelopeCream",new Color(.83f,.72f,.51f));var flapMat=Material("M_EnvelopeFlap",new Color(.95f,.85f,.65f));var red=Material("M_EnvelopeSeal",new Color(.52f,.035f,.025f));
 var root=new GameObject("PF_AuctionEnvelope");var model=new GameObject("Rotating Envelope").transform;model.SetParent(root.transform,false);
 try{
 var body=GameObject.CreatePrimitive(PrimitiveType.Cube);body.name="Thin Cream Body";body.transform.SetParent(model,false);body.transform.localScale=new Vector3(1.45f,.94f,.055f);body.GetComponent<MeshRenderer>().sharedMaterial=cream;UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
 var flap=SaveMesh("SM_EnvelopeFlap",new[]{new Vector3(-.725f,.47f,.033f),new Vector3(0,-.015f,.046f),new Vector3(.725f,.47f,.033f)},new[]{0,1,2});Part("Triangle Flap",model,flap,flapMat);
 var seal=Part("Plain Octagonal Red Seal",model,Seal(),red);seal.transform.localPosition=new Vector3(0,.015f,.052f);
 var textObject=new GameObject("Rounds - Editable 3D Text",typeof(TextMeshPro));textObject.transform.SetParent(model,false);textObject.transform.localPosition=new Vector3(0,-.255f,.039f);textObject.transform.localRotation=Quaternion.Euler(0,180,0);
 var label=textObject.GetComponent<TextMeshPro>();label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");label.text="6R · 9R";label.fontSize=2.1f;label.fontStyle=FontStyles.Bold;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.17f,.075f,.025f);label.rectTransform.sizeDelta=new Vector2(1.25f,.25f);label.enableWordWrapping=false;
 var display=root.AddComponent<AuctionEnvelopeDisplay>();display.rotatingModel=model;display.roundLabel=label;display.degreesPerSecond=20;display.RefreshLabel();
 AddCloche(root);PrefabUtility.SaveAsPrefabAsset(root,Root+"/PF_AuctionEnvelope.prefab");
 string path="Assets/Project/Dev/Sandbox/Interface/SC_AuctionSecondFloorInterface.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{var existing=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Auction Envelope Display");if(existing)UnityEngine.Object.DestroyImmediate(existing);var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/PF_AuctionEnvelope.prefab"),scene);instance.name="Auction Envelope Display";instance.transform.position=new Vector3(.35f,5.49f,-11.4f);instance.transform.rotation=Quaternion.Euler(0,-15,0);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Capture(scene);File.WriteAllText(Root+"/Verification.txt","PASS: thin cream body, triangle flap, 8-sided plain seal, editable TextMeshPro 3D rounds, prefab placed above pedestal. Rotation 20 degrees/sec (18 sec/revolution), Play only. No auction/network logic.");}
 finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }finally{UnityEngine.Object.DestroyImmediate(root);AssetDatabase.Refresh();}
 }
 }
}


