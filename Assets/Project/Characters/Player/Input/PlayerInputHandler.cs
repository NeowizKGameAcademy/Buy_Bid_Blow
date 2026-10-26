using BuyBidBlow.UI.Auction;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Characters
{
    // Receives PlayerInput events and gameplay keys and routes them to movement, interaction, seats,
    // the trade panel, the auction seat panel and emotions. Animation reacts to those components;
    // nothing here touches the Animator. Open screens block input through PlayerInputGate.
    public class PlayerInputHandler : MonoBehaviour
    {
        [SerializeField] private PlayerMove playerMove;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private PlayerAnimation characterAnimation;
        [SerializeField] private SeatController seats;
        [SerializeField] private AuctionSeatPanel auctionSeat;
        [SerializeField] private TradePanelController tradePanel;
        [SerializeField] private RadialMenuController radialMenu;

        private bool controlsBlocked;
        private Vector2 bufferedMoveInput;

        private bool RadialMenuOpen => radialMenu != null && radialMenu.IsOpen;

        private void Awake()
        {
            if (playerMove == null)
                TryGetComponent(out playerMove);

            if (playerInteraction == null)
                TryGetComponent(out playerInteraction);

            if (characterAnimation == null)
                TryGetComponent(out characterAnimation);

            if (seats == null)
                TryGetComponent(out seats);

            if (auctionSeat == null)
                auctionSeat = GetComponentInChildren<AuctionSeatPanel>(true);

            if (radialMenu == null)
                radialMenu = GetComponentInChildren<RadialMenuController>(true);
        }

        private void OnEnable()
        {
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
            if (radialMenu != null)
                radialMenu.SelectionConfirmed -= HandleRadialMenuSelection;

            if (tradePanel != null)
                tradePanel.OpenStateChanged -= SetControlsBlocked;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Q leaves the trade panel first; it is checked before the gate, as the panel itself is open.
            if (tradePanel != null && tradePanel.IsOpen && keyboard.qKey.wasPressedThisFrame)
            {
                tradePanel.Close();
                return;
            }

            if (PlayerInputGate.Blocked) return;

            if (seats != null && seats.IsSeated && keyboard.qKey.wasPressedThisFrame)
                seats.RequestStand();

            // On an auction chair: A/D amount, E bid, C cancel the secret submission.
            if (auctionSeat != null && auctionSeat.SeatedForAuction)
            {
                if (keyboard.dKey.wasPressedThisFrame) auctionSeat.AdjustBid(1);
                if (keyboard.aKey.wasPressedThisFrame) auctionSeat.AdjustBid(-1);
                if (keyboard.eKey.wasPressedThisFrame) auctionSeat.SubmitBid();
                if (keyboard.cKey.wasPressedThisFrame) auctionSeat.CancelSecret();
            }
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
            if (controlsBlocked || PlayerInputGate.Blocked || RadialMenuOpen) return;

            playerMove.SetMoveInput(bufferedMoveInput);
        }

        public void OnRotate(InputAction.CallbackContext context)
        {
            if (characterAnimation != null && characterAnimation.EmotionPlaying) return;
            if (controlsBlocked || PlayerInputGate.Blocked || RadialMenuOpen) return;
            playerMove.Rotate(context.ReadValue<Vector2>());
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (controlsBlocked || PlayerInputGate.Blocked) return;

            if (context.started)
            {
                characterAnimation?.StopEmotion(false);
                playerMove.SetJumpHeld(true);
            }
            else if (context.canceled)
                playerMove.SetJumpHeld(false);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (controlsBlocked || PlayerInputGate.Blocked) return;

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
                    playerMove.SetMoveInput(bufferedMoveInput);
                }

                return;
            }

            if (context.performed)
            {
                characterAnimation?.StopEmotion(false);
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
            characterAnimation?.PlayEmotion(menuNumber);
        }
    }
}
