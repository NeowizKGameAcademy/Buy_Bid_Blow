using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Characters
{
    // The local player's controls (Input/PlayerControls): turns them into a motor intent, mouse look,
    // the interact request and the emote menu (held on the middle mouse button).
    // While a panel, modal or the emote menu is open the body stops and the view holds still.
    // Runs before the motor so this frame's intent is used this frame.
    [DefaultExecutionOrder(-10)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private PlayerLook look;
        [SerializeField] private PlayerInteraction interaction;
        [SerializeField] private TradePanelController tradePanel;
        [SerializeField] private RadialMenuController radial;
        // A jump pressed this long before landing still counts.
        [SerializeField] private float jumpBuffer = .12f;

        private InputAction move, lookAction, jump, interact, emote;
        private HUDController hud;
        private float jumpPressedAt = float.NegativeInfinity;

        private bool UiOpen => (tradePanel && tradePanel.IsOpen) || (hud && hud.ModalOpen) || (radial && radial.IsOpen);

        private void Awake()
        {
            if (!motor) TryGetComponent(out motor);
            if (!look) TryGetComponent(out look);
            if (!interaction) TryGetComponent(out interaction);
            hud = GetComponentInChildren<HUDController>(true);
            var map = controls.FindActionMap("Player", true);
            move = map.FindAction("Move", true); lookAction = map.FindAction("Look", true); jump = map.FindAction("Jump", true);
            interact = map.FindAction("Interact", true); emote = map.FindAction("Emote", true);
        }

        private void OnEnable()
        {
            controls.FindActionMap("Player", true).Enable();
            motor.Jumped += SpendJump;
        }

        // 비활성화 시 입력을 끄고 이동 차단·메뉴 상태를 정리
        private void OnDisable()
        {
            controls.FindActionMap("Player", true).Disable();
            if (motor) { motor.Jumped -= SpendJump; motor.SetIntent(default); motor.Unblock(this); }
            if (radial) radial.EndSelection(false);
        }

        // 입력을 읽어 이동 의도·시선·상호작용·감정 메뉴로 전달
        private void Update()
        {
            if (emote.WasPressedThisFrame() && !(tradePanel && tradePanel.IsOpen) && !(hud && hud.ModalOpen) && radial) radial.BeginSelection();
            if (emote.WasReleasedThisFrame() && radial) radial.EndSelection(true);

            bool uiOpen = UiOpen;
            if (uiOpen) motor.Block(this); else motor.Unblock(this);

            if (jump.WasPressedThisFrame()) jumpPressedAt = Time.time;
            var input = uiOpen ? Vector2.zero : move.ReadValue<Vector2>();
            bool wantsJump = !uiOpen && Time.time - jumpPressedAt <= jumpBuffer;
            motor.SetIntent(new CharacterIntent { Move = transform.right * input.x + transform.forward * input.y, Jump = wantsJump });

            if (!uiOpen && Cursor.lockState == CursorLockMode.Locked && look) look.Look(lookAction.ReadValue<Vector2>());
            if (!uiOpen && interact.WasPressedThisFrame() && interaction) interaction.Interact();
        }

        // 점프가 실제로 일어나면 버퍼에 남은 입력을 소모
        private void SpendJump() => jumpPressedAt = float.NegativeInfinity;
    }
}
