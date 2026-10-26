using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace BuyBidBlow.Dev.Editor {
// Uses Unity's isolated preview renderer with the actual staff model.
static class HowToPlayClerkPreview {
 public static void Create(){
 const string path="Assets/Project/UI/Menu/HowToPlay/Examples/Clerk.png";
 var preview=new PreviewRenderUtility();Texture2D texture=null;
 try{
 var pig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Content/Characters/Pig/StandingStaff/PF_Staff_Green.prefab"));preview.AddSingleGO(pig);
 var animator=pig.GetComponent<Animator>();animator.runtimeAnimatorController.animationClips.First(c=>c.isLooping).SampleAnimation(pig,0);
 var renderers=pig.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
 preview.camera.orthographic=true;preview.camera.orthographicSize=bounds.size.y*.34f;preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=bounds.size.y*10;
 var focus=bounds.center+Vector3.up*bounds.size.y*.18f;preview.camera.transform.position=focus+Vector3.forward*bounds.size.y*3;preview.camera.transform.LookAt(focus);
 preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.025f,.13f,.12f,0);
 preview.ambientColor=new Color(.6f,.6f,.6f);preview.lights[0].intensity=3;preview.lights[0].transform.rotation=Quaternion.Euler(20,180,0);preview.lights[1].intensity=2;preview.lights[1].transform.rotation=Quaternion.Euler(15,130,0);
 preview.BeginStaticPreview(new Rect(0,0,800,400));preview.Render(true);texture=preview.EndStaticPreview();
 // Replace via temp file so imported image mappings do not block the write.
 var temporary=path+".new";File.WriteAllBytes(temporary,texture.EncodeToPNG());if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
 AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
 var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
 }finally{if(texture)Object.DestroyImmediate(texture);preview.Cleanup();}
 }
}
}
