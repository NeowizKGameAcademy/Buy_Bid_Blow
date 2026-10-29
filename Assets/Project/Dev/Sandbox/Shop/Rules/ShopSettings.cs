using System;
using System.Collections.Generic;

namespace BuyBidBlow.Systems.Shop
{
    // 상점이 효과를 아는 아이템 종류. 새 효과를 만들 때만 늘린다(값은 저장·통신에 쓰이므로 바꾸지 않는다).
    public enum ShopEffect
    {
        None = 0,
        Leash = 1,    // 다른 플레이어에게 채우면 그 플레이어는 사용자 반경 안에서만 움직인다.
        Scissors = 2, // 내게 채워진 목줄을 끊는다.
        Deed = 3      // 보유만으로 라운드가 넘어갈 때마다 수입. 상점에 되팔 수 있다.
    }

    // 판매 품목 하나. ShopSettings.json의 Items 배열 순서대로 판매창 칸(4×2)을 채운다.
    [Serializable]
    public sealed class ShopItemDef
    {
        public int Id;            // 1부터. 가방·통신에 쓰는 고유 번호.
        public string Name;
        public string Description;
        public int Price;         // 구매가(달러)
        public int SellPrice = -1; // 되팔기 가격. -1이면 되팔 수 없다.
        public ShopEffect Effect;
    }

    // 상점 수치. ShopSettings.json에서 읽는다.
    [Serializable]
    public sealed class ShopSettings
    {
        public ShopItemDef[] Items =
        {
            new ShopItemDef { Id = 1, Name = "목줄", Description = "상대에게 채우면 내 주변 2m 안에서만 움직일 수 있다.", Price = 200, Effect = ShopEffect.Leash },
            new ShopItemDef { Id = 2, Name = "가위", Description = "내게 채워진 목줄을 끊는다.", Price = 50, Effect = ShopEffect.Scissors },
            new ShopItemDef { Id = 3, Name = "집문서", Description = "보유하면 매 라운드 $50. 상점에 $500에 되팔 수 있다.", Price = 500, SellPrice = 500, Effect = ShopEffect.Deed },
        };
        public int ShopColumns = 4;
        public int ShopRows = 2;
        public int InventorySlots = 4;
        // 집문서 1장당 라운드 수입(달러).
        public int DeedIncome = 50;
        // 목줄 반경(m): 묶인 사람은 채운 사람에게서 이 거리 안에서만 움직인다.
        public float LeashRadius = 2f;
        // 목줄을 채울 수 있는 거리(m).
        public float LeashReach = 3f;
        // 목줄 지속 시간(초). 0이면 가위로 끊거나 경기가 끝날 때까지.
        public float LeashSeconds = 0f;

        public int ShopSlots => ShopColumns * ShopRows;

        public void Validate()
        {
            if (Items == null) throw new ArgumentException("Items is missing.");
            if (ShopColumns <= 0 || ShopRows <= 0) throw new ArgumentException("Shop grid must be at least 1x1.");
            if (Items.Length > ShopSlots) throw new ArgumentException($"Items ({Items.Length}) exceed shop slots ({ShopSlots}).");
            if (InventorySlots <= 0) throw new ArgumentException("InventorySlots must be positive.");
            if (DeedIncome < 0) throw new ArgumentException("DeedIncome must not be negative.");
            if (LeashRadius <= 0 || LeashReach <= 0 || LeashSeconds < 0) throw new ArgumentException("Leash values are out of range.");
            var ids = new HashSet<int>();
            foreach (var item in Items)
            {
                if (item == null || item.Id <= 0 || !ids.Add(item.Id)) throw new ArgumentException("Item ids must be positive and unique.");
                if (item.Price <= 0) throw new ArgumentException($"Item {item.Id} needs a positive price.");
                if (item.SellPrice < -1) throw new ArgumentException($"Item {item.Id} SellPrice must be -1 or more.");
                if (!Enum.IsDefined(typeof(ShopEffect), item.Effect)) throw new ArgumentException($"Item {item.Id} has an unknown effect.");
            }
        }

        public ShopItemDef Find(int id)
        {
            foreach (var item in Items) if (item.Id == id) return item;
            return null;
        }
    }
}
