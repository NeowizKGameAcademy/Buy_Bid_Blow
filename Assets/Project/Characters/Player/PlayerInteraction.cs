using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.Serialization;

namespace BuyBidBlow.Characters
{
    // The interact key's entry point. Uses whatever the sensor sees; the target decides what happens.
    public class PlayerInteraction : MonoBehaviour
    {
        [FormerlySerializedAs("detect")]
        [SerializeField] private InteractionSensor sensor;
        [SerializeField] private TradePanelController tradePanel;
        [SerializeField] private SeatController seats;
        private HUDController hud;

        // 거래 패널·모달이 열려 있거나 앉아 있으면 상호작용하지 않음
        public bool Blocked => (tradePanel != null && tradePanel.IsOpen) || (seats != null && seats.IsSeated) || (hud != null && hud.ModalOpen);

        private void Awake() => hud = GetComponentInChildren<HUDController>(true);

        // 바라보는 대상에 상호작용
        public void Interact()
        {
            var target = sensor != null && !Blocked ? sensor.Target : null;
            if (target != null && target.CanInteract) target.Interact(gameObject);
        }
    }
}
