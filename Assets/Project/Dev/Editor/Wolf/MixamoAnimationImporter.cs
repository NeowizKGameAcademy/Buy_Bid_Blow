using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace BuyBidBlow.Dev.Editor {
 [InitializeOnLoad] public static class MixamoAnimationImporter {
 const string Root="Assets/Project/Content/Characters/Wolf/Animation/Mixamo";
 static MixamoAnimationImporter(){EditorApplication.update+=Tick;}
 static void Tick(){if(!File.Exists("Library/WolfMixamo.request")||EditorApplication.isCompiling||EditorApplication.isUpdating)return;File.Delete("Library/WolfMixamo.request");try{Build();File.WriteAllText("Library/WolfMixamo.result","PASS");}catch(Exception ex){File.WriteAllText("Library/WolfMixamo.result",ex.ToString());Debug.LogException(ex);}}
 [MenuItem("Tools/Buy Bid Blow/Wolf/Import New Mixamo Animations")]
 public static void Build(){
 var report=new System.Text.StringBuilder();var sourcePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Content/Characters/Wolf/Wolf_Model.prefab");if(!sourcePrefab)throw new Exception("Wolf_Model missing");
 var wolf=UnityEngine.Object.Instantiate(sourcePrefab);wolf.hideFlags=HideFlags.HideAndDontSave;
 try{
 var transforms=wolf.GetComponentsInChildren<Transform>(true);var paths=transforms.ToDictionary(t=>AnimationUtility.CalculateTransformPath(t,wolf.transform),t=>t);
 var clips=new AnimationClip[2];string[] ids={"Idle","JoyfulJump"};
 for(int index=0;index<2;index++){
 string path=Root+"/Source/"+ids[index]+".fbx";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(ModelImporter)AssetImporter.GetAtPath(path);
 importer.importAnimation=true;importer.animationType=ModelImporterAnimationType.Legacy;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.SaveAndReimport();
 var source=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));if(!source||source.length<=0)throw new Exception("No animation in "+path);
 var clip=UnityEngine.Object.Instantiate(source);clip.name="Wolf_Mixamo_"+ids[index];clip.legacy=false;clip.hideFlags=HideFlags.None;
 int count=0;foreach(var binding in AnimationUtility.GetCurveBindings(clip)){
 if(binding.type!=typeof(Transform))continue;
 string newPath=binding.path;
 if(!paths.ContainsKey(newPath)){
 string bone=binding.path.Split('/').Last();var matches=transforms.Where(t=>t.name==bone).ToArray();if(matches.Length!=1)throw new Exception("Unmatched bone "+binding.path);
 newPath=AnimationUtility.CalculateTransformPath(matches[0],wolf.transform);
 }
 if(newPath!=binding.path){var curve=AnimationUtility.GetEditorCurve(clip,binding);AnimationUtility.SetEditorCurve(clip,binding,null);var mapped=binding;mapped.path=newPath;AnimationUtility.SetEditorCurve(clip,mapped,curve);}count++;
 }
 if(count==0)throw new Exception("No skeleton curves");
 var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=index==0;settings.loopBlend=index==0;AnimationUtility.SetAnimationClipSettings(clip,settings);
 string clipPath=Root+"/"+clip.name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;}else AssetDatabase.CreateAsset(clip,clipPath);clips[index]=clip;
 var before=transforms.Select(t=>t.localRotation).ToArray();clip.SampleAnimation(wolf,clip.length*.2f);var first=transforms.Select(t=>t.localRotation).ToArray();clip.SampleAnimation(wolf,clip.length*.7f);int moving=transforms.Where((t,i)=>Quaternion.Angle(first[i],t.localRotation)>.05f).Count();if(moving==0)throw new Exception("No bone movement in "+clip.name);
 report.AppendLine(clip.name+": "+clip.length.ToString("F2")+"s, "+clip.frameRate+"fps, "+count+" transform curves mapped, "+moving+" moving bones, loop="+settings.loopTime+", legacy="+clip.legacy);
 }
 string controllerPath=Root+"/Wolf_MixamoPreview.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
 var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);controller.parameters=new[]{new AnimatorControllerParameter{name="JoyfulJump",type=AnimatorControllerParameterType.Trigger}};
 var idle=machine.AddState("Idle");idle.motion=clips[0];machine.defaultState=idle;var jump=machine.AddState("JoyfulJump");jump.motion=clips[1];var enter=idle.AddTransition(jump);enter.hasExitTime=false;enter.duration=.12f;enter.AddCondition(AnimatorConditionMode.If,0,"JoyfulJump");var exit=jump.AddTransition(idle);exit.hasExitTime=true;exit.exitTime=1;exit.duration=.15f;
 var preview=UnityEngine.Object.Instantiate(sourcePrefab);try{preview.name="PF_Wolf_MixamoPreview";var animator=preview.GetComponent<Animator>();if(!animator)animator=preview.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;PrefabUtility.SaveAsPrefabAsset(preview,Root+"/PF_Wolf_MixamoPreview.prefab");}finally{UnityEngine.Object.DestroyImmediate(preview);}
 EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();File.WriteAllText(Root+"/Verification.txt",report+"PASS: standalone non-Legacy clips, all transform paths resolved on Wolf_Model; preview controller and prefab saved. Existing gameplay controller unchanged.\n");AssetDatabase.Refresh();
 }finally{UnityEngine.Object.DestroyImmediate(wolf);}
 }
 }
}
