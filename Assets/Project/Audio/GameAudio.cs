using System.Collections.Generic;
using CueAudio;
using UnityEngine;

namespace BuyBidBlow.Audio
{
    public static class GameAudio
    {
        static readonly Dictionary<string,int> playedFrame=new Dictionary<string,int>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>playedFrame.Clear();
        public static CueAudioHost EnsureHost()
        {
#if UNITY_SERVER
            return null;
#else
            if(!CueAudioHost.Instance)
            {
                var prefab=Resources.Load<GameObject>("GameAudioHost");
                if(prefab)Object.Instantiate(prefab);
            }
            return CueAudioHost.Instance;
#endif
        }
        public static void Play(string id,Object owner=null)
        {
            if(playedFrame.TryGetValue(id,out int frame)&&frame==Time.frameCount)return;
            var host=EnsureHost();if(!host||host.Engine==null)return;
            playedFrame[id]=Time.frameCount;host.Engine.Play(id,owner:owner);
        }
    }
}
