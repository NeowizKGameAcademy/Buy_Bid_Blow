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
        [SerializeField] private Animator playerAnimation;
        [SerializeField] private TradePanelController tradePanel;
        [SerializeField] private RadialMenuController radialMenu;

        private bool controlsBlocked;
        private bool emotionPlaying;

        private static readonly int JumpTrigger = Animator.StringToHash("Jump");

        private void Awake()
        {
            if (playerMove == null)
                TryGetComponent(out playerMove);

            if (playerInteraction == null)
                TryGetComponent(out playerInteraction);

            if (playerAnimation == null)
                playerAnimation = GetComponentInChildren<Animator>();

            if (radialMenu == null)
                radialMenu = GetComponentInChildren<RadialMenuController>(true);

            playerAnimation.SetBool("isGroud", playerMove.IsGrounded);
        }

        private void OnEnable()
        {
            playerMove.GroundedChanged += SetGroundedAnimation;
            playerMove.Jumped += PlayJumpAnimation;

            if (radialMenu != null)
                radialMenu.SelectionConfirmed += HandleRadialMenuSelection;

            if (tradePanel != null)
            {
                tradePanel.OpenStateChanged += SetControlsBlocked;
                SetControlsBlocked(tradePanel.IsOpen);
            }
        }

        private void OnDisable()
        {
            playerMove.GroundedChanged -= SetGroundedAnimation;
            playerMove.Jumped -= PlayJumpAnimation;

            if (radialMenu != null)
                radialMenu.SelectionConfirmed -= HandleRadialMenuSelection;

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
            if (controlsBlocked || (radialMenu != null && radialMenu.IsOpen)) return;

            Vector2 moveInput = context.ReadValue<Vector2>();

            if (moveInput != Vector2.zero)
                StopEmotionForInput("Player_Walking");

            playerMove.SetMoveInput(moveInput);
            playerAnimation.SetBool("isWalking", moveInput != Vector2.zero);
        }

        public void OnRotate(InputAction.CallbackContext context)
        {
            if (controlsBlocked || (radialMenu != null && radialMenu.IsOpen)) return;
            playerMove.Rotate(
                context.ReadValue<Vector2>()
            );
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (controlsBlocked) return;

            if (context.started)
            {
                emotionPlaying = false;
                playerMove.SetJumpHeld(true);
            }
            else if (context.canceled)
                playerMove.SetJumpHeld(false);
        }

        private void SetGroundedAnimation(bool isGrounded)
        {
            playerAnimation.SetBool("isGroud", isGrounded);
        }

        private void PlayJumpAnimation()
        {
            playerAnimation.ResetTrigger(JumpTrigger);
            playerAnimation.SetTrigger(JumpTrigger);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (controlsBlocked) return;

            // Shift shares the existing Interction action with E. Inspect the
            // triggering control so E keeps its existing trade behaviour.
            if (IsShiftControl(context))
            {
                if (context.started)
                    radialMenu?.BeginSelection();
                else if (context.canceled)
                    radialMenu?.EndSelection(true);

                return;
            }

            if (context.performed)
            {
                StopEmotionForInput("Player_Idle");
                playerInteraction.Interact_OpenTradePanel();
            }
        }

        private static bool IsShiftControl(InputAction.CallbackContext context)
        {
            var controlPath = context.control?.path;
            return !string.IsNullOrEmpty(controlPath)
                && controlPath.ToLowerInvariant().Contains("shift");
        }

        private void HandleRadialMenuSelection(int menuNumber)
        {
            Debug.Log($"Radial menu selected: {menuNumber}", this);
            emotionPlaying = true;

            switch (menuNumber)
            {
                case 1:
                    playerAnimation.SetTrigger("Defeat");
                    break;

                case 2:
                    playerAnimation.SetTrigger("Flair");
                    break;

                case 3:
                    playerAnimation.SetTrigger("Hadraise");
                    break;

                case 4:
                    playerAnimation.SetTrigger("HipHopDance");
                    break;
            }
        }

        private void StopEmotionForInput(string nextState)
        {
            if (!emotionPlaying)
                return;

            emotionPlaying = false;
            playerAnimation.Play(nextState, 0, 0f);
        }
    }
}
