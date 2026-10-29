using UnityEngine;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Characters
{
    // A desk clerk: interacting opens the trade panel for this clerk's company.
    public class NPCInfo : MonoBehaviour, IInteractable
    {
        [SerializeField] private Company company;

        public Company Company => company;
        public bool CanInteract => true;
        public string Prompt => "Trade";

        // 상호작용한 플레이어의 거래 패널을 이 직원의 회사로 열기
        public bool Interact(GameObject player)
        {
            var trade = player ? player.GetComponentInChildren<LocalTradeController>() : null;
            if (!trade) return false;
            trade.OpenPanel(company);
            return true;
        }
    }
}
