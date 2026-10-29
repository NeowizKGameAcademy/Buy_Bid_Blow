using BuyBidBlow.Common;
using BuyBidBlow.Systems.Types;
using UnityEngine;

namespace BuyBidBlow.Sandbox.Labor
{
    // 층별 하역장. 짐을 든 상태에서만 프롬프트가 보이고, E로 내려놓으면 서버가 보상을 지급한다.
    public sealed class LaborDropZone : MonoBehaviour, IInteractable
    {
        public int floor = 2;

        public bool CanInteract => LaborSite.Instance && LaborSite.Carrying >= 0;
        public string Prompt => CanInteract
            ? $"Unload {floor}F  +${LaborSite.Instance.Settings.Pay(floor, (Company)LaborSite.Carrying)}"
            : "";

        public bool Interact(GameObject player)
        {
            if (!CanInteract) return false;
            LaborSite.Request(LaborAction.Deliver, floor: floor);
            return true;
        }
    }
}
