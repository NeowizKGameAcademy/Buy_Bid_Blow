using BuyBidBlow.Characters;
using BuyBidBlow.Network;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BuyBidBlow.Presentation
{
    // The lobby and match share the same loaded hall and its lighting/physics.
    // Only presentation and the local player's input are switched on match start.
    public sealed class HallSession : MonoBehaviour
    {
        public GameNetworkManager room;
        public GameObject lobbyPresentation;
        public GameObject gameplayPresentation;
        public PlayerMove player;
        public bool IsPlaying { get; private set; }

        void OnEnable()
        {
            room.ClientMatchStarted += BeginMatch;
            room.ClientMatchReturned += ReturnToLobby;
            if (room.ClientInMatch) BeginMatch();
        }
        void OnDisable()
        {
            if (room) room.ClientMatchStarted -= BeginMatch;
            if (room) room.ClientMatchReturned -= ReturnToLobby;
            if (IsPlaying) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        }

        void Start()
        {
            if (IsPlaying) return;
            gameplayPresentation.SetActive(false);
            lobbyPresentation.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void ReturnToLobby()
        {
            var local=NetworkClient.localPlayer?NetworkClient.localPlayer.GetComponent<PlayerNet>():null;
            if(local)local.SetGameplayActive(false);
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);
            IsPlaying=false;gameplayPresentation.SetActive(false);lobbyPresentation.SetActive(true);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }

        void BeginMatch()
        {
            if (IsPlaying) return;
            var local = NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;
            if (!local) { Debug.LogError("Cannot enter the match without the admitted local player.",this); return; }
            player = local.GetComponent<PlayerMove>();
            if (!player) { Debug.LogError("Network player is missing PlayerMove.", this); return; }
            IsPlaying = true;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            lobbyPresentation.SetActive(false);
            // The spawned network Player owns the local camera, HUD and input.
            // The scene's old presentation rig remains disabled.
            local.SetGameplayActive(true);
            // Start at the avatar's own waiting position, keeping the six players apart.
            player.transform.SetPositionAndRotation(PlayerNet.SeatPosition(local.seat), Quaternion.Euler(0,180,0));
            gameplayPresentation.SetActive(false);
            player.SetMoveInput(Vector2.zero);
            if (player.TryGetComponent<Rigidbody>(out var body)) { body.linearVelocity=Vector3.zero; body.angularVelocity=Vector3.zero; }
            var appearance=player.GetComponentInChildren<BBBWolfAppearance>(true);
            if (appearance) { appearance.SetOutfit(local.top,local.bottom); appearance.SetTieColor(PlayerNet.Colors[Mathf.Clamp(local.tie,0,PlayerNet.Colors.Length-1)]); }
            Physics.SyncTransforms();
            Debug.Log("[Hall] Match started in the existing hall; no scene load.");
        }
    }
}
