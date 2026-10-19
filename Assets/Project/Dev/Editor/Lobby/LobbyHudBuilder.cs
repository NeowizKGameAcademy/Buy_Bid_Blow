using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.UI.Lobby;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad] public static class LobbyHudBuilder
    {
        const string Request="Library/LobbyHud.request";
        static double next;
        static LobbyHudBuilder(){EditorApplication.update+=Tick;}
        [MenuItem("Tools/BuyBidBlow/Lobby/Apply Sprite HUD")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");
            var previous=SceneManager.GetActiveScene();
            var scene=SceneManager.GetSceneByPath(LobbyBuilder.ScenePath);bool opened=!scene.IsValid();
            if(opened)scene=EditorSceneManager.OpenScene(LobbyBuilder.ScenePath,OpenSceneMode.Additive);
            if(scene.isDirty)throw new Exception("Save your current lobby edits before applying HUD.");
            try {
                SceneManager.SetActiveScene(scene);
                var old=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LobbyScreen>(true)).Single();old.enabled=false;
                var hud=old.GetComponent<LobbyHud>();if(!hud)hud=old.gameObject.AddComponent<LobbyHud>();
                hud.font=old.font;hud.view=old.view;hud.room=old.room;
                hud.library=AssetDatabase.LoadAssetAtPath<LobbySpriteLibrary>(LobbyBuilder.Root+"/LobbySpriteLibrary.asset");
                if(!hud.library || hud.library.entries.Length!=33)throw new Exception("Import lobby sprites first.");
                hud.view.transform.position=new Vector3(0,3.3f,-.4f);hud.view.transform.LookAt(new Vector3(0,1.6f,-10.1f));hud.view.fieldOfView=48;
                var preview=GameObject.Find("Lobby Art Preview - Editor Only");if(preview)UnityEngine.Object.DestroyImmediate(preview);
                preview=new GameObject("Lobby Art Preview - Editor Only");preview.tag="EditorOnly";
                var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Characters/Player/Wolf/WolfExpressions/PF_Wolf_Character.prefab");
                for(int i=0;i<4;i++) {
                    var wolf=(GameObject)PrefabUtility.InstantiatePrefab(original);wolf.name="Preview Wolf "+(i+1);wolf.transform.SetParent(preview.transform);
                    var animator=wolf.GetComponentInChildren<Animator>();
                    var clip=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0);
                    if(clip)clip.SampleAnimation(animator.gameObject,0);
                    wolf.transform.SetPositionAndRotation(PlayerNet.SeatPosition(i),Quaternion.Euler(0,180,0));wolf.transform.localScale=Vector3.one*1.5f;
                    var appearance=wolf.GetComponentInChildren<BBBWolfAppearance>();appearance.SetOutfit(i%3,i%2);appearance.SetTieColor(PlayerNet.Colors[(i+3)%6]);
                }
                var ring=GameObject.Find("Lobby Selection Ring");if(!ring){ring=GameObject.CreatePrimitive(PrimitiveType.Quad);ring.name="Lobby Selection Ring";UnityEngine.Object.DestroyImmediate(ring.GetComponent<Collider>());}
                ring.transform.rotation=Quaternion.Euler(90,0,0);ring.transform.localScale=Vector3.one*2;
                const string matPath=LobbyBuilder.Root+"/M_LobbySelectionRing.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,matPath);}
                material.SetTexture("_BaseMap",hud.library.Find("23_PlayerRing").texture);material.SetFloat("_Surface",1);material.SetFloat("_ZWrite",0);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_Cull",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;EditorUtility.SetDirty(material);
                ring.GetComponent<MeshRenderer>().sharedMaterial=material;hud.selectionRing=ring.transform;
                hud.BuildLayout();Canvas.ForceUpdateCanvases();hud.Refresh(true);EditorUtility.SetDirty(hud);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                CapturePreview(hud);
            } finally {if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static void CapturePreview(LobbyHud hud)
        {
            var canvas=hud.GetComponentInChildren<Canvas>();var oldMode=canvas.renderMode;
            var oldCamera=canvas.worldCamera;float oldDistance=canvas.planeDistance;float oldAspect=hud.view.aspect;
            var target=new RenderTexture(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try {
                hud.view.targetTexture=target;hud.view.aspect=16f/9f;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=hud.view;canvas.planeDistance=.5f;
                Canvas.ForceUpdateCanvases();hud.Refresh(true);Canvas.ForceUpdateCanvases();hud.view.Render();
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes("Library/LobbyHud-Layout.png",image.EncodeToPNG());
            } finally {
                RenderTexture.active=null;hud.view.targetTexture=null;hud.view.aspect=oldAspect;
                canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
                UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
                Canvas.ForceUpdateCanvases();hud.Refresh(true);
            }
        }
        static void Tick()
        {
            if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
            int stage=SessionState.GetInt("LobbyHudVerifyStage",0);
            try {
                if(!EditorApplication.isPlaying) {
                    if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                    if(stage!=0){SessionState.SetInt("LobbyHudVerifyStage",0);return;}
                    Build();
                    if(SceneManager.GetActiveScene().path!=LobbyBuilder.ScenePath) {
                        if(SceneManager.GetActiveScene().isDirty)throw new Exception("Current scene is dirty; HUD saved but play test needs an available editor scene.");
                        EditorSceneManager.OpenScene(LobbyBuilder.ScenePath);
                    }
                    SessionState.SetInt("LobbyHudVerifyStage",1);EditorApplication.isPlaying=true;next=EditorApplication.timeSinceStartup+3;return;
                }
                var hud=UnityEngine.Object.FindFirstObjectByType<LobbyHud>();
                if(stage==1){hud.room.StartHost();SessionState.SetInt("LobbyHudVerifyStage",2);next=EditorApplication.timeSinceStartup+3;return;}
                var player=UnityEngine.Object.FindFirstObjectByType<PlayerNet>();
                if(!player||!player.isLocalPlayer)throw new Exception("Host player missing.");
                if(stage==2){hud.SelectCategory(1);hud.ChangeVariant(1);SessionState.SetInt("LobbyHudVerifyStage",3);next=EditorApplication.timeSinceStartup+1;return;}
                if(stage==3){if(player.bottom!=1||hud.SelectedCategory!=1)throw new Exception("Category / appearance command failed.");hud.ToggleReady();SessionState.SetInt("LobbyHudVerifyStage",4);next=EditorApplication.timeSinceStartup+1;return;}
                if(stage==4){if(!player.ready)throw new Exception("Ready command failed.");hud.ToggleOptions();if(!hud.OptionsOpen)throw new Exception("Options failed.");hud.ToggleOptions();ScreenCapture.CaptureScreenshot("Library/LobbyHud-Host.png");SessionState.SetInt("LobbyHudVerifyStage",5);next=EditorApplication.timeSinceStartup+2;return;}
                if(stage==5){hud.room.StopHost();File.WriteAllText("Library/LobbyHud.result","OK: HUD saved, host spawn, keyboard category API, appearance command, ready command, options open/close and disconnect. Capture: Library/LobbyHud-Host.png");File.Delete(Request);SessionState.SetInt("LobbyHudVerifyStage",0);EditorApplication.isPlaying=false;}
            } catch(Exception ex){File.WriteAllText("Library/LobbyHud.result",ex.ToString());File.Delete(Request);SessionState.SetInt("LobbyHudVerifyStage",0);if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;Debug.LogException(ex);}
        }
    }
}
