using System.Collections;
using System.Collections.Generic;
using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.UI.HUD
{
    public class CursorController : MonoBehaviour
    {
        [SerializeField] private TradePanelController tradePanel;
        private Coroutine lockCursorRoutine;

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

            if (lockCursorRoutine != null)
                StopCoroutine(lockCursorRoutine);
        }

        private void OnPanelStateChanged(bool isOpen)
        {
            if (isOpen)
            {
                if (lockCursorRoutine != null)
                {
                    StopCoroutine(lockCursorRoutine);
                    lockCursorRoutine = null;
                }

                UnlockCursor();
                return;
            }

            // ESC can release the lock after this frame's input callbacks. Locking on
            // the following frame keeps the cursor hidden whether Close came from ESC
            // or from the panel's close button.
            if (lockCursorRoutine != null)
                StopCoroutine(lockCursorRoutine);
            lockCursorRoutine = StartCoroutine(LockCursorWhenPanelHasClosed());
        }

        private IEnumerator LockCursorWhenPanelHasClosed()
        {
            yield return null;

            if (tradePanel != null && !tradePanel.IsOpen)
                LockCursor();

            lockCursorRoutine = null;
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
