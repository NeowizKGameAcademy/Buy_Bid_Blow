using System.Collections.Generic;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Shop
{
    // 경기 하나의 상점 상태: 플레이어별 가방과 마지막 사용 시각.
    // 돈은 다루지 않는다(MatchEngine.Shop이 자산과 함께 처리). 경기마다 새로 만들어지므로 남은 아이템은 경기가 끝나면 사라진다.
    public sealed class ShopBoard
    {
        public readonly ShopSettings Settings;
        readonly Dictionary<PlayerId, int[]> bags = new Dictionary<PlayerId, int[]>();
        readonly Dictionary<PlayerId, double> lastUse = new Dictionary<PlayerId, double>();

        public ShopBoard(ShopSettings settings)
        {
            settings.Validate();
            Settings = settings;
        }

        // ---- 가방 ----
        public int[] Bag(PlayerId player)
        {
            if (!bags.TryGetValue(player, out var bag)) bags[player] = bag = new int[Settings.InventorySlots];
            return bag;
        }

        public int[] BagCopy(PlayerId player) => (int[])Bag(player).Clone();

        public bool HasRoom(PlayerId player) => System.Array.IndexOf(Bag(player), 0) >= 0;

        public bool TryAdd(PlayerId player, int itemId)
        {
            var bag = Bag(player);
            for (int i = 0; i < bag.Length; i++)
                if (bag[i] == 0) { bag[i] = itemId; return true; }
            return false;
        }

        public ShopItemDef ItemAt(PlayerId player, int slot)
        {
            var bag = Bag(player);
            return slot >= 0 && slot < bag.Length && bag[slot] != 0 ? Settings.Find(bag[slot]) : null;
        }

        public void RemoveAt(PlayerId player, int slot) => Bag(player)[slot] = 0;

        // ---- 사용 간격 ----
        // 다시 쓸 수 있을 때까지 남은 초. 0이면 지금 쓸 수 있다.
        public double CooldownLeft(PlayerId player, double now)
        {
            if (!lastUse.TryGetValue(player, out var at)) return 0;
            double left = at + Settings.UseCooldown - now;
            return left > 0 ? left : 0;
        }

        public void MarkUsed(PlayerId player, double now) => lastUse[player] = now;
    }
}
