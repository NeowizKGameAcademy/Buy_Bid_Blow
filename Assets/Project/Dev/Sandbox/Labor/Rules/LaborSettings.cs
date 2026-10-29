using System;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Labor
{
    // 노동(짐 나르기) 수치. LaborSettings.json에서 읽는다. 층 배열의 인덱스 0 = 2층.
    [Serializable]
    public sealed class LaborSettings
    {
        // 층별 기본 보상(달러): 2층, 3층, 4층.
        public int[] FloorPay = { 15, 30, 45 };
        // 짐을 든 뒤 그 층에 내려놓기까지의 최소 시간(초). 순간이동·매크로 방지.
        public float[] MinTripSeconds = { 4, 9, 13 };
        // 자재별 보상 배율(%): Straw, Timber, Brick.
        public int[] MaterialPayPercent = { 100, 110, 120 };
        // 자재별 운반 중 이동 속도 배율: Straw, Timber, Brick.
        public float[] CarrySpeed = { 1f, .85f, .7f };

        public const int FirstFloor = 2;
        public int TopFloor => FirstFloor + FloorPay.Length - 1;

        public void Validate()
        {
            int companies = Enum.GetValues(typeof(Company)).Length;
            if (FloorPay == null || FloorPay.Length == 0) throw new ArgumentException("FloorPay is empty.");
            if (MinTripSeconds == null || MinTripSeconds.Length != FloorPay.Length) throw new ArgumentException("MinTripSeconds must match FloorPay.");
            if (MaterialPayPercent == null || MaterialPayPercent.Length != companies) throw new ArgumentException("MaterialPayPercent needs one value per material.");
            if (CarrySpeed == null || CarrySpeed.Length != companies) throw new ArgumentException("CarrySpeed needs one value per material.");
            foreach (var pay in FloorPay) if (pay < 0) throw new ArgumentException("FloorPay must not be negative.");
            foreach (var percent in MaterialPayPercent) if (percent < 0) throw new ArgumentException("MaterialPayPercent must not be negative.");
            foreach (var speed in CarrySpeed) if (speed <= 0) throw new ArgumentException("CarrySpeed must be positive.");
        }

        public bool IsFloor(int floor) => floor >= FirstFloor && floor <= TopFloor;

        // 한 번 운반의 보상. 소수점은 버린다.
        public int Pay(int floor, Company material) =>
            FloorPay[floor - FirstFloor] * MaterialPayPercent[(int)material] / 100;
    }
}
