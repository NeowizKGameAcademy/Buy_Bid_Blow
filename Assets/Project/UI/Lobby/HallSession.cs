using BuyBidBlow.Characters;
using BuyBidBlow.Network;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BuyBidBlow.UI.Lobby
{
    // The lobby and match share the same loaded hall and its lighting/physics.
    // Only presentation and the local player's input are switched on match start.
    public sealed class HallSession : MonoBehaviour
    {
        public GameNetworkManager room;
        public GameObject lobbyPresentation;
        public GameObject gameplayPresentation;
        public PlayerSeating player;
        public bool IsPlaying { get; private set; }

        // 매치 시작 이벤트를 구독하고 이미 매치 중이면 바로 시작
        void OnEnable()
        {
            room.ClientMatchStarted += BeginMatch;
            room.ClientMatchEnded += ReturnToLobby;
            if (room.ClientInMatch) BeginMatch();
        }
        // 매치 시작 이벤트 구독 해제 및 플레이 중이었다면 커서 해제
        void OnDisable()
        {
            if (room) { room.ClientMatchStarted -= BeginMatch; room.ClientMatchEnded -= ReturnToLobby; }
            if (IsPlaying) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        }

        // 매치 전이면 로비 화면을 켜고 게임플레이 화면을 끈 뒤 커서 표시
        void Start()
        {
            if (IsPlaying) return;
            gameplayPresentation.SetActive(false);
            lobbyPresentation.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ReturnToLobby()
        {
            IsPlaying = false;
            gameplayPresentation.SetActive(false);
            lobbyPresentation.SetActive(true);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        // 로비 화면을 끄고 로컬 플레이어를 자리로 배치·외형 적용해 같은 홀에서 매치 시작
        void BeginMatch()
        {
            if (IsPlaying) return;
            var local = NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;
            if (!local) { Debug.LogError("Cannot enter the match without the admitted local player.",this); return; }
            IsPlaying = true;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            lobbyPresentation.SetActive(false);
            gameplayPresentation.SetActive(true);
            // Start in the hall at the avatar's own slot, keeping the six players apart.
            var start = PlayerNet.MatchStartPosition(local.seat); var facing = Quaternion.Euler(0,180,0);
            if (player.TryGetComponent<CharacterMotor>(out var motor)) motor.Teleport(start, facing);
            else player.transform.SetPositionAndRotation(start, facing);
            var appearance=player.GetComponentInChildren<BBBWolfAppearance>(true);
            if (appearance) { appearance.SetOutfit(local.top,local.bottom); appearance.SetTieColor(PlayerNet.Colors[Mathf.Clamp(local.tie,0,PlayerNet.Colors.Length-1)]); }
            Physics.SyncTransforms();
            if (player.TryGetComponent<CapsuleCollider>(out var capsule))
            {
                foreach (var rope in FindObjectsByType<BuyBidBlow.Props.RopeBarrier>(FindObjectsSortMode.None))
                    if (rope.contacts == null || rope.contacts.Length == 0) rope.contacts = new[] { capsule };
                foreach (var banner in FindObjectsByType<BuyBidBlow.Props.BannerWind>(FindObjectsSortMode.None)) banner.SetContacts(new[] { capsule });
            }
            Debug.Log("[Hall] Match started in the existing hall; no scene load.");
        }
    }
}
