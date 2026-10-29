using BuyBidBlow.Systems.Types;
using UnityEngine;

namespace BuyBidBlow.Sandbox.Labor
{
    // 일터 배치 수치. 빌더는 이 값으로 건물을 만들고, 서버는 같은 값으로 플레이어 위치를 검증한다.
    // 일터 루트를 씬에서 옮기지 말고, 옮겨야 하면 Origin을 바꾼 뒤 Build Work Site를 다시 실행한다.
    public static class LaborLayout
    {
        // 홀 아래 멀리 떨어진 별도 공간.
        public static readonly Vector3 Origin = new Vector3(0, -200, 0);
        public const float FloorHeight = 4f;
        public const int TopFloor = 4;
        // 서버 거리 검사 허용치(m). 상호작용 거리 3m + 네트워크 지연 여유.
        public const float ServerReach = 5f;

        public static Vector3 ArrivalLocal => new Vector3(0, .1f, -21);
        public static Vector3 ExitDoorLocal => new Vector3(0, 0, -24);
        public static Vector3 PileLocal(Company material) => new Vector3(((int)material - 1) * 6f, 0, -12);
        public static Vector3 DropLocal(int floor) => new Vector3(0, (floor - 1) * FloorHeight, 6);

        public static Vector3 Arrival => Origin + ArrivalLocal;
        public static Vector3 PilePosition(Company material) => Origin + PileLocal(material);
        public static Vector3 DropPosition(int floor) => Origin + DropLocal(floor);
        // 이 높이보다 아래면 일터 안에 있는 것으로 본다(HUD 표시용).
        public static bool IsAtSite(Vector3 position) => position.y < Origin.y + 40;
    }
}
