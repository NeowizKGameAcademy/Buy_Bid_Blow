using System.Collections;
using System.Linq;
using BuyBidBlow.Audio;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Match;
using CueAudio;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class MatchAudioTests
    {
        readonly HostedMatch match=new HostedMatch();
        [UnityTest]public IEnumerator ProductionClockAndActualJumpUseCurrentSettings()
        {
#if UNITY_SERVER
            Assert.Ignore("Client audio only.");
#endif
            yield return match.Start();
            Assert.AreEqual(13,MatchNet.Current.totalRounds);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var engine=(BuyBidBlow.Systems.Match.MatchEngine)typeof(BuyBidBlow.Network.GameNetworkManager).GetField("engine",flags).GetValue(match.Room);
            Assert.AreEqual(0,(int)typeof(BuyBidBlow.Network.GameNetworkManager).GetField("roundSecondsOverride",flags).GetValue(match.Room));
            double firstClose=engine.Deadline;
            engine.Tick(firstClose);
            Assert.AreEqual(2,engine.Round);
            Assert.AreEqual(180,engine.Deadline-firstClose,.001);
            var local=NetworkClient.localPlayer.GetComponent<BuyBidBlow.Characters.PlayerNet>();
            var move=local.GetComponent<BuyBidBlow.Characters.PlayerMove>();
            yield return HostedMatch.Until(()=>move.IsGrounded,"player grounded");
            var audio=GameAudio.EnsureHost().Engine;int before=Count(audio,"player.jump");
            move.SetJumpHeld(true);move.SetJumpHeld(false);
            yield return HostedMatch.Until(()=>Count(audio,"player.jump")==before+1,"actual jump RPC sound");
            move.SetJumpHeld(true);move.SetJumpHeld(false);yield return null;yield return null;
            Assert.AreEqual(before+1,Count(audio,"player.jump"),"airborne input must not replay jump sound");
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            yield return match.Stop();
            if(CueAudioHost.Instance)Object.Destroy(CueAudioHost.Instance.gameObject);
            yield return null;
        }
        [UnityTest]public IEnumerator ReplicatedStatePlaysEachTransitionOnceAndActionHooksStaySeparate()
        {
#if UNITY_SERVER
            Assert.Ignore("Run audio playback verification with the Player subtarget; dedicated servers intentionally disable CueAudio.");
#endif
            yield return match.Start(0,settings=>
            {
                settings.RoundCount=5;settings.RoundSeconds=2;settings.AuctionRoundSeconds=6;settings.AuctionRounds=new[]{2};settings.AuctionDurationSeconds=2;
                settings.AuctionSlots=new[]{new AuctionSlot{StartsAtSeconds=0,DurationSeconds=2},new AuctionSlot{StartsAtSeconds=2,DurationSeconds=2}};
            });
            var observer=Object.FindFirstObjectByType<MatchAudioClient>();Assert.IsNotNull(observer);
            yield return null;Assert.IsNotNull(CueAudioHost.Instance);
            var audio=CueAudioHost.Instance.Engine;Assert.IsNotNull(audio);Assert.IsEmpty(CueAudioHost.Instance.Catalog.Validate());
            Assert.AreEqual(0,Count(audio,"match.round_start"));
            observer.enabled=false;observer.enabled=true;yield return null;
            Assert.AreEqual(0,Count(audio,"match.round_start"),"no temporary price sound at round start");
            yield return HostedMatch.Until(()=>MatchNet.Current.phase==MatchNet.Phase.Finished,"match audio finish",20);yield return null;
            Assert.AreEqual(0,Count(audio,"match.round_start"));Assert.AreEqual(5,Count(audio,"match.closing_bell"));
            Assert.AreEqual(2,Count(audio,"match.auction_start"),"public + secret slots");Assert.AreEqual(2,Count(audio,"match.auction_end"));
            Assert.AreEqual(2,Count(audio,"match.closing_soon"),"once per slot even when short test slots start below 10 seconds");
            Assert.AreEqual(1,Count(audio,"bgm.game"));
            Assert.IsFalse(audio.History.Any(r=>r.Result!="Playing"),"all cue requests accepted");
            Assert.AreEqual(0,Count(audio,"player.footstep"));Assert.AreEqual(0,Count(audio,"player.jump"),"action hooks are not automatically driven yet");
            var action=NetworkClient.localPlayer.GetComponent<PlayerActionAudio>();Assert.IsNotNull(action);
            action.PlayJump();action.PlayFootstep();action.PlayEmotion();
            Assert.AreEqual(1,Count(audio,"player.jump"));Assert.AreEqual(1,Count(audio,"player.footstep"));
            Assert.IsTrue(audio.GetActiveVoices().Where(v=>v.CueId.StartsWith("player.")).All(v=>v.Source.spatialBlend==1));
            int count=audio.History.Count;yield return null;yield return null;Assert.AreEqual(count,audio.History.Count,"unchanged finished state stays silent");
            Assert.AreEqual(0,observer.GetComponents<NetworkBehaviour>().Length);
        }
        [UnityTest]public IEnumerator MenuMusicTransitionsToGameAndUIEventsUseCollectedClips()
        {
#if UNITY_SERVER
            Assert.Ignore("Client audio only.");
#endif
            UnityEngine.SceneManagement.SceneManager.LoadScene("SC_MainMenu");yield return null;yield return null;
            var host=GameAudio.EnsureHost();Assert.IsNotNull(host);var audio=host.Engine;
            Assert.AreEqual(1,Count(audio,"bgm.menu"));
            var how=Object.FindFirstObjectByType<BuyBidBlow.UI.Menu.MainMenuActions>();
            how.HowToPlay();yield return null;
            var pages=how.howToPlayPanel.GetComponent<BuyBidBlow.UI.Menu.HowToPlayPanel>();
            pages.Turn(1);Assert.AreEqual(1,Count(audio,"ui.page"));
            yield return null;how.CloseHow();
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            var button=how.howToPlayButton;UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
            Assert.GreaterOrEqual(Count(audio,"ui.navigate"),1);
            yield return match.Start();yield return new WaitForSecondsRealtime(1);
            Assert.AreSame(host,CueAudioHost.Instance);Assert.AreEqual(1,Count(audio,"bgm.game"));
            Assert.AreEqual(1,audio.GetActiveVoices().Count(v=>v.CueId.StartsWith("bgm.")),"old menu music faded out");
            var player=NetworkClient.localPlayer.GetComponent<BuyBidBlow.Characters.PlayerNet>();var notebook=player.GetComponentInChildren<BuyBidBlow.UI.Notebook.NetworkNotebookController>(true);
            notebook.Open();yield return null;notebook.view.Scroll(1);yield return null;notebook.Close();
            Assert.AreEqual(2,Count(audio,"ui.notebook"));Assert.GreaterOrEqual(Count(audio,"ui.page"),2);
            var trader=player.GetComponent<LocalTradeController>();var trade=player.GetComponentInChildren<BuyBidBlow.UI.Market.TradePanelController>(true);
            trader.OpenPanel(BuyBidBlow.Systems.Types.Company.Straw);yield return null;trade.IncreaseQuantity();yield return null;trade.Confirm();
            yield return HostedMatch.Until(()=>Count(audio,"market.trade")==1,"server confirmed trade sound");
            yield return null;trade.SelectTab(BuyBidBlow.UI.Market.TradePanelController.Tab.DownsideBet);yield return null;trade.Confirm();
            yield return HostedMatch.Until(()=>Count(audio,"market.bet")==1,"server confirmed bet sound");
            yield return null;trade.Close();Assert.GreaterOrEqual(Count(audio,"ui.close"),1);Assert.GreaterOrEqual(Count(audio,"ui.quantity"),1);Assert.GreaterOrEqual(Count(audio,"ui.confirm"),2);
            Assert.IsFalse(audio.History.Any(r=>r.Result!="Playing"));
        }
        static int Count(CueAudioEngine engine,string id)=>engine.History.Count(r=>r.CueId==id&&r.Result=="Playing");
    }
}
