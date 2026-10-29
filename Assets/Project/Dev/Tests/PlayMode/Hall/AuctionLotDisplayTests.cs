using System.Collections;
using BuyBidBlow.Common;
using BuyBidBlow.Props;
using BuyBidBlow.Systems.Match;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class AuctionLotDisplayTests
    {
        readonly HostedMatch match = new HostedMatch();
        [UnityTearDown] public IEnumerator Cleanup() { yield return match.Stop(); }

        [UnityTest] public IEnumerator PedestalEnvelopeFollowsExistingPublicAuctionState()
        {
            yield return match.Start(0,settings=>
            {
                settings.RoundCount=5;settings.RoundSeconds=2;settings.AuctionRoundSeconds=5;
                settings.AuctionRounds=new[]{2};settings.AuctionDurationSeconds=2;
                settings.AuctionSlots=new[]{new AuctionSlot{StartsAtSeconds=0,DurationSeconds=2},new AuctionSlot{StartsAtSeconds=2,DurationSeconds=2}};
            });
            var client=Object.FindFirstObjectByType<AuctionLotDisplayClient>();Assert.IsNotNull(client);
            var display=client.display;Assert.IsNotNull(display);
            Assert.IsTrue(client.gameObject.activeInHierarchy,"binding parent stays active");
            Assert.IsFalse(display.gameObject.activeSelf,"hidden outside auction");
            Assert.AreEqual(0,client.GetComponents<NetworkBehaviour>().Length);
            Assert.AreEqual(0,display.GetComponentsInChildren<NetworkIdentity>(true).Length,"envelope is not a network object");
            yield return HostedMatch.Until(()=>MatchNet.Current.auctionStage==MatchNet.AuctionStage.Open,"open auction",5);
            yield return null;
            Assert.IsTrue(display.gameObject.activeInHierarchy);
            AssertLabel(display);
            var rotation=display.rotatingModel.rotation;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.Greater(Quaternion.Angle(rotation,display.rotatingModel.rotation),1,"existing display rotates");
            client.enabled=false;Assert.IsFalse(display.gameObject.activeSelf);
            client.enabled=true;Assert.IsTrue(display.gameObject.activeSelf,"late enable reads current state immediately");
            yield return HostedMatch.Until(()=>MatchNet.Current.auctionStage==MatchNet.AuctionStage.Secret,"secret auction",5);
            yield return null;
            Assert.IsTrue(display.gameObject.activeSelf);AssertLabel(display);
            yield return HostedMatch.Until(()=>MatchNet.Current.auctionStage==MatchNet.AuctionStage.None,"auction closed",5);
            yield return null;
            Assert.IsFalse(display.gameObject.activeSelf,"stage None hides the envelope and cloche");
            Assert.IsTrue(client.gameObject.activeInHierarchy);
            yield return HostedMatch.Until(()=>MatchNet.Current.phase==MatchNet.Phase.Finished,"match finished",10);
            yield return null;Assert.IsFalse(display.gameObject.activeSelf,"finished match stays hidden");
        }
        static void AssertLabel(AuctionEnvelopeDisplay display)
        {
            var lot=MatchNet.Current.auctionLot;
            Assert.AreEqual(lot.firstRound,display.firstRound);Assert.AreEqual(lot.secondRound,display.secondRound);
            Assert.AreEqual($"{lot.firstRound}R · {lot.secondRound}R",display.roundLabel.text);
        }
    }
}
