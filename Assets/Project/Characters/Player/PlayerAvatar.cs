using BuyBidBlow.UI.HUD;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Characters
{
    // Shows the local body's state on its wolf (BBBWolfCharacter), the same API remote avatars use:
    // walk speed, jump, seated pose and emotes from the radial menu. Jumps and emotes are also sent
    // through PlayerNet so other players see them.
    public sealed class PlayerAvatar : MonoBehaviour
    {
        // Radial menu items 1..4, in PlayerNet.RpcEmote's order.
        public static readonly BBBWolfCharacter.Action[] Emotes =
        {
            BBBWolfCharacter.Action.Defeat, BBBWolfCharacter.Action.Flair,
            BBBWolfCharacter.Action.HandRaising, BBBWolfCharacter.Action.HipHopDancing
        };

        [SerializeField] private CharacterMotor motor;
        [SerializeField] private BBBWolfCharacter character;
        [SerializeField] private PlayerSeating seating;
        [SerializeField] private RadialMenuController radial;

        private bool emoting;

        private static PlayerNet LocalNet =>
            NetworkClient.active && NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;

        private void Awake()
        {
            if (!motor) TryGetComponent(out motor);
            if (!seating) TryGetComponent(out seating);
            if (!character) character = GetComponentInChildren<BBBWolfCharacter>(true);
            if (!radial) radial = GetComponentInChildren<RadialMenuController>(true);
        }

        // 점프·착석·감정 메뉴 이벤트 구독
        private void OnEnable()
        {
            motor.Jumped += OnJumped;
            if (seating) seating.onSeatedChanged.AddListener(OnSeatedChanged);
            if (radial) radial.SelectionConfirmed += OnEmoteSelected;
        }

        // 이벤트 구독 해제
        private void OnDisable()
        {
            motor.Jumped -= OnJumped;
            if (seating) seating.onSeatedChanged.RemoveListener(OnSeatedChanged);
            if (radial) radial.SelectionConfirmed -= OnEmoteSelected;
        }

        // 이동 속도를 걷기 블렌드에 반영하고, 움직이기 시작하면 감정 표현을 멈춤
        private void Update()
        {
            if (!character) return;
            var v = motor.Velocity; v.y = 0;
            float walk = motor.IsBlocked ? 0 : v.magnitude / Mathf.Max(.01f, motor.MoveSpeed);
            if (emoting && walk > .1f) StopEmote();
            character.SetMovementSpeed(walk);
        }

        // 점프 동작을 재생하고 다른 플레이어에게 알림
        private void OnJumped()
        {
            emoting = false;
            if (character) character.PlayAction(BBBWolfCharacter.Action.Jump);
            LocalNet?.CmdJump();
        }

        // 앉은 자세 전환
        private void OnSeatedChanged(bool seated)
        {
            if (seated) StopEmote();
            if (character) character.SetSeated(seated);
        }

        // 선택한 감정 표현(1~4)을 재생하고 다른 플레이어에게 알림
        private void OnEmoteSelected(int number)
        {
            if (seating && seating.IsSeated || number < 1 || number > Emotes.Length) return;
            emoting = true;
            if (character) character.PlayAction(Emotes[number - 1]);
            LocalNet?.CmdEmote(number);
        }

        // 감정 표현을 멈추고 다른 플레이어에게 알림
        public void StopEmote()
        {
            if (!emoting) return;
            emoting = false;
            if (character) character.StopAction();
            LocalNet?.CmdEmote(0);
        }
    }
}
