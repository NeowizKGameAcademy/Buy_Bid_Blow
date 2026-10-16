using System.Collections.Generic;

namespace BuyBidBlow.LobbySandbox
{
    // Server-owned admission reservations include players whose spawn is still pending.
    public sealed class LobbyRoster
    {
        public const int Capacity=6;
        readonly Dictionary<int,string> entries=new Dictionary<int,string>();
        public int Count=>entries.Count;
        public bool Contains(int connectionId)=>entries.ContainsKey(connectionId);
        public bool TryReserve(int connectionId,string nickname,out string error)
        {
            error="";
            if(entries.ContainsKey(connectionId)){error="이미 입장 처리 중입니다.";return false;}
            if(!LobbyNickname.TryNormalize(nickname,out var name)){error="닉네임이 올바르지 않습니다.";return false;}
            if(entries.Count>=Capacity){error="방이 가득 찼습니다. (최대 6명)";return false;}
            entries.Add(connectionId,name);return true;
        }
        public bool TryGetName(int connectionId,out string nickname)=>entries.TryGetValue(connectionId,out nickname);
        public void Release(int connectionId)=>entries.Remove(connectionId);
        public void Clear()=>entries.Clear();
    }
}
