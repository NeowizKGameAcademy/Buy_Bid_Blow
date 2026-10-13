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

        private void Awake()
        {
            if (playerMove == null)
                TryGetComponent(out playerMove);

            if (playerInteraction == null)
                TryGetComponent(out playerInteraction);
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            playerMove.SetMoveInput(
                context.ReadValue<Vector2>()
            );
        }

        public void OnRotate(InputAction.CallbackContext context)
        {
            playerMove.Rotate(
                context.ReadValue<Vector2>()
            );
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
                playerMove.SetJumpHeld(true);
            else if (context.canceled)
                playerMove.SetJumpHeld(false);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed)
                playerInteraction.Interact();
        }
    }
}
