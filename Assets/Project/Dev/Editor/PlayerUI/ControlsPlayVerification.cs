using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace BuyBidBlow.PlayerUISandbox.Editor {
[InitializeOnLoad] public static class ControlsPlayVerification {
static bool pending=SessionState.GetBool("ControlsCheck",false);static int phase;static double began=SessionState.GetFloat("ControlsStarted",0);static float phaseTime,peak;static PlayerUIController player;
static ControlsPlayVerification(){EditorApplication.update+=Update;}
static void Update(){if(!pending){if(!File.Exists("Library/ControlsPlay.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;File.Delete("Library/ControlsPlay.request");SessionState.SetBool("ControlsCheck",true);pending=true;began=EditorApplication.timeSinceStartup;SessionState.SetFloat("ControlsStarted",(float)began);EditorApplication.isPlaying=true;return;}
if(EditorApplication.timeSinceStartup-began>40){Finish("FAIL: physics test timed out");return;}if(!EditorApplication.isPlaying)return;EditorApplication.QueuePlayerLoopUpdate();
try{if(phase==0){player=UnityEngine.Object.FindFirstObjectByType<PlayerUIController>();if(!player||!player.bridge||player.bridge.Game==null)return;Application.runInBackground=true;player.enabled=false;var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Physics verification floor";floor.transform.position=new Vector3(0,49.5f,0);floor.transform.localScale=new Vector3(20,1,20);player.body.position=new Vector3(0,50.02f,0);player.body.rotation=Quaternion.identity;player.body.linearVelocity=Vector3.zero;player.movement.enabled=true;player.movement.SetMoveInput(Vector2.zero);Physics.SyncTransforms();phase=1;phaseTime=Time.time;return;}
if(phase==1&&Time.time-phaseTime>.3f){player.movement.SetJumpHeld(true);phase=2;phaseTime=Time.time;peak=50;}
if(phase==2){player.movement.SetJumpHeld(true);peak=Mathf.Max(peak,player.body.position.y);if(Time.time-phaseTime>2.7f){if(peak<50.7f||peak>52||player.body.position.y>50.2f)throw new Exception("Jump height or held-key auto-jump: peak="+peak+" landed="+player.body.position.y);player.movement.SetJumpHeld(false);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Physics verification wall";wall.transform.position=new Vector3(1.3f,51,0);wall.transform.localScale=new Vector3(.5f,4,6);player.body.position=new Vector3(.74f,52,0);player.body.linearVelocity=Vector3.zero;player.movement.SetMoveInput(Vector2.right);Physics.SyncTransforms();phase=3;phaseTime=Time.time;}}
if(phase==3&&Time.time-phaseTime>.2f){player.movement.SetJumpHeld(true);phase=4;phaseTime=Time.time;}
if(phase==4&&Time.time-phaseTime>.12f){if(player.body.linearVelocity.y>0)throw new Exception("Wall contact incorrectly allowed a jump");Finish("PASS: actual physics jump, landing while holding Space does not jump again; wall-side contact cannot jump. Diagonal speed and UI restore checked by MatchPlayVerification.");}
}catch(Exception e){Debug.LogException(e);Finish("FAIL: "+e);}}
static void Finish(string message){File.WriteAllText("Assets/Project/Dev/Sandbox/Interface/PlayerUI/ControlsPlayVerification.txt",message);File.WriteAllText("Library/ControlsPlay.result",message);SessionState.SetBool("ControlsCheck",false);pending=false;phase=0;EditorApplication.isPlaying=false;}
}
}