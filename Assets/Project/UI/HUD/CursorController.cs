using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.UI.HUD
{
    // The only owner of the in-match cursor. It stays locked unless the trade panel or the emotion menu
    // needs it, or the player holds Left Alt. Esc is not used in play: the Editor's Game view drops the
    // lock on Esc by itself, so Q closes the panel (and stands up, see SeatController) instead.
    public class CursorController : MonoBehaviour
    {
        [SerializeField] private TradePanelController tradePanel;
        [SerializeField] private RadialMenuController radialMenu;

        private void Update()
        {
            var keyboard = Keyboard.current;
            bool panelOpen = tradePanel != null && tradePanel.IsOpen;
            if (panelOpen && keyboard != null && keyboard.qKey.wasPressedThisFrame)
            {
                tradePanel.Close();
                panelOpen = false;
            }

            bool free = panelOpen
                || (radialMenu != null && radialMenu.IsOpen)
                || (keyboard != null && keyboard.leftAltKey.isPressed);
            // Reasserted every frame: the lock comes back once the game window has focus again.
            if (free) UnlockCursor();
            else LockCursor();
        }

        public void LockCursor()
        {
            if (Cursor.lockState == CursorLockMode.Locked && !Cursor.visible) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            if (Cursor.lockState == CursorLockMode.None && Cursor.visible) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
