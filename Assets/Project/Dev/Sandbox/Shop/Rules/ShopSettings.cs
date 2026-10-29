using System;
using System.Collections.Generic;

namespace BuyBidBlow.Systems.Shop
{
    // 상점이 효과를 아는 아이템 종류. 모두 표현용이며 주가·주식·이동·입찰에는 영향이 없다.
    // 새 효과를 만들 때만 늘린다(값은 JSON·통신에 쓰인다).
    public enum ShopEffect
    {
        None = 0,
        Confetti = 1, // 내 주변에 색종이 폭죽
        Bubbles = 2,  // 내 주변에 비눗방울
        Emote = 3     // 내 머리 위에 이모티콘(Emotes 중 하나를 골라 사용)
    }

    // 판매 품목 하나. ShopSettings.json의 Items 배열 순서대로 판매창 칸(4×2)을 채운다.
    // 모두 일회용이며 되팔 수 없고, 경기가 끝나면 남은 것은 사라진다.
    [Serializable]
    public sealed class ShopItemDef
    {
        public int Id;            // 1부터. 가방·통신에 쓰는 고유 번호.
        public string Name;
        public string Description;
        public int Price;         // 구매가(달러, 인게임 현금)
        public float Seconds = 2.5f; // 효과 지속 시간(초)
        public ShopEffect Effect;
    }

    // 상점 수치. ShopSettings.json에서 읽는다.
    [Serializable]
    public sealed class ShopSettings
    {
        public ShopItemDef[] Items =
        {
            new ShopItemDef { Id = 1, Name = "색종이 폭죽", Description = "내 주변에 색종이가 터진다.", Price = 3, Seconds = 2.5f, Effect = ShopEffect.Confetti },
            new ShopItemDef { Id = 2, Name = "비눗방울", Description = "내 주변에 비눗방울이 떠오른다.", Price = 3, Seconds = 3f, Effect = ShopEffect.Bubbles },
            new ShopItemDef { Id = 3, Name = "이모티콘", Description = "머리 위에 고른 이모티콘을 띄운다.", Price = 2, Seconds = 3f, Effect = ShopEffect.Emote },
        };
        // 이모티콘 아이템으로 고를 수 있는 글자. Pretendard에 있는 글자만 쓴다.
        public string[] Emotes = { "^_^", "ㅠㅠ", "!!", "??", "ㅋㅋㅋ", "GG" };
        public int ShopColumns = 4;
        public int ShopRows = 2;
        // 최대 보유 수(칸마다 1개).
        public int InventorySlots = 3;
        // 아이템 사용 간격(초). 종류와 관계없이 한 사람 기준.
        public float UseCooldown = 10f;

        public const float MaxEffectSeconds = 10f;
        public const int MaxEmoteLength = 8;

        public int ShopSlots => ShopColumns * ShopRows;

        public void Validate()
        {
            if (Items == null) throw new ArgumentException("Items is missing.");
            if (ShopColumns <= 0 || ShopRows <= 0) throw new ArgumentException("Shop grid must be at least 1x1.");
            if (Items.Length > ShopSlots) throw new ArgumentException($"Items ({Items.Length}) exceed shop slots ({ShopSlots}).");
            if (InventorySlots <= 0) throw new ArgumentException("InventorySlots must be positive.");
            if (UseCooldown < 0) throw new ArgumentException("UseCooldown must not be negative.");
            if (Emotes == null || Emotes.Length == 0) throw new ArgumentException("Emotes needs at least one entry.");
            foreach (var emote in Emotes)
                if (string.IsNullOrWhiteSpace(emote) || emote.Length > MaxEmoteLength) throw new ArgumentException($"Emotes must be 1~{MaxEmoteLength} characters.");
            var ids = new HashSet<int>();
            foreach (var item in Items)
            {
                if (item == null || item.Id <= 0 || !ids.Add(item.Id)) throw new ArgumentException("Item ids must be positive and unique.");
                if (item.Price <= 0) throw new ArgumentException($"Item {item.Id} needs a positive price.");
                if (item.Seconds <= 0 || item.Seconds > MaxEffectSeconds) throw new ArgumentException($"Item {item.Id} Seconds must be in (0, {MaxEffectSeconds}].");
                if (item.Effect == ShopEffect.None || !Enum.IsDefined(typeof(ShopEffect), item.Effect)) throw new ArgumentException($"Item {item.Id} has an unknown effect.");
            }
        }

        public ShopItemDef Find(int id)
        {
            foreach (var item in Items) if (item.Id == id) return item;
            return null;
        }
    }
}
