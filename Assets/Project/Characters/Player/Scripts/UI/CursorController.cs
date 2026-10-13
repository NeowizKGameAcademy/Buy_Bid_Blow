using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow
{
    public class CursorController : MonoBehaviour
    {
        [SerializeField] private TradePanelController tradePanel;

        private void Start()
        {
            if (tradePanel != null && tradePanel.IsOpen)
                UnlockCursor();
            else
                LockCursor();
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
            if (isOpen) UnlockCursor();
            else LockCursor();
        }

        private void Update()
        {
            if (tradePanel != null && tradePanel.IsOpen)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                    tradePanel.Close();
                else
                    UnlockCursor();
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                UnlockCursor();

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
            }
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
