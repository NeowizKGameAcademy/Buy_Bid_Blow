using BuyBidBlow.Common;
using UnityEngine;

namespace BuyBidBlow.Sandbox.Labor
{
    // 홀 ↔ 일터 이동 문. 이동은 내 플레이어를 로컬에서 옮기는 것으로 충분하다(위치는 클라이언트 권한).
    public sealed class LaborDoor : MonoBehaviour, IInteractable
    {
        public enum Kind { ToWorkSite, ToHall }
        public Kind kind;

        public bool CanInteract => kind == Kind.ToWorkSite || (LaborSite.Instance && LaborSite.Instance.hallReturn);
        public string Prompt => kind == Kind.ToWorkSite ? "Go to work" : "Back to hall";

        public bool Interact(GameObject player)
        {
            if (kind == Kind.ToWorkSite)
            {
                LaborSite.Teleport(player, LaborLayout.Arrival, Vector3.forward);
                return true;
            }
            var back = LaborSite.Instance ? LaborSite.Instance.hallReturn : null;
            if (!back) return false;
            // 짐을 든 채 나가면 더미에 돌려놓은 것으로 처리한다.
            if (LaborSite.Carrying >= 0) LaborSite.Request(LaborAction.Return);
            LaborSite.Teleport(player, back.position, back.forward);
            return true;
        }
    }
}
