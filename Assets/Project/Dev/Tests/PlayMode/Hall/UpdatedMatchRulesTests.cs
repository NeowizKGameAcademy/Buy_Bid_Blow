using System.Collections;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Notebook;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class UpdatedMatchRulesTests
    {
        readonly HostedMatch match=new HostedMatch();Keyboard keyboard;InputSettings originalSettings;
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            if(originalSettings)InputSystem.settings=originalSettings;
            yield return match.Stop();
        }
        [UnityTest]public IEnumerator ProductionRulesAndThirteenRoundServerMatch()
        {
            var rules=JsonUtility.FromJson<MatchSettings>(System.IO.File.ReadAllText("Assets/Project/Content/Match/MatchSettings.json"));rules.Validate();
            Assert.AreEqual(13,rules.RoundCount);Assert.AreEqual(120,rules.AuctionRoundSeconds);
            CollectionAssert.AreEqual(Enumerable.Range(1,13).ToArray(),rules.AuctionRounds);
            Assert.IsTrue(rules.SingleAuctionPerRound);
            var schedule=BuyBidBlow.Systems.Round.MatchSchedule.Build(rules,0,0);
            foreach(var span in schedule.Where(s=>s.Kind==BuyBidBlow.Systems.Round.TimelineSpanKind.SecretAuction))
                Assert.AreEqual(60,span.EndsAt-span.StartsAt,.001,"replicated secret auction countdown");
            foreach(int count in new[]{3,6})
            {
                var players=Enumerable.Range(0,count).Select(i=>new PlayerId("player"+i)).ToArray();
                var engine=new MatchEngine(rules,20261020,players,0);int expected=count*10+10;
                Assert.AreEqual(expected,rules.GetInitialCompanySupply(count));
                foreach(Company company in System.Enum.GetValues(typeof(Company)))Assert.AreEqual(expected,engine.Stock(company));
                double previous=0;
                for(int round=1;round<=13;round++)
                {
                    Assert.AreEqual(round,engine.Round);Assert.AreEqual(120,engine.Deadline-previous,.001);
                    if(engine.Auction!=null)
                    {
                        bool secret=round%2==0;
                        Assert.AreEqual(60,(secret?engine.Auction.Secret.Deadline:engine.Auction.Open.Deadline)-previous,.001);
                        engine.Tick(previous+60);Assert.IsTrue(engine.Auction.IsFinished);
                        Assert.AreEqual(60,engine.Deadline-(previous+60),.001,"trading time after the one auction");
                    }
                    previous=engine.Deadline;engine.Tick(previous);
                }
                Assert.IsTrue(engine.Complete);Assert.AreEqual(13,engine.Round);Assert.AreEqual(1560,previous,"26 minute match");
            }
            // Same production JSON, only server clock accelerated for the end-to-end run.
            yield return match.Start(1,port:27999);
            Assert.AreEqual(13,MatchNet.Current.totalRounds);
            foreach(Company company in System.Enum.GetValues(typeof(Company)))Assert.AreEqual(20,MatchNet.Current.GetStock(company),"one actual test participant ×10+10");
            var local=NetworkClient.localPlayer.GetComponent<PlayerNet>();
            var notebook=HostedMatch.Body.GetComponentInChildren<NetworkNotebookController>(true);var hud=HostedMatch.Body.GetComponentInChildren<NetworkPlayerHUD>(true);
            Assert.IsTrue(hud.roundLabel.text.Contains("/ 13"));
            Assert.IsTrue(notebook.view.GetComponentsInChildren<UnityEngine.UI.Text>(true).Any(t=>t.name=="Keyboard Guide"&&t.text.Contains("Q / Tab")));
            originalSettings=InputSystem.settings;var inputSettings=Object.Instantiate(originalSettings);
            inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=inputSettings;
            keyboard=InputSystem.AddDevice<Keyboard>();
            yield return Press(Key.Tab);Assert.IsTrue(notebook.view.IsOpen);
            yield return Press(Key.Q);Assert.IsFalse(notebook.view.IsOpen);
            yield return Press(Key.Tab);Assert.IsTrue(notebook.view.IsOpen);
            yield return Press(Key.Tab);Assert.IsFalse(notebook.view.IsOpen);
            yield return HostedMatch.Until(()=>PlayerNet.LocalResults!=null,"13 round final result",20);
            var feedback=HostedMatch.Body.GetComponentInChildren<NetworkMatchFeedback>(true);yield return null;
            Assert.IsTrue(feedback.results.IsOpen);Assert.AreEqual("13 / 13 ROUND",feedback.results.Body.text);
            Assert.AreEqual(13,MatchNet.Current.round);Assert.AreEqual(MatchNet.Phase.Finished,MatchNet.Current.phase);
        }
        IEnumerator Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;}
    }
}
