using UnityEngine;
namespace BuyBidBlow.UI.Menu
{
    public static class GameSettings
    {
        const string Preference = "BBB.Settings.v1";
        public static SettingsValues Current { get; private set; } = SettingsValues.Defaults;
        public static float LookMultiplier => Current.Sensitivity / 50f;
        public static void Load() => Apply(JsonUtility.FromJson<SettingsValues>(PlayerPrefs.GetString(Preference, JsonUtility.ToJson(SettingsValues.Defaults))), false);
        public static void Apply(SettingsValues values, bool save = true)
        {
            values.Master=Mathf.Clamp(values.Master,0,100); values.Music=Mathf.Clamp(values.Music,0,100); values.Effects=Mathf.Clamp(values.Effects,0,100);
            values.Sensitivity=Mathf.Clamp(values.Sensitivity,1,100); values.DisplayMode=Mathf.Clamp(values.DisplayMode,0,2); Current=values;
            AudioListener.volume=values.Master/100f;
            var engine=BuyBidBlow.Audio.GameAudio.EnsureHost()?.Engine;
            if(engine!=null){engine.SetBusVolume("music",values.Music/100f);engine.SetBusVolume("sfx",values.Effects/100f);engine.SetBusVolume("ui",values.Effects/100f);}
            Screen.fullScreenMode=new[]{FullScreenMode.Windowed,FullScreenMode.ExclusiveFullScreen,FullScreenMode.FullScreenWindow}[values.DisplayMode];
            if(save){PlayerPrefs.SetString(Preference,JsonUtility.ToJson(values));PlayerPrefs.Save();}
        }
    }
}
