using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Common
{
    // An offline match in this process for LocalTradeController. Implemented by the Dev sandbox
    // MatchManager (editor only); the Game assembly can't reference Dev directly, hence the interface.
    public interface ILocalMatch
    {
        TradeService TradeService { get; }
        // 회사의 현재 주가를 반환
        int GetPrice(Company company);
        // 매치에 플레이어를 추가
        void AddPlayer(PlayerId playerId);
        event Action<int, IReadOnlyList<PriceChange>> RoundAdvanced;
    }

    public static class LocalMatch
    {
        // Set by the sandbox MatchManager while its scene is loaded.
        public static ILocalMatch Current { get; set; }
    }
}
