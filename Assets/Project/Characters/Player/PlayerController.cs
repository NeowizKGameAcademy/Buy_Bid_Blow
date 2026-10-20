using System.Collections;
using System.Collections.Generic;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Characters
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
        private Vector2 bufferedMoveInput;

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
            bufferedMoveInput = context.ReadValue<Vector2>();
            if (IsSeated)
            {
                bufferedMoveInput = Vector2.zero;
                StopMovementForSeat();
                return;
            }

            if (controlsBlocked || (radialMenu != null && radialMenu.IsOpen)) return;

            ApplyMoveInput(bufferedMoveInput);
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
            if (controlsBlocked || IsSeated)
            {
                playerMove.SetJumpHeld(false);
                return;
            }

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

            var isShift = IsShiftControl(context);
            if (IsSeated)
            {
                // E is reserved for releasing the seat. All other interaction
                // controls, including the Shift emotion menu, stay unavailable.
                StopMovementForSeat();
                if (!isShift && context.performed)
                    playerInteraction?.Interact();
                return;
            }

            // Shift shares the existing Interction action with E. Inspect the
            // triggering control so E keeps its existing trade behaviour.
            if (isShift)
            {
                if (context.started)
                {
                    // Clear the already-applied movement input before opening
                    // the menu, otherwise the player keeps moving while input
                    // callbacks are intentionally ignored.
                    playerMove.SetMoveInput(Vector2.zero);
                    playerAnimation.SetBool("isWalking", false);
                    radialMenu?.BeginSelection();
                }
                else if (context.canceled)
                {
                    radialMenu?.EndSelection(true);
                    ApplyMoveInput(bufferedMoveInput);
                }

                return;
            }

            if (context.performed)
            {
                StopEmotionForInput("Player_Idle");
                playerInteraction?.Interact();
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
            if (IsSeated)
                return;

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

        private void ApplyMoveInput(Vector2 moveInput)
        {
            if (moveInput != Vector2.zero)
                StopEmotionForInput("Player_Walking");

            playerMove.SetMoveInput(moveInput);
            playerAnimation.SetBool("isWalking", moveInput != Vector2.zero);
        }

        private bool IsSeated => TryGetComponent(out PlayerSeating seating) && seating.IsSeated;

        private void StopMovementForSeat()
        {
            playerMove.SetMoveInput(Vector2.zero);
            playerMove.SetJumpHeld(false);
            playerAnimation.SetBool("isWalking", false);
            StopEmotionForInput("Player_Idle");
        }

        private void Update()
        {
            // Clear input that was active just before sitting down, so neither
            // movement nor its animation can remain active while seated.
            if (IsSeated)
                StopMovementForSeat();
        }
    }
}
