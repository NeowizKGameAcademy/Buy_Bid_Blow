using System.Collections.Generic;

namespace BuyBidBlow.Systems.Room
{
    // Server-owned admission reservations include players whose spawn is still pending.
    public sealed class RoomRoster
    {
        public const int Capacity=6;
        readonly Dictionary<int,string> entries=new Dictionary<int,string>();
        public int Count=>entries.Count;
        // 연결이 명단에 있는지 확인
        public bool Contains(int connectionId)=>entries.ContainsKey(connectionId);
        // 닉네임을 검증하고 정원 내에서 연결의 입장 자리를 예약
        public bool TryReserve(int connectionId,string nickname,out string error)
        {
            error="";
            if(entries.ContainsKey(connectionId)){error="이미 입장 처리 중입니다.";return false;}
            if(!Nickname.TryNormalize(nickname,out var name)){error="닉네임이 올바르지 않습니다.";return false;}
            if(entries.Count>=Capacity){error="방이 가득 찼습니다. (최대 6명)";return false;}
            entries.Add(connectionId,name);return true;
        }
        // 연결의 닉네임을 조회
        public bool TryGetName(int connectionId,out string nickname)=>entries.TryGetValue(connectionId,out nickname);
        // 연결을 명단에서 제거
        public void Release(int connectionId)=>entries.Remove(connectionId);
        // 명단을 비움
        public void Clear()=>entries.Clear();
    }
}
