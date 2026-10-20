using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BuyBidBlow.Props;
namespace BuyBidBlow.AuctionDisplay.Editor {
 public static partial class EnvelopeBuilder {
 static void AddCloche(GameObject envelope){
 var glass=Material("M_ClocheGlass",new Color(.72f,.9f,.95f,.10f));AssetDatabase.ImportAsset(Root+"/ClocheGlass.shader",ImportAssetOptions.ForceSynchronousImport);glass.shader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/ClocheGlass.shader");glass.SetFloat("_CenterOpacity",.035f);glass.SetFloat("_RimOpacity",.38f);glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_SrcBlendAlpha",1);glass.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Cull",2);glass.SetFloat("_Smoothness",.94f);glass.SetFloat("_Metallic",.05f);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;glass.SetShaderPassEnabled("ShadowCaster",false);EditorUtility.SetDirty(glass);
 int segments=40,rings=14;float radius=1.32f;var vertices=new Vector3[(rings+1)*(segments+1)];var indices=new System.Collections.Generic.List<int>();
 for(int r=0;r<=rings;r++){float theta=r*Mathf.PI*.5f/rings;for(int s=0;s<=segments;s++){float phi=s*Mathf.PI*2/segments;vertices[r*(segments+1)+s]=new Vector3(Mathf.Sin(theta)*Mathf.Cos(phi),Mathf.Cos(theta),Mathf.Sin(theta)*Mathf.Sin(phi))*radius;}}
 for(int r=0;r<rings;r++)for(int s=0;s<segments;s++){int a=r*(segments+1)+s,b=a+segments+1;indices.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
 var mesh=SaveMesh("SM_ClocheHemisphere",vertices,indices.ToArray());var normals=new Vector3[vertices.Length];for(int k=0;k<vertices.Length;k++)normals[k]=vertices[k].normalized;mesh.normals=normals;EditorUtility.SetDirty(mesh);
 var cloche=new GameObject("Glass Cloche - Stationary").transform;cloche.SetParent(envelope.transform,false);cloche.localPosition=new Vector3(0,-.57f,0);
 var dome=Part("Transparent Hemisphere",cloche,mesh,glass);dome.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
 var brass=Material("M_ClocheBrass",new Color(.62f,.40f,.14f));brass.SetFloat("_Metallic",.25f);brass.SetFloat("_Smoothness",.55f);
 // Thin stationary circular tray and a small glass knob, separate from rotating envelope.
 var tray=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tray.name="Thin Brass Tray";tray.transform.SetParent(cloche,false);tray.transform.localPosition=new Vector3(0,-.018f,0);tray.transform.localScale=new Vector3(radius*2+.09f,.018f,radius*2+.09f);tray.GetComponent<MeshRenderer>().sharedMaterial=brass;UnityEngine.Object.DestroyImmediate(tray.GetComponent<Collider>());
 var knob=GameObject.CreatePrimitive(PrimitiveType.Sphere);knob.name="Glass Knob";knob.transform.SetParent(cloche,false);knob.transform.localPosition=new Vector3(0,radius+.065f,0);knob.transform.localScale=Vector3.one*.15f;knob.GetComponent<MeshRenderer>().sharedMaterial=glass;knob.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;UnityEngine.Object.DestroyImmediate(knob.GetComponent<Collider>());
 }
 }
}

