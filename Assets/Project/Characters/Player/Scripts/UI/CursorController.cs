using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow
{
    public class CursorController : MonoBehaviour
    {
        private void Start()
        {
            LockCursor();
        }

        private void Update()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                UnlockCursor();

            if (Mouse.current.leftButton.wasPressedThisFrame &&
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
