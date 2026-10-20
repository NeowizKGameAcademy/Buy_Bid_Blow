using System;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Round;
using CueAudio;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Audio
{
    // The hall's PA speaker above the ticker boards. Hears match moments from MatchTimeline and asks CueAudio
    // to play them from its position. No commands, RPCs or replicated fields.
    public sealed class HallSpeaker : MonoBehaviour
    {
        const double ClosingSoonSeconds=10;
        public CueReference roundStart, closingBell, auctionStart, auctionEnd,closingSoon;
        // Start of the auction slot already warned, so each slot warns once.
        double warnedSlot=double.NaN;

        void OnEnable()
        {
            GameAudio.EnsureHost();
            // The timeline only raises moments from now on, so late activation never replays old bells.
            MatchTimeline.SpanStarted+=Started;MatchTimeline.SpanEnded+=Ended;
        }
        void OnDisable(){MatchTimeline.SpanStarted-=Started;MatchTimeline.SpanEnded-=Ended;}
        void Update()
        {
            var timeline=MatchTimeline.Current;if(!timeline)return;
            double now=NetworkTime.time;
            // Once per slot, even when a short slot starts inside the warning window.
            if((timeline.TryGetActive(TimelineSpanKind.OpenAuction,now,out var slot)||timeline.TryGetActive(TimelineSpanKind.SecretAuction,now,out slot))
                &&slot.StartsAt!=warnedSlot&&now>=Math.Max(slot.StartsAt,slot.EndsAt-ClosingSoonSeconds))
            {warnedSlot=slot.StartsAt;Play(closingSoon);}
        }
        void Started(TimelineSpan span)
        {
            if(span.Kind==TimelineSpanKind.Round)Play(roundStart);
            if(IsAuction(span))Play(auctionStart);
        }
        void Ended(TimelineSpan span)
        {
            if(span.Kind==TimelineSpanKind.Round)Play(closingBell);
            if(IsAuction(span))Play(auctionEnd);
        }
        static bool IsAuction(TimelineSpan span)=>span.Kind==TimelineSpanKind.OpenAuction||span.Kind==TimelineSpanKind.SecretAuction;
        void Play(CueReference cue)=>CueAudioHost.PlayAt(cue,transform.position,this);
    }
}
