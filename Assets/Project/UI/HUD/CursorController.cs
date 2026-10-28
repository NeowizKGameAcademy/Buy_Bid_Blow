using BuyBidBlow.Characters;
using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.UI.HUD
{
    public class CursorController : MonoBehaviour
    {
        [SerializeField] private TradePanelController tradePanel;
        // While seated, Esc stands the player up (SeatController) instead of releasing the cursor.
        [SerializeField] private PlayerSeating seating;
        private bool wantLocked = true;
        private bool busyLastFrame;
        private HUDController hud;
        private RadialMenuController radial;

        // 시작 시 거래 패널 상태에 맞춰 커서를 잠그거나 풂
        private void Start()
        {
            hud = tradePanel ? tradePanel.GetComponent<HUDController>() : null;
            radial = seating ? seating.GetComponentInChildren<RadialMenuController>(true) : GetComponentInChildren<RadialMenuController>(true);
            wantLocked = !(tradePanel != null && tradePanel.IsOpen);
            if (wantLocked) LockCursor();
            else UnlockCursor();
        }

        // 거래 패널 열림/닫힘 이벤트 구독
        private void OnEnable()
        {
            if (tradePanel != null)
                tradePanel.OpenStateChanged += OnPanelStateChanged;
        }

        // 거래 패널 이벤트 구독 해제
        private void OnDisable()
        {
            if (tradePanel != null)
                tradePanel.OpenStateChanged -= OnPanelStateChanged;
        }

        // 패널이 열리면 커서를 풀고, 닫히면 다시 잠그도록 표시
        private void OnPanelStateChanged(bool isOpen)
        {
            wantLocked = !isOpen;
            if (isOpen) UnlockCursor();
        }

        // 매 프레임 Esc/클릭/패널/착석 상태를 보고 커서 잠금 여부를 결정해 적용
        // In the Editor, Esc also releases the lock after this frame's input, and a single LockCursor call
        // then loses to it. So while playing the lock is reasserted every frame until the player asks
        // for the cursor (Esc with nothing open) or a panel needs it.
        private void Update()
        {
            if ((hud && hud.ModalOpen) || (radial && radial.IsOpen))
            {
                UnlockCursor(); wantLocked = true; busyLastFrame = true; return;
            }
            var keyboard = Keyboard.current;
            bool escape = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            bool panelOpen = tradePanel != null && tradePanel.IsOpen;
            if (panelOpen)
            {
                if (escape) tradePanel.Close();
                else UnlockCursor();
                busyLastFrame = true;
                return;
            }

            // Esc that closed a panel or stood the player up last frame is not a request for the cursor.
            bool seated = seating != null && seating.IsSeated;
            if (escape && !seated && !busyLastFrame)
                wantLocked = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                wantLocked = true;
            busyLastFrame = seated;

            if (wantLocked && (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)) LockCursor();
            else if (!wantLocked && Cursor.lockState == CursorLockMode.Locked) UnlockCursor();
        }

        // 커서를 화면 중앙에 잠그고 숨김
        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 커서 잠금을 풀고 보이게 함
        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
