using BuyBidBlow.Characters;
using BuyBidBlow.UI.Market;
using UnityEngine;

namespace BuyBidBlow.UI.HUD
{
    // The only owner of the in-match cursor. It stays locked unless the trade panel or the emotion menu
    // needs it, a screen blocks gameplay input (PlayerInputGate), or the player holds Left Alt.
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
            bool free = (tradePanel != null && tradePanel.IsOpen)
                || PlayerInputGate.Blocked
                || (radialMenu != null && radialMenu.IsOpen)
                || PlayerInputGate.FreeCursorHeld;
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
