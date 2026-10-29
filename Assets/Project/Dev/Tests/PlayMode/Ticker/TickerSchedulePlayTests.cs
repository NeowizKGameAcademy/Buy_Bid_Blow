using System.Collections;
using System.Linq;
using System.Reflection;
using BuyBidBlow.Common;
using BuyBidBlow.UI.Ticker;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BuyBidBlow.Dev.Tests.PlayMode.Ticker
{
    public sealed class TickerSchedulePlayTests
    {
        readonly HostedMatch match=new HostedMatch();
        [UnityTearDown]public IEnumerator Cleanup()
        {
            yield return match.Stop();
            if(CueAudio.CueAudioHost.Instance)Object.Destroy(CueAudio.CueAudioHost.Instance.gameObject);
            yield return null;
        }

        [UnityTest]public IEnumerator HallBoardsFollowTheScheduleThroughARoundChange()
        {
            yield return match.Start(30,settings=>{settings.RoundCount=3;settings.AuctionRounds=new int[0];});
            Assert.IsNotNull(TickerDirector.Active,"the hall scene places the ticker director");
            // The schedule lists boards from the audience's left to right.
            var boards=TickerDirector.Active.Schedule.Data.boards
                .Select(n=>Object.FindObjectsByType<TickerBoardDisplay>(FindObjectsSortMode.None).Single(b=>b.name==n)).ToArray();
            yield return null;
            Assert.IsTrue(boards.All(b=>b.CurrentChannel!=null),"boards follow the schedule once the match runs");

            yield return HostedMatch.Until(()=>MatchNet.Current.SecondsLeft<9,"last ten seconds of round 1",30);
            yield return null;
            CollectionAssert.AreEqual(new[]{"timer","timer","timer"},boards.Select(b=>b.CurrentChannel).ToArray());
            Assert.IsTrue(boards.All(b=>b.transform.Find("Ticker UI/Timer").gameObject.activeSelf));

            // Round-over and transition videos are not scheduled yet: the playlist's regular screens carry on.
            yield return HostedMatch.Until(()=>MatchNet.Current.round==2,"round 2",20);
            yield return null;yield return null;
            var layers=boards.Select(b=>(RawImage)typeof(TickerBoardDisplay).GetField("videoLayer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(b)).ToArray();
            for(int frame=0;frame<30;frame++)
            {
                Assert.IsTrue(boards.All(b=>b.CurrentChannel!=null&&b.CurrentChannel!="roundOver"&&b.CurrentChannel!="pig"),"regular screens at the round change");
                Assert.IsTrue(layers.All(l=>!l.enabled),"no video layer");
                yield return null;
            }
        }
    }
}
