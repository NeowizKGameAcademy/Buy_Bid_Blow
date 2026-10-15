using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerMove playerMove;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private TradePanelController tradePanel;
        private bool controlsBlocked;

        private void Awake()
        {
            if (playerMove == null)
                TryGetComponent(out playerMove);

            if (playerInteraction == null)
                TryGetComponent(out playerInteraction);
        }

        private void OnEnable()
        {
            if (tradePanel != null)
            {
                tradePanel.OpenStateChanged += SetControlsBlocked;
                SetControlsBlocked(tradePanel.IsOpen);
            }
        }

        private void OnDisable()
        {
            if (tradePanel != null)
                tradePanel.OpenStateChanged -= SetControlsBlocked;
        }

        private void SetControlsBlocked(bool blocked)
        {
            controlsBlocked = blocked;
            playerMove.SetMoveInput(Vector2.zero);
            playerMove.SetJumpHeld(false);
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (controlsBlocked) return;
            playerMove.SetMoveInput(
                context.ReadValue<Vector2>()
            );
        }

        public void OnRotate(InputAction.CallbackContext context)
        {
            if (controlsBlocked) return;
            playerMove.Rotate(
                context.ReadValue<Vector2>()
            );
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (controlsBlocked) return;
            if (context.started)
                playerMove.SetJumpHeld(true);
            else if (context.canceled)
                playerMove.SetJumpHeld(false);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (controlsBlocked) return;
            if (context.performed)
                playerInteraction.Interact_OpenTradePanel();
        }
    }
}
