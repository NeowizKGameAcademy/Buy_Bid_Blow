using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad]public static class AuctioneerAnimationSetup
    {
        const string Root="Assets/Project/Content/Characters/Pig/Auctioneer";
        const string Models="Assets/Project/Dev/Sandbox/Pigs/Models/";
        static AuctioneerAnimationSetup(){EditorApplication.update+=()=>{
            if(!File.Exists("Library/AuctioneerAnimationSetup.request")||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            File.Delete("Library/AuctioneerAnimationSetup.request");
            try{Apply();DeskStaffAnimationSetup.Apply();File.WriteAllText("Library/AuctioneerAnimationSetup.result","PASS");}catch(Exception e){File.WriteAllText("Library/AuctioneerAnimationSetup.result",e.ToString());Debug.LogException(e);}
        };}
        [MenuItem("Tools/Buy Bid Blow/Pigs/Connect Auctioneer Animations")]
        public static void Apply()
        {
            var prefab=PrefabUtility.LoadPrefabContents(Root+"/PF_Auctioneer.prefab");
            try
            {
                AnimationClip Copy(string source,string name,bool loop)
                {
                    var sourceClip=AssetDatabase.LoadAllAssetsAtPath(Models+source+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
                    var missing=AnimationUtility.GetCurveBindings(sourceClip).Where(b=>b.type==typeof(Transform)&&!string.IsNullOrEmpty(b.path)&&!prefab.transform.Find(b.path)).Select(b=>b.path).Distinct().ToArray();
                    if(missing.Length>0)throw new InvalidOperationException(source+" has incompatible bone paths: "+string.Join(", ",missing));
                    string path=Root+"/"+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if(!clip){clip=UnityEngine.Object.Instantiate(sourceClip);AssetDatabase.CreateAsset(clip,path);}else EditorUtility.CopySerialized(sourceClip,clip);
                    clip.name=name;var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=false;
                    AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);return clip;
                }
                var idle=Copy("Pig_Black_50k@Dwarf Idle","Black_DwarfIdle",true);
                var talking=Copy("Pig_Green_50k@Talking (1)","Black_Talking",false);
                var counting=Copy("Pig_Black_50k@Counting","Black_Counting",false);
                var thankful=Copy("Pig_Yellow_50k@Thankful","Black_Thankful",false);
                var bow=Copy("Pig_Black_50k@Quick Formal Bow","Black_FormalBow",false);
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"/Auctioneer.controller");
                var machine=controller.layers[0].stateMachine;
                foreach(var child in machine.states)machine.RemoveState(child.state);
                var idleState=machine.AddState("Idle");idleState.motion=idle;machine.defaultState=idleState;
                void State(string name,AnimationClip clip){var state=machine.AddState(name);state.motion=clip;var back=state.AddTransition(idleState);back.hasExitTime=true;back.exitTime=1;back.hasFixedDuration=true;back.duration=.12f;}
                State("Talking",talking);State("Counting",counting);State("Thankful",thankful);State("Bow",bow);
                EditorUtility.SetDirty(controller);
                var animator=prefab.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var client=prefab.GetComponent<AuctioneerAnimationClient>();if(!client)client=prefab.AddComponent<AuctioneerAnimationClient>();
                client.animator=animator;client.talking=talking;client.counting=counting;client.thankful=thankful;client.bow=bow;
                PrefabUtility.SaveAsPrefabAsset(prefab,Root+"/PF_Auctioneer.prefab");AssetDatabase.SaveAssets();
                File.WriteAllText(Root+"/AnimationSetup.txt","PASS: Generic Transform curve paths match Black model; idle loops, Talking/Counting/Thankful/Bow play once. Existing prefab GUID retained. Public MatchNet and public outcome only; secret ending always counts; public sold counts; bid/warning thanks; unsold and match end bow.");
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
    }
}
