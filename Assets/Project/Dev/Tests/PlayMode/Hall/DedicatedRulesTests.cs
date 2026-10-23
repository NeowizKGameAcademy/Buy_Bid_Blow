using System.Collections;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.UI.HUD;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    // Opt-in: requires the locally built dedicated server on the default port, -minPlayers 1.
    public sealed class DedicatedRulesTests
    {
        GameNetworkManager room;
        [UnityTearDown] public IEnumerator Cleanup(){if(room){room.StopClient();yield return null;if(room)Object.Destroy(room.gameObject);}yield return null;}
        [UnityTest]
        public IEnumerator BuiltServerPublishesThirteenRoundsAndThreeMinuteAuction()
        {
            if(!System.IO.File.Exists("Library/DedicatedRulesTests.enabled"))Assert.Ignore("Requires the rebuilt local dedicated server");
            SceneManager.LoadScene(GameNetworkManager.PlayScene);yield return null;
            room=Object.FindFirstObjectByType<GameNetworkManager>();room.networkAddress="127.0.0.1";room.LocalNickname="RuleTester";room.AdmissionRejected+=reason=>Assert.Fail(reason);room.StartClient();
            yield return HostedMatch.Until(()=>NetworkClient.localPlayer,"dedicated admission");
            var player=NetworkClient.localPlayer.GetComponent<PlayerNet>();player.SetReady(true);
            yield return HostedMatch.Until(()=>player.ready,"dedicated ready");player.CmdStartMatch();
            yield return HostedMatch.Until(()=>MatchNet.Current&&MatchNet.Current.phase==MatchNet.Phase.Playing,"dedicated start");
            Assert.AreEqual(13,MatchNet.Current.totalRounds);
            Assert.AreEqual(60,MatchNet.Current.roundClosesAt-MatchNet.Current.firstRoundStartsAt,.001);
            double firstClose=MatchNet.Current.roundClosesAt;yield return null;
            Assert.IsTrue(player.GetComponentInChildren<NetworkPlayerHUD>(true).roundLabel.text.Contains("/ 13"));
            yield return HostedMatch.Until(()=>MatchNet.Current.round==2,"unaccelerated auction round",65);
            Assert.AreEqual(120,MatchNet.Current.roundClosesAt-firstClose,.001);
            Assert.AreEqual(MatchNet.AuctionStage.Open,MatchNet.Current.auctionStage);
        }
    }
}




