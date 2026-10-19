using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BuyBidBlow.Characters;
using UnityEditor;
using UnityEngine;

namespace BuyBidBlow.Interface.Editor
{
    public static class CharacterModelPrefabBuilder
    {
        const string PigRoot="Assets/Project/Characters/NPC/Pig",WolfRoot="Assets/Project/Characters/Player/Wolf";
        static readonly Dictionary<UnityEngine.Object,UnityEngine.Object> copies=new Dictionary<UnityEngine.Object,UnityEngine.Object>();
        static string folder;
        static T CopyObject<T>(T original,string subfolder) where T:UnityEngine.Object
        {
            if(!original)return null;
            if(copies.TryGetValue(original,out var cached))return (T)cached;
            string source=AssetDatabase.GetAssetPath(original);string directory=folder+"/"+subfolder;Directory.CreateDirectory(directory);
            string clean=string.Concat(original.name.Select(c=>char.IsLetterOrDigit(c)||c=='_'||c=='-'?c:'_'));
            string path=AssetDatabase.GenerateUniqueAssetPath(directory+"/"+clean+".asset");
            T clone;
            if(original is Texture && AssetDatabase.IsMainAsset(original) && !string.IsNullOrEmpty(source)) {
                path=AssetDatabase.GenerateUniqueAssetPath(directory+"/"+Path.GetFileName(source));
                if(!AssetDatabase.CopyAsset(source,path))throw new Exception("Texture copy failed: "+source);
                clone=AssetDatabase.LoadAssetAtPath<T>(path);
            }else{
                clone=UnityEngine.Object.Instantiate(original);clone.name=original.name;AssetDatabase.CreateAsset(clone,path);
            }
            if(!clone)throw new Exception("Dependency copy missing: "+original.name);
            copies.Add(original,clone);return clone;
        }
        static Material CopyMaterial(Material original)
        {
            if(!original)return null;
            if(copies.TryGetValue(original,out var cached))return (Material)cached;
            var material=CopyObject(original,"Materials");
            foreach(string property in original.GetTexturePropertyNames())if(original.GetTexture(property))material.SetTexture(property,CopyObject(original.GetTexture(property),"Textures"));
            EditorUtility.SetDirty(material);return material;
        }
        static void Make(string source,string destination,StringBuilder report,bool wolf)
        {
            if(File.Exists(destination)) {
                throw new Exception("Preserving existing prefab: "+destination);
            }
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(source);if(!asset)throw new Exception("Missing source: "+source);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(asset);
            try {
                PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                model.name=Path.GetFileNameWithoutExtension(destination);model.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);model.transform.localScale=Vector3.one;
                if(wolf) {
                    var character=model.GetComponentInChildren<BBBWolfCharacter>(true);if(character&&character.paddle)UnityEngine.Object.DestroyImmediate(character.paddle);
                    var appearance=model.GetComponentInChildren<BBBWolfAppearance>(true);if(appearance)appearance.ApplyAppearance();
                    foreach(var t in model.GetComponentsInChildren<Transform>(true))if(new[]{"Happy","Suspicious","Shocked","BlankBluff"}.Contains(t.name))t.gameObject.SetActive(t.name=="BlankBluff");
                }
                // Keep transforms / bones / geometry only. No animation or gameplay components.
                foreach(var component in model.GetComponentsInChildren<Component>(true))
                    if(component && !(component is Transform) && !(component is Renderer) && !(component is MeshFilter))UnityEngine.Object.DestroyImmediate(component);
                foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))renderer.sharedMesh=CopyObject(renderer.sharedMesh,"Meshes");
                foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))filter.sharedMesh=CopyObject(filter.sharedMesh,"Meshes");
                foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=renderer.sharedMaterials.Select(CopyMaterial).ToArray();
                if(!PrefabUtility.SaveAsPrefabAsset(model,destination))throw new Exception("Save failed: "+destination);
                AssetDatabase.SaveAssets();
                var saved=AssetDatabase.LoadAssetAtPath<GameObject>(destination);
                if(saved.GetComponentsInChildren<Animator>(true).Length!=0||saved.GetComponentsInChildren<Animation>(true).Length!=0||saved.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)throw new Exception("Unexpected behaviour in "+destination);
                var renderers=saved.GetComponentsInChildren<Renderer>(true);
                if(renderers.Length==0||!renderers.Any(r=>r.enabled&&r.gameObject.activeSelf))throw new Exception("Missing visible geometry: "+destination);
                foreach(var r in renderers)if(r.sharedMaterials.Any(m=>!m||!m.shader||m.shader.name.Contains("InternalErrorShader")))throw new Exception("Invalid material: "+destination);
                var dependencies=AssetDatabase.GetDependencies(destination,true);
                if(dependencies.Any(p=>p.StartsWith("Assets/Project/Dev/")||p.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase)||p.EndsWith(".controller")||p.EndsWith(".anim")))throw new Exception("Unexpected dev/animation dependency: "+destination);
                report.AppendLine($"PASS {destination}: {renderers.Length} renderers, {saved.GetComponentsInChildren<Transform>(true).Length} transforms; no Animator/Animation/scripts/FBX/Dev dependency.");
            }finally{UnityEngine.Object.DestroyImmediate(model);}
        }
        [MenuItem("Tools/BuyBidBlow/Characters/Create Model Only Prefabs")]
        public static void Build()
        {
            AssetDatabase.SaveAssets();copies.Clear();Directory.CreateDirectory(PigRoot);Directory.CreateDirectory(WolfRoot);AssetDatabase.Refresh();
            var report=new StringBuilder();folder=PigRoot;
            foreach(string color in new[]{"Red","Yellow","Green","Black"}) {
                string file=color=="Black"?"Pig_Black_50k@Dwarf Idle":"Pig_"+color+"_50k@Standing Idle";
                Make("Assets/Project/Dev/Sandbox/Pigs/Models/"+file+".fbx",folder+"/PF_Pig_"+color+"_Model.prefab",report,false);
            }
            folder=WolfRoot;
            Make("Assets/Project/Characters/Player/Wolf/WolfExpressions/PF_Wolf_Character.prefab",folder+"/PF_Wolf_Model.prefab",report,true);
            AssetDatabase.SaveAssets();
            foreach(var path in new[]{PigRoot,WolfRoot}.SelectMany(root=>Directory.GetFiles(root,"*.prefab",SearchOption.AllDirectories))) {
                var dependencies=AssetDatabase.GetDependencies(path,true);
                if(dependencies.Any(p=>p.StartsWith("Assets/Project/Dev/")||p.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase)||p.EndsWith(".controller")||p.EndsWith(".anim")))throw new Exception("External model dependency: "+string.Join(", ",dependencies));
            }
            File.WriteAllText("Library/CharacterModelPrefabs.result",report.AppendLine("PASS: all five prefabs contain only model components and Content dependencies.").ToString());AssetDatabase.Refresh();
        }
        [InitializeOnLoadMethod] static void Request()
        {
            if(!File.Exists("Library/CharacterModelPrefabs.request"))return;
            EditorApplication.delayCall+=()=>{File.Delete("Library/CharacterModelPrefabs.request");try{Build();}catch(Exception ex){File.WriteAllText("Library/CharacterModelPrefabs.result",ex.ToString());Debug.LogException(ex);}};
        }
    }
}
