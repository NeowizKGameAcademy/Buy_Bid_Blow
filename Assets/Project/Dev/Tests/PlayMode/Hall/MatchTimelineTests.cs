using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Round;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class MatchTimelineTests
    {
        readonly HostedMatch match=new HostedMatch();
        [UnityTearDown]public IEnumerator Cleanup()
        {
            yield return match.Stop();
            // The hall's audio host persists across scenes; leave none behind for later tests.
            if(CueAudio.CueAudioHost.Instance)UnityEngine.Object.Destroy(CueAudio.CueAudioHost.Instance.gameObject);
            yield return null;
        }

        [UnityTest]public IEnumerator ServerScheduleMatchesEngineAndClientPassesEveryMoment()
        {
            yield return match.Start(0,settings=>
            {
                settings.RoundCount=5;settings.RoundSeconds=2;settings.AuctionRoundSeconds=6;settings.AuctionRounds=new[]{2};settings.AuctionDurationSeconds=2;
                settings.AuctionSlots=new[]{new AuctionSlot{StartsAtSeconds=0,DurationSeconds=2},new AuctionSlot{StartsAtSeconds=2,DurationSeconds=2}};
            });
            var timeline=MatchTimeline.Current;Assert.IsNotNull(timeline);Assert.AreSame(MatchNet.Current.gameObject,timeline.gameObject);
            var engine=(MatchEngine)typeof(GameNetworkManager).GetField("engine",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(match.Room);
            var round1=timeline.spans.Single(s=>s.Kind==TimelineSpanKind.Round&&s.Round==1);
            Assert.AreEqual(engine.Deadline,round1.EndsAt,1e-6);
            Assert.AreEqual(MatchNet.Current.firstRoundStartsAt,round1.StartsAt,1e-6);
            Assert.AreEqual(5,timeline.spans.Count(s=>s.Kind==TimelineSpanKind.Round));
            Assert.AreEqual(1,timeline.spans.Count(s=>s.Kind==TimelineSpanKind.OpenAuction));
            Assert.AreEqual(1,timeline.spans.Count(s=>s.Kind==TimelineSpanKind.SecretAuction));

            var seen=new List<(bool start,TimelineSpan span)>();
            System.Action<TimelineSpan> started=s=>seen.Add((true,s)),ended=s=>seen.Add((false,s));
            MatchTimeline.SpanStarted+=started;MatchTimeline.SpanEnded+=ended;
            try
            {
                yield return HostedMatch.Until(()=>MatchNet.Current.phase==MatchNet.Phase.Finished,"match finish",25);
                yield return null;
            }
            finally{MatchTimeline.SpanStarted-=started;MatchTimeline.SpanEnded-=ended;}
            Assert.AreEqual(5,seen.Count(e=>!e.start&&e.span.Kind==TimelineSpanKind.Round),"round 1 is already open, rounds 1-5 all close");
            Assert.AreEqual(4,seen.Count(e=>e.start&&e.span.Kind==TimelineSpanKind.Round));
            Assert.AreEqual(2,seen.Count(e=>e.span.Kind==TimelineSpanKind.OpenAuction),"open slot starts and ends");
            Assert.AreEqual(2,seen.Count(e=>e.span.Kind==TimelineSpanKind.SecretAuction));
            Assert.AreEqual(1,seen.Count(e=>!e.start&&e.span.Kind==TimelineSpanKind.Match));
            for(int r=1;r<5;r++)
            {
                int closed=seen.FindIndex(e=>!e.start&&e.span.Kind==TimelineSpanKind.Round&&e.span.Round==r);
                int opened=seen.FindIndex(e=>e.start&&e.span.Kind==TimelineSpanKind.Round&&e.span.Round==r+1);
                Assert.Less(closed,opened,$"R{r} closes before R{r+1} opens");
            }
            Assert.IsTrue(timeline.TryGetLast(TimelineSpanKind.Round,Mirror.NetworkTime.time,out var last));Assert.AreEqual(5,last.Round);
            Assert.IsFalse(timeline.TryGetNext(TimelineSpanKind.Round,Mirror.NetworkTime.time,out _));
        }
    }
}
