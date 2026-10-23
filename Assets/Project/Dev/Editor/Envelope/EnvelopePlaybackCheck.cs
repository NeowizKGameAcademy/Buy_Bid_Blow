using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BuyBidBlow.Props;
namespace BuyBidBlow.AuctionDisplay.Editor {
 [InitializeOnLoad] public static class EnvelopePlaybackCheck {
 static double next;static float startedTime;static Quaternion before;static AuctionEnvelopeDisplay item;
 static EnvelopePlaybackCheck(){EditorApplication.update+=Tick;next=EditorApplication.timeSinceStartup+1;}
 static void Tick(){
 if(File.Exists("Library/EnvelopePlayback.request")&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode){File.Delete("Library/EnvelopePlayback.request");SessionState.SetInt("EnvelopePlayback",1);EditorApplication.isPlaying=true;return;}
 if(EditorApplication.isPlaying)EditorApplication.QueuePlayerLoopUpdate();int stage=SessionState.GetInt("EnvelopePlayback",0);if(stage==0||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
 try{if(stage==1){item=UnityEngine.Object.FindFirstObjectByType<AuctionEnvelopeDisplay>();if(!item||!item.roundLabel||item.roundLabel.text!="6R · 9R")throw new Exception("Missing envelope/text");before=item.rotatingModel.rotation;startedTime=Time.unscaledTime;next=EditorApplication.timeSinceStartup+1;SessionState.SetInt("EnvelopePlayback",2);}
 else if(stage==2){if(Time.unscaledTime-startedTime<1)return;float angle=Quaternion.Angle(before,item.rotatingModel.rotation);if(angle<1)throw new Exception("Envelope not rotating");File.WriteAllText("Library/EnvelopePlayback.result","PASS: actual Play-mode rotation "+angle.ToString("F1")+" degrees; text 6R · 9R visible component assigned.");File.AppendAllText("Assets/Project/Dev/Sandbox/Interface/Envelope/Verification.txt","\nPLAY PASS: rotating transform advances; round label initialized.\n");SessionState.SetInt("EnvelopePlayback",0);EditorApplication.isPlaying=false;}
 }catch(Exception ex){SessionState.SetInt("EnvelopePlayback",0);File.WriteAllText("Library/EnvelopePlayback.result",ex.ToString());EditorApplication.isPlaying=false;}
 }
 }
}


