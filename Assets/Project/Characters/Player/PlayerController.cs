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
            playerMove.MoveInputChanged += OnMoveInputChanged;

            if (radialMenu != null)
                radialMenu.SelectionConfirmed += HandleRadialMenuSelection;

            if (tradePanel != null)
            {
                tradePanel.OpenStateChanged += SetControlsBlocked;
                SetControlsBlocked(tradePanel.IsOpen);
            }
            OnMoveInputChanged(playerMove.MoveInput);
        }

        private void OnDisable()
        {
            playerMove.GroundedChanged -= SetGroundedAnimation;
            playerMove.Jumped -= PlayJumpAnimation;
            playerMove.MoveInputChanged -= OnMoveInputChanged;

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
            if (controlsBlocked || BuyBidBlow.UI.Notebook.NetworkNotebookController.BlocksInput || (radialMenu != null && radialMenu.IsOpen)) return;

            ApplyMoveInput(bufferedMoveInput);
        }

        public void OnRotate(InputAction.CallbackContext context)
        {
            if (controlsBlocked || BuyBidBlow.UI.Notebook.NetworkNotebookController.BlocksInput || (radialMenu != null && radialMenu.IsOpen)) return;
            playerMove.Rotate(
                context.ReadValue<Vector2>()
            );
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (controlsBlocked || BuyBidBlow.UI.Notebook.NetworkNotebookController.BlocksInput) return;

            if (context.started)
            {
                StopEmotionForInput("Player_Idle");
                playerMove.SetJumpHeld(true);
            }
            else if (context.canceled)
                playerMove.SetJumpHeld(false);
        }

        private void SetGroundedAnimation(bool isGrounded)
        {
            playerAnimation.SetBool("isGroud", isGrounded);
            if (TryGetComponent(out PlayerNet net)) net.SetGroundedAnimation(isGrounded);
        }

        private void PlayJumpAnimation()
        {
            ResetAnimationTrigger(JumpTrigger);
            SetAnimationTrigger(JumpTrigger);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (controlsBlocked || BuyBidBlow.UI.Notebook.NetworkNotebookController.BlocksInput) return;

            // Shift shares the existing Interction action with E. Inspect the
            // triggering control so E keeps its existing trade behaviour.
            if (IsShiftControl(context))
            {
                if (context.started)
                {
                    // Clear the already-applied movement input before opening
                    // the menu, otherwise the player keeps moving while input
                    // callbacks are intentionally ignored.
                    playerMove.SetMoveInput(Vector2.zero);
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
                playerInteraction.Interact();
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
                    SetAnimationTrigger("Defeat");
                    break;

                case 2:
                    SetAnimationTrigger("Flair");
                    break;

                case 3:
                    SetAnimationTrigger("Hadraise");
                    break;

                case 4:
                    SetAnimationTrigger("HipHopDance");
                    break;
            }
        }

        private void StopEmotionForInput(string nextState)
        {
            if (!emotionPlaying)
                return;

            emotionPlaying = false;
            playerAnimation.ResetTrigger("Defeat");
            playerAnimation.ResetTrigger("Flair");
            playerAnimation.ResetTrigger("Hadraise");
            playerAnimation.ResetTrigger("HipHopDance");
            playerAnimation.Play(nextState, 0, 0f);
            if (TryGetComponent(out PlayerNet net))
                net.CancelEmotion(nextState == "Player_Walking");
        }

        private void ApplyMoveInput(Vector2 moveInput)
        {
            playerMove.SetMoveInput(moveInput);
        }

        private void OnMoveInputChanged(Vector2 moveInput)
        {
            if (moveInput != Vector2.zero)
                StopEmotionForInput("Player_Walking");

            playerAnimation.SetBool("isWalking", moveInput != Vector2.zero);
            if (TryGetComponent(out PlayerNet net))
                net.SetWalkingAnimation(moveInput != Vector2.zero);
        }

        private void SetAnimationTrigger(string trigger)
        {
            playerAnimation.SetTrigger(trigger);
            if (TryGetComponent(out PlayerNet net))
                net.PlayAnimationTrigger(trigger);
        }

        private void SetAnimationTrigger(int trigger)
        {
            playerAnimation.SetTrigger(trigger);
            if (TryGetComponent(out PlayerNet net))
                net.PlayAnimationTrigger(trigger);
        }

        private void ResetAnimationTrigger(int trigger)
        {
            playerAnimation.ResetTrigger(trigger);
        }
    }
}
