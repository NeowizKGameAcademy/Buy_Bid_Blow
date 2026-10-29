using BuyBidBlow.Systems.Account;
using BuyBidBlow.Systems.Room;
using UnityEngine;

namespace BuyBidBlow.Sandbox.Login
{
    // 로그인한 계정. 씬이 바뀌어도 유지된다(앱 종료 시 사라짐).
    public static class LoginSession
    {
        public static AccountProfile Current { get; private set; }
        public static bool SignedIn => Current != null;

        public static void Begin(AccountProfile profile)
        {
            Current = profile;
            // 로비 닉네임 입력칸이 이 값으로 채워진다(LobbyHud가 같은 키를 읽음).
            PlayerPrefs.SetString(Nickname.PreferenceKey, profile.Nickname);
            PlayerPrefs.Save();
        }

        public static void End() => Current = null;
    }
}
