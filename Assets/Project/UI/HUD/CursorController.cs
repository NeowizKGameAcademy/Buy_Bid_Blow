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

        private void Start()
        {
            wantLocked = !(tradePanel != null && tradePanel.IsOpen);
            if (wantLocked) LockCursor();
            else UnlockCursor();
        }

        private void OnEnable()
        {
            if (tradePanel != null)
                tradePanel.OpenStateChanged += OnPanelStateChanged;
        }

        private void OnDisable()
        {
            if (tradePanel != null)
                tradePanel.OpenStateChanged -= OnPanelStateChanged;
        }

        private void OnPanelStateChanged(bool isOpen)
        {
            wantLocked = !isOpen;
            if (isOpen) UnlockCursor();
        }

        // In the Editor, Esc also releases the lock after this frame's input, and a single LockCursor call
        // then loses to it. So while playing the lock is reasserted every frame until the player asks
        // for the cursor (Esc with nothing open) or a panel needs it.
        private void Update()
        {
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

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
