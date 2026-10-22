using System.Collections;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Match;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class AuctioneerAnimationTests
    {
        readonly HostedMatch match=new HostedMatch();GameObject observer;
        [UnityTearDown]public IEnumerator Cleanup(){if(observer)Object.Destroy(observer);yield return match.Stop();}
        [UnityTest]public IEnumerator AuctionActionsPlayOnceAndObserversUseTheSamePublicState()
        {
            yield return match.Start(0,s=>{s.RoundCount=5;s.RoundSeconds=2;s.AuctionRounds=new[]{2};s.AuctionRoundSeconds=38;s.AuctionDurationSeconds=16;s.AuctionSlots=new[]{new AuctionSlot{StartsAtSeconds=0,DurationSeconds=16},new AuctionSlot{StartsAtSeconds=16,DurationSeconds=16}};},27993);
            var actor=Object.FindFirstObjectByType<AuctioneerAnimationClient>();Assert.IsNotNull(actor);
            Assert.IsFalse(actor.animator.applyRootMotion);
            foreach(var clip in new[]{actor.talking,actor.counting,actor.thankful,actor.bow}){Assert.IsNotNull(clip);Assert.IsFalse(clip.isLooping);}
            var position=actor.transform.position;observer=Object.Instantiate(actor.gameObject);observer.name="Independent observer presentation";
            var other=observer.GetComponent<AuctioneerAnimationClient>();
            yield return HostedMatch.Until(()=>actor.CurrentAction=="Talking","auction opening gesture",8);
            Assert.AreEqual("Talking",other.CurrentAction);
            var arm=actor.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:RightArm");var rotation=arm.localRotation;
            yield return new WaitForSeconds(.5f);Assert.Greater(Quaternion.Angle(rotation,arm.localRotation),.1f,"Talking moves the Black model bones");
            var player=NetworkClient.localPlayer.GetComponent<PlayerNet>();var seat=player.GetComponent<SeatController>();
            seat.RequestSit(Object.FindObjectsByType<BuyBidBlow.Props.Seat>(FindObjectsSortMode.None).First(s=>s.auction));
            yield return HostedMatch.Until(()=>seat.IsSeated,"auction chair");player.CmdBid(1,false);
            yield return HostedMatch.Until(()=>actor.CurrentAction=="Thankful","public bid reaction",9);Assert.AreEqual("Thankful",other.CurrentAction);
            yield return HostedMatch.Until(()=>actor.CurrentAction=="Counting","public winning outcome",16);Assert.AreEqual("Counting",other.CurrentAction);
            yield return HostedMatch.Until(()=>actor.CurrentAction=="Talking","secret introduction after public thanks",6);Assert.AreEqual(MatchNet.AuctionStage.Secret,MatchNet.Current.auctionStage);
            // Even a private success outcome must not change this public character's action.
            player.TargetAuctionOutcome(new AuctionOutcome{Secret=true,Sold=true,Won=true,Amount=1});yield return null;
            Assert.AreEqual("Talking",actor.CurrentAction);Assert.AreEqual("Talking",other.CurrentAction);
            yield return HostedMatch.Until(()=>actor.CurrentAction=="Thankful","secret closing warning",9);
            yield return HostedMatch.Until(()=>MatchNet.Current.auctionStage==MatchNet.AuctionStage.None,"both auctions ended",10);yield return null;
            Assert.AreEqual("Counting",actor.CurrentAction);Assert.AreEqual("Counting",other.CurrentAction);
            yield return HostedMatch.Until(()=>MatchNet.Current.phase==MatchNet.Phase.Finished,"match end",15);
            yield return HostedMatch.Until(()=>actor.CurrentAction=="Bow","match ending gesture",2);
            yield return HostedMatch.Until(()=>actor.CurrentAction=="Idle","one shot returns to idle",7);
            yield return new WaitForSeconds(.5f);Assert.AreEqual("Idle",actor.CurrentAction);
            Assert.Less(Vector3.Distance(position,actor.transform.position),.001f,"no root motion drift");
            actor.enabled=false;actor.enabled=true;yield return null;Assert.AreEqual("Idle",actor.CurrentAction,"reenabling does not replay historical actions");
        }
    }
}
