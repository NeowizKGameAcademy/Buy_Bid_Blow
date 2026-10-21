using System;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Round;
using CueAudio;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Audio
{
    // The hall's PA speaker above the ticker boards. Plays what happened on the same MatchNet events the screens use,
    // and the closing-soon warning ahead of time from MatchTimeline, from its own position. No commands, RPCs or replicated fields.
    public sealed class HallSpeaker : MonoBehaviour
    {
        const double ClosingSoonSeconds=10;
        public CueReference matchStart, matchEnd, roundTurn, auctionStart, auctionEnd,closingSoon;
        // Start of the auction slot already warned, so each slot warns once.
        double warnedSlot=double.NaN;

        void OnEnable()
        {
            GameAudio.EnsureHost();
            // MatchNet only raises changes seen while connected, so late activation never replays old bells.
            MatchNet.RoundTurned+=Turned;MatchNet.AuctionStageChanged+=StageChanged;
        }
        void OnDisable(){MatchNet.RoundTurned-=Turned;MatchNet.AuctionStageChanged-=StageChanged;}
        void Update()
        {
            var timeline=MatchTimeline.Current;if(!timeline)return;
            double now=NetworkTime.time;
            // Once per slot, even when a short slot starts inside the warning window.
            if((timeline.TryGetActive(TimelineSpanKind.OpenAuction,now,out var slot)||timeline.TryGetActive(TimelineSpanKind.SecretAuction,now,out slot))
                &&slot.StartsAt!=warnedSlot&&now>=Math.Max(slot.StartsAt,slot.EndsAt-ClosingSoonSeconds))
            {warnedSlot=slot.StartsAt;Play(closingSoon);}
        }
        // The first round's opening is the match start and the last bell the match end; every other turn rings the bell.
        void Turned(MatchNet.RoundTurn turn)=>Play(turn.Closed==0?matchStart:turn.Opened==0?matchEnd:roundTurn);
        void StageChanged(MatchNet.AuctionStage previous,MatchNet.AuctionStage current)
        {
            if(previous!=MatchNet.AuctionStage.None)Play(auctionEnd);
            if(current!=MatchNet.AuctionStage.None)Play(auctionStart);
        }
        void Play(CueReference cue)=>CueAudioHost.PlayAt(cue,transform.position,this);
    }
}
