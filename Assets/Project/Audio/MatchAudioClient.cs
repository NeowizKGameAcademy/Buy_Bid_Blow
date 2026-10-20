using BuyBidBlow.Common;
using CueAudio;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Audio
{
    // Observes existing replicated public state. No commands, RPCs or replicated fields.
    public sealed class MatchAudioClient : MonoBehaviour
    {
        public GameObject hostPrefab;
        public CueReference roundStart, closingBell, auctionStart, auctionEnd,closingSoon;
        MatchNet bound;
        MatchNet.Phase phase;
        MatchNet.AuctionStage stage;
        int round, closedRound;
        double auctionDeadline;
        bool initialized;
        bool warned;

        void Update()
        {
#if !UNITY_SERVER
            if(!NetworkClient.active){ResetState();return;}
            var match=MatchNet.Current;
            if(!match){ResetState();return;}
            EnsureHost();
            if(bound!=match || !initialized)
            {
                bound=match;Capture(match);initialized=true;
                // Late activation reads a baseline; it must not replay old bells.
                return;
            }
            bool playing=match.phase==MatchNet.Phase.Playing;
            bool slotChanged=match.auctionStage!=stage || match.auctionClosesAt!=auctionDeadline || match.round!=round;
            if(slotChanged)warned=false;
            if(stage!=MatchNet.AuctionStage.None && phase==MatchNet.Phase.Playing && (slotChanged || !playing))Play(auctionEnd);
            if(match.lastClosedRound>closedRound)Play(closingBell);
            if(playing && (phase!=MatchNet.Phase.Playing || match.round!=round))Play(roundStart);
            if(playing && match.auctionStage!=MatchNet.AuctionStage.None && (slotChanged || phase!=MatchNet.Phase.Playing))Play(auctionStart);
            double left=match.auctionClosesAt-Mirror.NetworkTime.time;
            if(playing&&match.auctionStage!=MatchNet.AuctionStage.None&&!warned&&left>0&&left<=10){Play(closingSoon);warned=true;}
            Capture(match);
#endif
        }
        void EnsureHost(){GameAudio.EnsureHost();}
        void Play(CueReference cue)
        {if(CueAudioHost.Instance && CueAudioHost.Instance.Engine!=null && !string.IsNullOrEmpty(cue.Id))CueAudioHost.Instance.Engine.Play(cue,owner:this);}
        void Capture(MatchNet match){phase=match.phase;stage=match.auctionStage;round=match.round;closedRound=match.lastClosedRound;auctionDeadline=match.auctionClosesAt;}
        void ResetState(){bound=null;initialized=false;warned=false;}
        void OnDisable()=>ResetState();
    }
}
