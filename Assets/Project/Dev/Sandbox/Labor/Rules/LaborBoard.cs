using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Labor
{
    // 플레이어별 운반 상태와 보상 계산. 라운드·경매와 무관하게 동작하며 서버에서만 쓴다.
    // 실제 현금 지급은 MatchEngine이 한다.
    public sealed class LaborBoard
    {
        public readonly LaborSettings Settings;
        readonly Dictionary<PlayerId, Load> carrying = new Dictionary<PlayerId, Load>();
        readonly Dictionary<PlayerId, int> earned = new Dictionary<PlayerId, int>();

        public LaborBoard(LaborSettings settings)
        {
            settings.Validate();
            Settings = settings;
        }

        public bool TryGetLoad(PlayerId player, out Company material)
        {
            bool found = carrying.TryGetValue(player, out var load);
            material = load.Material;
            return found;
        }

        public int Earned(PlayerId player) => earned.TryGetValue(player, out int total) ? total : 0;

        public string Pickup(PlayerId player, Company material, double now)
        {
            if (!Enum.IsDefined(typeof(Company), material)) return "잘못된 자재입니다.";
            if (carrying.ContainsKey(player)) return "이미 짐을 들고 있습니다.";
            carrying[player] = new Load(material, now);
            return $"{Name(material)} 짐을 들었습니다.";
        }

        public string Return(PlayerId player) =>
            carrying.Remove(player) ? "짐을 내려놓았습니다." : "들고 있는 짐이 없습니다.";

        // 성공하면 pay > 0이고 짐이 사라진다. 너무 빨리 도착하면 짐을 든 채로 거절한다.
        public string Deliver(PlayerId player, int floor, double now, out int pay)
        {
            pay = 0;
            if (!carrying.TryGetValue(player, out var load)) return "들고 있는 짐이 없습니다.";
            if (!Settings.IsFloor(floor)) return "잘못된 층입니다.";
            if (now - load.PickedAt < Settings.MinTripSeconds[floor - LaborSettings.FirstFloor])
                return "너무 빨리 도착했습니다. 다시 내려놓으세요.";
            pay = Settings.Pay(floor, load.Material);
            carrying.Remove(player);
            earned[player] = checked(Earned(player) + pay);
            return $"{floor}층 {Name(load.Material)} 하역 완료 +${pay}";
        }

        public static string Name(Company material) =>
            material == Company.Straw ? "볏짚" : material == Company.Timber ? "나무" : "벽돌";

        readonly struct Load
        {
            public readonly Company Material;
            public readonly double PickedAt;
            public Load(Company material, double pickedAt) { Material = material; PickedAt = pickedAt; }
        }
    }
}
