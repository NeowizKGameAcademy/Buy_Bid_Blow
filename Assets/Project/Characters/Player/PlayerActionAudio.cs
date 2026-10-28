using CueAudio;
using UnityEngine;

namespace BuyBidBlow.Audio
{
    // Jump and emotions are called by PlayerNet's existing animation RPCs; footsteps expose a hook.
    public sealed class PlayerActionAudio : MonoBehaviour
    {
        public CueReference footstep, jump;
        AudioHandle emotion;
        public void PlayFootstep()=>Play(footstep);
        public void PlayJump()=>Play(jump);
        // Emotion cues are named after the animator trigger (emote.Flair); a trigger without a cue stays silent.
        public void PlayEmotion(string trigger)
        {
            StopEmotion();
            var host=CueAudioHost.Instance;
            if(host&&host.Engine!=null)emotion=host.Engine.PlayAt("emote."+trigger,transform.position,this);
        }
        public void StopEmotion()=>emotion.Stop(.2f);
        void OnDisable()=>StopEmotion();
        void Play(CueReference cue)=>CueAudioHost.PlayAt(cue,transform.position,this);
    }
}
