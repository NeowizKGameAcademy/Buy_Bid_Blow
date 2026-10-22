using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Characters
{
    // Local presentation of already replicated state. Never reads private secret bids.
    public sealed class AuctioneerAnimationClient : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip talking,counting,thankful,bow;
        [Min(0)]public float bidReactionInterval=3;
        public string CurrentAction {get;private set;}="Idle";
        MatchNet bound;MatchNet.AuctionStage stage;MatchNet.Phase phase;
        int highest;double deadline;bool initialized,warned,pendingStart,pendingCount,waitingPublicResult,publicEnded;
        float finishesAt,nextBidReaction;

        void OnEnable(){ResetPresentation();PlayerNet.LocalAuctionOutcome+=Outcome;}
        void OnDisable(){PlayerNet.LocalAuctionOutcome-=Outcome;initialized=false;bound=null;}
        void ResetPresentation()
        {
            initialized=false;bound=null;pendingStart=pendingCount=waitingPublicResult=publicEnded=false;
            warned=false;nextBidReaction=0;Play("Idle",null);
        }
        void Update()
        {
            var match=MatchNet.Current;
            if(!NetworkClient.active||!match){if(initialized)ResetPresentation();return;}
            if(bound!=match||!initialized){bound=match;Capture(match);initialized=true;return;}
            if(match.phase==MatchNet.Phase.Finished&&phase!=MatchNet.Phase.Finished)
            {pendingStart=pendingCount=waitingPublicResult=false;Play("Bow",bow);}
            else if(match.phase==MatchNet.Phase.Playing)
            {
                bool changed=match.auctionStage!=stage||match.auctionClosesAt!=deadline;
                if(changed)
                {
                    warned=false;pendingCount=false;
                    if(stage==MatchNet.AuctionStage.Secret){pendingStart=waitingPublicResult=false;Play("Counting",counting);}
                    if(match.auctionStage==MatchNet.AuctionStage.Open)
                    {publicEnded=false;waitingPublicResult=false;pendingStart=false;Play("Talking",talking);}
                    else if(match.auctionStage==MatchNet.AuctionStage.Secret)
                    {
                        pendingStart=true;
                        // Public settlement arrives through the existing outcome RPC, not inferred from stale bids.
                        waitingPublicResult=!publicEnded;
                        if(!waitingPublicResult&&CurrentAction=="Idle")StartPending();
                    }
                }
                if(match.auctionStage==MatchNet.AuctionStage.Open&&match.highestBid>highest&&Time.unscaledTime>=nextBidReaction)
                {pendingCount=true;nextBidReaction=Time.unscaledTime+bidReactionInterval;}
                double left=match.auctionClosesAt-NetworkTime.time;
                if(match.auctionStage!=MatchNet.AuctionStage.None&&!warned&&left>0&&left<=10)
                {warned=true;pendingCount=true;}
            }
            Capture(match);
            if(CurrentAction!="Idle"&&Time.unscaledTime>=finishesAt)Play("Idle",null);
            if(CurrentAction=="Idle")StartPending();
        }
        void StartPending()
        {
            if(waitingPublicResult)return;
            if(pendingStart){pendingStart=false;Play("Talking",talking);}
            else if(pendingCount&&bound&&bound.auctionStage!=MatchNet.AuctionStage.None){pendingCount=false;Play("Thankful",thankful);}
        }
        void Outcome(AuctionOutcome outcome)
        {
            // Public outcomes are sent to everyone. Secret outcomes differ per player and must never drive this NPC.
            if(outcome.Secret||publicEnded||!MatchNet.Current||MatchNet.Current.phase!=MatchNet.Phase.Playing)return;
            publicEnded=true;waitingPublicResult=false;pendingCount=false;
            Play(outcome.Sold?"Counting":"Bow",outcome.Sold?counting:bow);
        }
        void Capture(MatchNet match){stage=match.auctionStage;phase=match.phase;highest=match.highestBid;deadline=match.auctionClosesAt;}
        void Play(string state,AnimationClip clip)
        {
            CurrentAction=state;finishesAt=clip?Time.unscaledTime+clip.length:float.PositiveInfinity;
            if(animator&&animator.runtimeAnimatorController){animator.applyRootMotion=false;animator.CrossFadeInFixedTime("Base Layer."+state,.12f,0);}
        }
    }
}
