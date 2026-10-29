using System.Collections.Generic;
using BuyBidBlow.Systems.Shop;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Match
{
    // 상점 Sandbox 연결. 설정을 넘긴 경기에서만 Shop이 생기며, 원본 경기에는 영향이 없다.
    // 라운드 마감(Deadline)과 무관하게 경기가 끝나기 전까지 언제든 사고팔 수 있다.
    public sealed partial class MatchEngine
    {
        public ShopBoard Shop { get; private set; }

        public void EnableShop(ShopSettings settings)
        {
            if (Shop == null) Shop = new ShopBoard(settings, Round);
        }

        string ShopGate(PlayerId player) =>
            Shop == null ? "상점 기능이 꺼져 있습니다." :
            Complete ? "경기가 끝났습니다." :
            !assets.ContainsKey(player) ? "경기 참가자만 이용할 수 있습니다." : null;

        public string BuyItem(PlayerId player, int itemId)
        {
            var gate = ShopGate(player);
            if (gate != null) return gate;
            var item = Shop.Settings.Find(itemId);
            if (item == null) return "없는 상품입니다.";
            if (System.Array.IndexOf(Shop.Bag(player), 0) < 0) return "가방이 가득 찼습니다.";
            if (!assets[player].TrySpendAvailable(item.Price))
                return assets[player].Reserved > 0 ? "입찰 예약금을 제외한 돈이 부족합니다." : "돈이 부족합니다.";
            Shop.TryAdd(player, item.Id);
            Changed?.Invoke();
            return $"{item.Name} 구매 · -${item.Price}";
        }

        public string SellItem(PlayerId player, int slot)
        {
            var gate = ShopGate(player);
            if (gate != null) return gate;
            var item = Shop.ItemAt(player, slot);
            if (item == null) return "빈 칸입니다.";
            if (item.SellPrice < 0) return $"{item.Name}은(는) 되팔 수 없습니다.";
            Shop.RemoveAt(player, slot);
            if (item.SellPrice > 0) assets[player].Credit(item.SellPrice);
            Changed?.Invoke();
            return $"{item.Name} 판매 · +${item.SellPrice}";
        }

        // 목줄: holder가 target에게 채운다. 거리 검증은 호출하는 쪽(서버)이 한다.
        public string UseLeash(PlayerId holder, int slot, PlayerId target, double now)
        {
            var gate = ShopGate(holder);
            if (gate != null) return gate;
            var item = Shop.ItemAt(holder, slot);
            if (item == null || item.Effect != ShopEffect.Leash) return "그 칸에 목줄이 없습니다.";
            if (holder.Equals(target)) return "자기 자신에게는 채울 수 없습니다.";
            if (!assets.ContainsKey(target)) return "경기 참가자에게만 채울 수 있습니다.";
            if (Shop.IsLeashed(target)) return "이미 목줄이 채워져 있습니다.";
            Shop.RemoveAt(holder, slot);
            Shop.AddLeash(holder, target, now);
            return "목줄을 채웠습니다.";
        }

        // 가위: 내게 채워진 목줄을 끊는다.
        public string UseScissors(PlayerId player, int slot)
        {
            var gate = ShopGate(player);
            if (gate != null) return gate;
            var item = Shop.ItemAt(player, slot);
            if (item == null || item.Effect != ShopEffect.Scissors) return "그 칸에 가위가 없습니다.";
            if (!Shop.IsLeashed(player)) return "목줄이 채워져 있지 않습니다.";
            Shop.RemoveAt(player, slot);
            Shop.RemoveLeash(player);
            return "목줄을 끊었습니다.";
        }

        // 새 라운드가 시작됐으면 집문서 수입을 지급한다. 받은 플레이어와 금액을 돌려준다.
        public List<KeyValuePair<PlayerId, int>> PayDeedIncome()
        {
            var paid = new List<KeyValuePair<PlayerId, int>>();
            if (Shop == null || Complete) return paid;
            int rounds = Shop.TakeNewRounds(Round);
            if (rounds == 0) return paid;
            foreach (var owner in Shop.Owners)
            {
                if (!assets.ContainsKey(owner)) continue;
                int amount = checked(Shop.Count(owner, ShopEffect.Deed) * Shop.Settings.DeedIncome * rounds);
                if (amount <= 0) continue;
                assets[owner].Credit(amount);
                paid.Add(new KeyValuePair<PlayerId, int>(owner, amount));
            }
            if (paid.Count > 0) Changed?.Invoke();
            return paid;
        }
    }
}
