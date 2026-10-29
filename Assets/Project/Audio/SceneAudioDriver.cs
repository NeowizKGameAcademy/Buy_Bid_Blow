using CueAudio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Audio
{
    public sealed class SceneAudioDriver:MonoBehaviour
    {
        public CueReference menuMusic,gameMusic;
        AudioHandle music;string currentMusic;
        void OnEnable(){SceneManager.sceneLoaded+=OnScene;OnScene(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
        void OnDisable(){SceneManager.sceneLoaded-=OnScene;music.Stop(.3f);}
        void OnScene(Scene scene,LoadSceneMode _)
        {
            foreach(var root in scene.GetRootGameObjects())UIAudioFeedback.Bind(root.transform);
            var cue=scene.name=="SC_MainMenu"?menuMusic:scene.name=="SC_ExchangeFurnished"?gameMusic:default;
            if(cue.Id==currentMusic)return;
            currentMusic=cue.Id;var host=CueAudioHost.Instance;
            if(string.IsNullOrEmpty(cue.Id)){music.Stop(.6f);return;}
            if(host&&host.Engine!=null)music=host.Engine.Crossfade(music,cue,.8f,owner:this);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Initialize()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.name=="SC_MainMenu"||scene.name=="SC_ExchangeFurnished")GameAudio.EnsureHost();
        }
    }
}
