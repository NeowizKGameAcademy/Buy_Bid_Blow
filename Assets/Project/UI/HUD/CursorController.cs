using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.UI.HUD
{
    // The only owner of the in-match cursor. It stays locked unless the trade panel or the emotion menu
    // needs it, or the player holds Left Alt. The shared Esc game menu also unlocks the cursor.
    // Q closes trading and stands up (SeatController).
    public class CursorController : MonoBehaviour
    {
        [SerializeField] private TradePanelController tradePanel;
        [SerializeField] private RadialMenuController radialMenu;
        [SerializeField] private Texture2D pointer;
        [SerializeField] private Vector2 hotspot = new Vector2(.37f,.11f);
        void OnEnable(){if(pointer)Cursor.SetCursor(pointer,new Vector2(pointer.width*hotspot.x,pointer.height*hotspot.y),CursorMode.Auto);}
        void OnDisable(){Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);}

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
                || BuyBidBlow.UI.Notebook.NetworkNotebookController.BlocksInput
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
