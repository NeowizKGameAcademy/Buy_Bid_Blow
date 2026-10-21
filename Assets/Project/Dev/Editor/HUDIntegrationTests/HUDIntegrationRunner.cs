using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad]
    public static class HUDIntegrationRunner
    {
        static TestRunnerApi api;
        static HUDIntegrationRunner(){api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new Report());EditorApplication.update+=Tick;}
        static void Tick()
        {
            foreach(var kind in new[]{"Capture","Review"})
            {
                string request="Library/HowToPlay"+kind+"Tests.request";
                if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)continue;
                File.Delete(request);
                if(kind=="Capture")File.WriteAllText("Library/HowToPlayCapture.enabled","capture");
                string method=kind=="Capture"?"CaptureCurrentGameExamples":"MainMenuPagesMatchCurrentRules";
                api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,testNames=new[]{"BuyBidBlow.Dev.Tests.PlayMode.HowToPlayReviewTests."+method}}));return;
            }
            if(File.Exists("Library/NewAudioTests.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if(EditorUserBuildSettings.standaloneBuildSubtarget!=StandaloneBuildSubtarget.Player){EditorUserBuildSettings.standaloneBuildSubtarget=StandaloneBuildSubtarget.Player;return;}
#if UNITY_SERVER
                File.Delete("Library/NewAudioTests.request");
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                return;
#endif
                File.Delete("Library/NewAudioTests.request");
                api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,testNames=new[]{"BuyBidBlow.Dev.Tests.PlayMode.MatchAudioTests.NewNotebookAwardAndRejectionSounds"}}));return;
            }
            if(File.Exists("Library/TradeRefreshTests.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete("Library/TradeRefreshTests.request");
                api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,testNames=new[]{"BuyBidBlow.Dev.Tests.PlayMode.TradeRefreshTests"}}));return;
            }
            if(File.Exists("Library/DedicatedRulesTests.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if(EditorUserBuildSettings.standaloneBuildSubtarget!=StandaloneBuildSubtarget.Player){EditorUserBuildSettings.standaloneBuildSubtarget=StandaloneBuildSubtarget.Player;return;}
                File.Delete("Library/DedicatedRulesTests.request");
                api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,testNames=new[]{"BuyBidBlow.Dev.Tests.PlayMode.DedicatedRulesTests.BuiltServerPublishesThirteenRoundsAndThreeMinuteAuction"}}));
                return;
            }
            if(File.Exists("Library/AudioRulesTests.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete("Library/AudioRulesTests.request");
                api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,testNames=new[]{"BuyBidBlow.Dev.Tests.PlayMode.MatchAudioTests.ProductionClockAndActualJumpUseCurrentSettings","BuyBidBlow.Dev.Tests.PlayMode.UpdatedMatchRulesTests"}}));
                return;
            }
            if(!File.Exists("Library/HUDIntegrationTests.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete("Library/HUDIntegrationTests.request");
            api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,testNames=new[]{"BuyBidBlow.Dev.Tests.PlayMode.MatchFeedbackTests","BuyBidBlow.Dev.Tests.PlayMode.LocalPlayerHUDTests"}}));
        }
        sealed class Report:ICallbacks
        {
            public void RunStarted(ITestAdaptor test){}
            public void TestStarted(ITestAdaptor test){}
            public void TestFinished(ITestResultAdaptor result){}
            public void RunFinished(ITestResultAdaptor result){TestRunnerApi.SaveResultToFile(result,"Logs/HUDIntegrationTests.xml");File.WriteAllText("Library/HUDIntegrationTests.result",result.ResultState+"\n"+result.Message);}
        }
    }
}

