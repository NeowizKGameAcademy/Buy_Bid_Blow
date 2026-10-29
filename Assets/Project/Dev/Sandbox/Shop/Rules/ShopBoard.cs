using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Shop
{
    // 경기 하나의 상점 상태: 플레이어별 가방, 목줄 관계, 집문서 수입 정산 라운드.
    // 돈은 다루지 않는다(MatchEngine.Shop이 자산과 함께 처리).
    public sealed class ShopBoard
    {
        public readonly ShopSettings Settings;
        readonly Dictionary<PlayerId, int[]> bags = new Dictionary<PlayerId, int[]>();
        readonly Dictionary<PlayerId, Leash> leashes = new Dictionary<PlayerId, Leash>(); // 묶인 사람 → 목줄
        int paidRound;

        public readonly struct Leash
        {
            public readonly PlayerId Holder;
            public readonly double Until; // 0이면 무기한
            public Leash(PlayerId holder, double until) { Holder = holder; Until = until; }
        }

        public ShopBoard(ShopSettings settings, int currentRound)
        {
            settings.Validate();
            Settings = settings;
            paidRound = currentRound;
        }

        // ---- 가방 ----
        public int[] Bag(PlayerId player)
        {
            if (!bags.TryGetValue(player, out var bag)) bags[player] = bag = new int[Settings.InventorySlots];
            return bag;
        }

        public int[] BagCopy(PlayerId player) => (int[])Bag(player).Clone();

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

        public int Count(PlayerId player, ShopEffect effect) =>
            Bag(player).Count(id => id != 0 && Settings.Find(id)?.Effect == effect);

        // ---- 목줄 ----
        public bool IsLeashed(PlayerId target) => leashes.ContainsKey(target);

        public void AddLeash(PlayerId holder, PlayerId target, double now) =>
            leashes[target] = new Leash(holder, Settings.LeashSeconds > 0 ? now + Settings.LeashSeconds : 0);

        public bool RemoveLeash(PlayerId target) => leashes.Remove(target);

        public IEnumerable<KeyValuePair<PlayerId, Leash>> Leashes => leashes;

        // 시간이 다 된 목줄과, present에 없는(나간) 플레이어가 걸린 목줄을 푼다. 바뀌었으면 true.
        public bool ExpireLeashes(double now, System.Func<PlayerId, bool> present)
        {
            var gone = leashes.Where(e => (e.Value.Until > 0 && now >= e.Value.Until) || !present(e.Key) || !present(e.Value.Holder))
                .Select(e => e.Key).ToList();
            foreach (var target in gone) leashes.Remove(target);
            return gone.Count > 0;
        }

        public void ClearLeashes() => leashes.Clear();

        // ---- 집문서 ----
        // 지난 정산 이후 새로 시작된 라운드 수. 호출하면 정산한 것으로 기록한다.
        public int TakeNewRounds(int currentRound)
        {
            int rounds = currentRound - paidRound;
            if (rounds <= 0) return 0;
            paidRound = currentRound;
            return rounds;
        }

        public IEnumerable<PlayerId> Owners => bags.Keys;
    }
}
