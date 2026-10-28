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
        private bool modalBlocked;
        private bool emotionPlaying;
        private Vector2 bufferedMoveInput;

        private static readonly int JumpTrigger = Animator.StringToHash("Jump");

        // 비어 있는 참조를 자동으로 찾고 착지 애니메이션 상태를 초기화
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

        // 이동·점프·라디얼 메뉴·거래 패널 이벤트 구독
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

        // 구독한 이벤트 해제
        private void OnDisable()
        {
            bufferedMoveInput = Vector2.zero; modalBlocked = false; controlsBlocked = false; emotionPlaying = false;
            playerMove.GroundedChanged -= SetGroundedAnimation;
            playerMove.Jumped -= PlayJumpAnimation;

            if (radialMenu != null)
                radialMenu.SelectionConfirmed -= HandleRadialMenuSelection;

            if (tradePanel != null)
                tradePanel.OpenStateChanged -= SetControlsBlocked;
        }

        // 조작 차단 여부를 설정하고 이동·점프 입력을 초기화
        private void SetControlsBlocked(bool blocked)
        {
            controlsBlocked = blocked;
            playerMove.SetMoveInput(Vector2.zero);
            playerMove.SetJumpHeld(false);
        }
        public void SetModalBlocked(bool blocked)
        {
            modalBlocked = blocked;
            if (blocked) { playerMove.SetMoveInput(Vector2.zero); playerMove.SetJumpHeld(false); }
            else if (!controlsBlocked && isActiveAndEnabled) ApplyMoveInput(bufferedMoveInput);
        }

        // 이동 입력을 버퍼에 저장하고 조작 가능할 때 적용
        public void OnMove(InputAction.CallbackContext context)
        {
            bufferedMoveInput = context.ReadValue<Vector2>();
            if (controlsBlocked || modalBlocked || (radialMenu != null && radialMenu.IsOpen)) return;

            ApplyMoveInput(bufferedMoveInput);
        }

        // 조작 가능할 때 마우스 회전 입력을 이동 컴포넌트에 전달
        public void OnRotate(InputAction.CallbackContext context)
        {
            if (controlsBlocked || modalBlocked || (radialMenu != null && radialMenu.IsOpen)) return;
            playerMove.Rotate(
                context.ReadValue<Vector2>()
            );
        }

        // 점프 키 누름/뗌에 따라 점프 유지 상태를 설정하고 감정 표현 해제
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.canceled) { playerMove.SetJumpHeld(false); return; }
            if (controlsBlocked || modalBlocked || (radialMenu && radialMenu.IsOpen)) return;

            if (context.started)
            {
                emotionPlaying = false;
                playerMove.SetJumpHeld(true);
            }
            else if (context.canceled)
                playerMove.SetJumpHeld(false);
        }

        // 애니메이터의 착지 상태 값을 갱신
        private void SetGroundedAnimation(bool isGrounded)
        {
            playerAnimation.SetBool("isGroud", isGrounded);
        }

        // 점프 트리거를 리셋 후 다시 발동
        private void PlayJumpAnimation()
        {
            playerAnimation.ResetTrigger(JumpTrigger);
            playerAnimation.SetTrigger(JumpTrigger);
            LocalNet()?.CmdJump();
        }

        // Shift면 라디얼 메뉴를 열고 닫으며, E면 감정 표현을 멈추고 상호작용 실행
        public void OnInteract(InputAction.CallbackContext context)
        {
            if (controlsBlocked || modalBlocked) return;

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
                    playerMove.SetJumpHeld(false);
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
                if (radialMenu && radialMenu.IsOpen) return;
                StopEmotionForInput("Player_Idle");
                playerInteraction.Interact();
            }
        }

        // 입력을 발생시킨 컨트롤이 Shift 키인지 확인
        private static bool IsShiftControl(InputAction.CallbackContext context)
        {
            var controlPath = context.control?.path;
            return !string.IsNullOrEmpty(controlPath)
                && controlPath.ToLowerInvariant().Contains("shift");
        }

        // 선택한 라디얼 메뉴 번호에 맞는 감정 표현 애니메이션 재생
        private void HandleRadialMenuSelection(int menuNumber)
        {
            Debug.Log($"Radial menu selected: {menuNumber}", this);
            emotionPlaying = true;
            LocalNet()?.CmdEmote(menuNumber);

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

        // 감정 표현 중이면 중단하고 지정한 애니메이션 상태로 전환
        private void StopEmotionForInput(string nextState)
        {
            if (!emotionPlaying)
                return;

            emotionPlaying = false;
            LocalNet()?.CmdEmote(0);
            playerAnimation.Play(nextState, 0, 0f);
        }

        // 이동 입력을 적용하고 걷기 애니메이션 상태를 갱신
        private void ApplyMoveInput(Vector2 moveInput)
        {
            if (moveInput != Vector2.zero)
                StopEmotionForInput("Player_Walking");

            playerMove.SetMoveInput(moveInput);
            playerAnimation.SetBool("isWalking", moveInput != Vector2.zero);
        }
        private static PlayerNet LocalNet() => Mirror.NetworkClient.localPlayer ? Mirror.NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;
    }
}
