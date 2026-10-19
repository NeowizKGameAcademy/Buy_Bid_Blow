using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BuyBidBlow.UI.Shared;
using BuyBidBlow.UI.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad] public static class UIShowcaseVerification
    {
        private const string Flag="UIShowcaseQA";
        private static UIShowcase demo;
        private static readonly Queue<Action> steps=new Queue<Action>();
        private static double next;
        private static int requests;
        private static bool initialized;
        private static readonly List<string> passed=new List<string>();
        static UIShowcaseVerification(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Flag+"Restore",false)){var path=SessionState.GetString(Flag+"Previous","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);SessionState.SetBool(Flag+"Restore",false);}};}
        [MenuItem("Tools/Buy Bid Blow/UI/Verify UI Showcase (Play)")]
        public static void Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before running UI verification.");
            SessionState.SetString(Flag+"Previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));SessionState.SetBool(Flag+"Restore",true);
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(UIShowcaseBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;
        }
        private static void Check(bool value,string label){if(!value)throw new Exception(label);passed.Add(label);}
        private static void Key(Key key){steps.Enqueue(()=>InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key)));steps.Enqueue(()=>InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState()));}
        private static void Initialize()
        {
            initialized=true;demo=UnityEngine.Object.FindFirstObjectByType<UIShowcase>();if(!demo)throw new Exception("Showcase scene missing");
            Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();
            steps.Enqueue(()=>{for(int i=0;i<demo.Count;i++)demo.ShowCase(i);Check(demo.Count==21,"All 21 display states open");demo.ShowCase(0);demo.Error.ActionRequested+=i=>requests++;});
            Key(UnityEngine.InputSystem.Key.Enter);
            steps.Enqueue(()=>Check(requests==1&&!demo.Error.IsOpen,"Enter submits exactly one action"));
            steps.Enqueue(()=>{demo.ShowCase(6);requests=0;demo.LeaveConfirm.ActionRequested+=i=>{if(i==0)requests++;};});
            Key(UnityEngine.InputSystem.Key.Escape);
            steps.Enqueue(()=>Check(requests==1&&!demo.LeaveConfirm.IsOpen,"Escape selects safe cancel"));
            steps.Enqueue(()=>{demo.ShowCase(10);requests=0;demo.Results.ActionRequested+=i=>requests++;});
            Key(UnityEngine.InputSystem.Key.Enter);
            steps.Enqueue(()=>{Check(requests==0&&demo.Results.IsOpen,"Disabled return cannot submit");Check(demo.Results.Rows.Count(r=>r.activeSelf)==6,"Six result rows");Check(demo.Results.CashLabels[0].text=="—"&&demo.Results.CashLabels[1].text!="—","Private result breakdown hidden");Check(demo.Results.Winner.text.Contains("루나")&&demo.Results.Winner.text.Contains("모카"),"Joint winners shown");demo.ShowCase(9);Check(demo.Results.Rows.Count(r=>r.activeSelf)==3,"Three result rows");});
            steps.Enqueue(()=>{demo.ShowCase(13);Check(!demo.Auction.Body.text.Contains("루나")&&demo.Auction.Body.text.Contains("$40"),"Secret result contains only own amount");demo.ShowCase(14);Check(!demo.Auction.Body.text.Contains("25"),"Secret unsuccessful result exposes no bid amounts");});
            steps.Enqueue(()=>demo.ShowCase(18));
            Key(UnityEngine.InputSystem.Key.RightArrow);
            steps.Enqueue(()=>Check(demo.Settings.Draft.Master==85,"Settings keyboard adjustment"));
            Key(UnityEngine.InputSystem.Key.Escape);
            steps.Enqueue(()=>{Check(!demo.Settings.IsOpen&&demo.Settings.Draft.Master==80,"Settings discard on Escape");demo.ShowCase(18);requests=0;demo.Settings.ApplyRequested+=v=>requests++;demo.Settings.Adjust(1);demo.Settings.ApplyDraft();Check(requests==1&&demo.Settings.Draft.Master==85,"Settings emits apply data");});
            steps.Enqueue(()=>{demo.ShowCase(16);demo.MarketStack.Clear();for(int i=0;i<3;i++)demo.MarketStack.Push("short"+i,"detail",NoticeTone.Neutral,.1f);demo.MarketStack.Push("queued","detail",NoticeTone.Success,5);Check(demo.MarketStack.ActiveCount==3&&demo.MarketStack.QueuedCount==1,"Notification overflow queues");});
            steps.Enqueue(()=>{});steps.Enqueue(()=>Check(demo.MarketStack.ActiveCount==1&&demo.MarketStack.Titles[0].text=="queued","Expired notifications advance queue"));
            steps.Enqueue(()=>{demo.MarketStack.Clear();for(int i=0;i<30;i++)demo.MarketStack.Push("bounded","detail",NoticeTone.Neutral,10);Check(demo.MarketStack.QueuedCount<=12,"Notification queue bounded");demo.MarketStack.Clear();demo.Round.Show(1,17,false,.1f);});
            steps.Enqueue(()=>{});steps.Enqueue(()=>Check(!demo.Round.gameObject.activeSelf,"Round banner expires"));
            steps.Enqueue(()=>{demo.ShowCase(8);Check(Mathf.Abs(demo.Loading.ProgressFill.fillAmount-.62f)<.001f,"Loading progress supplied by caller");demo.ShowCase(3);demo.Connection.Cancel();Check(demo.Connection.IsOpen,"Disconnect cannot dismiss with Escape");});
            steps.Enqueue(()=>{demo.ShowCase(6);var button=demo.LeaveConfirm.Buttons[0];demo.LeaveConfirm.Hide();EventSystem.current.SetSelectedGameObject(button.gameObject);demo.Error.SetButtons("확인");demo.Error.Show("focus","test");demo.Error.Hide();/* inactive objects intentionally aren't restored */Check(EventSystem.current.currentSelectedGameObject!=button.gameObject,"Inactive previous focus is not restored");});
            steps.Enqueue(()=>{var report="PASS\n"+string.Join("\n",passed)+"\n";File.WriteAllText(UIShowcaseBuilder.Root+"/PlayVerification.txt",report);Finish();});
        }
        private static void Finish(){SessionState.SetBool(Flag,false);initialized=false;steps.Clear();EditorApplication.isPlaying=false;AssetDatabase.Refresh();}
        private static void Tick()
        {
            if(File.Exists("Library/UIShowcaseQA.request")&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlaying){File.Delete("Library/UIShowcaseQA.request");Run();return;}
            if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
            next=EditorApplication.timeSinceStartup+.22;
            try{if(!initialized){Initialize();return;}if(steps.Count>0)steps.Dequeue()();}
            catch(Exception e){File.WriteAllText(UIShowcaseBuilder.Root+"/PlayVerification.txt","FAIL\n"+e);Finish();Debug.LogException(e);}
        }
    }
}
