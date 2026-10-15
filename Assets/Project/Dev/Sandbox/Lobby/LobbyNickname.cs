using System.Globalization;

namespace BuyBidBlow.LobbySandbox
{
    public static class LobbyNickname
    {
        public const int MaxLength=12;
        public const string PreferenceKey="BuyBidBlow.Lobby.Nickname";
        public static bool TryNormalize(string input,out string nickname)
        {
            nickname=(input??string.Empty).Trim();
            if(nickname.Length==0||nickname.Length>MaxLength*8)return false;
            foreach(char c in nickname)if(char.IsControl(c)||c=='<'||c=='>')return false;
            return new StringInfo(nickname).LengthInTextElements<=MaxLength;
        }
    }
}
