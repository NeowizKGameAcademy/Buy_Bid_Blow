using CueAudio;
using UnityEngine;

namespace BuyBidBlow.Audio
{
    // Jump is called by PlayerNet's existing animation RPC; other actions expose hooks.
    public sealed class PlayerActionAudio : MonoBehaviour
    {
        public CueReference footstep, jump, emotion;
        public void PlayFootstep()=>Play(footstep);
        public void PlayJump()=>Play(jump);
        public void PlayEmotion()=>Play(emotion);
        void Play(CueReference cue)
        {
#if !UNITY_SERVER
            var host=CueAudioHost.Instance;
            if(host && host.Engine!=null && !string.IsNullOrEmpty(cue.Id))host.Engine.PlayAt(cue,transform.position,owner:this);
#endif
        }
    }
}
