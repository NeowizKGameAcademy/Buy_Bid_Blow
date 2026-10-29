using BuyBidBlow.Systems.Shop;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Match
{
    // 상점 Sandbox 연결. 설정을 넘긴 경기에서만 Shop이 생기며, 원본 경기에는 영향이 없다.
    // 라운드 마감(Deadline)과 무관하게 경기가 끝나기 전까지 언제든 사고 쓸 수 있다.
    // 파티 아이템은 구매 비용만큼 현금이 줄 뿐, 주가·주식·이동·입찰에는 영향이 없다.
    public sealed partial class MatchEngine
    {
        public ShopBoard Shop { get; private set; }

        public void EnableShop(ShopSettings settings)
        {
            if (Shop == null) Shop = new ShopBoard(settings);
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
            if (!Shop.HasRoom(player)) return $"최대 {Shop.Settings.InventorySlots}개까지 가질 수 있습니다.";
            if (!assets[player].TrySpendAvailable(item.Price))
                return assets[player].Reserved > 0 ? "입찰 예약금을 제외한 돈이 부족합니다." : "돈이 부족합니다.";
            Shop.TryAdd(player, item.Id);
            Changed?.Invoke();
            return $"{item.Name} 구매 · -${item.Price}";
        }

        // 파티 아이템 사용. 성공하면 used에 아이템을 돌려주고 칸을 비운다. emote는 이모티콘일 때 Emotes 번호.
        public string UseItem(PlayerId player, int slot, int emote, double now, out ShopItemDef used)
        {
            used = null;
            var gate = ShopGate(player);
            if (gate != null) return gate;
            var item = Shop.ItemAt(player, slot);
            if (item == null) return "빈 칸입니다.";
            double left = Shop.CooldownLeft(player, now);
            if (left > 0) return $"{System.Math.Ceiling(left):0}초 뒤에 다시 쓸 수 있습니다.";
            if (item.Effect == ShopEffect.Emote && (emote < 0 || emote >= Shop.Settings.Emotes.Length)) return "이모티콘을 골라 주세요.";
            Shop.RemoveAt(player, slot);
            Shop.MarkUsed(player, now);
            used = item;
            return $"{item.Name} 사용!";
        }
    }
}
