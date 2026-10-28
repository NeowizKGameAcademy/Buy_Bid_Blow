using BuyBidBlow.Props;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Characters
{
    // The local player's chairs: E on a free seat asks to sit, Esc stands up. In a match the server
    // grants the seat first (PlayerNet.hallSeat); offline the seat is taken directly.
    public sealed class SeatController : MonoBehaviour
    {
        const string SeatedState = "Seated";
        const string IdleState = "Player_Idle";

        [SerializeField] private PlayerSeating seating;
        [SerializeField] private Animator model;

        private static PlayerNet LocalNet =>
            NetworkClient.active && NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;

        public bool IsSeated => seating && seating.IsSeated;

        // 착석 상태 변경과 서버 좌석 변경 이벤트 구독
        private void OnEnable()
        {
            seating.onSeatedChanged.AddListener(ShowSeated);
            PlayerNet.LocalSeatChanged += OnServerSeat;
        }

        // 이벤트 구독 해제
        private void OnDisable()
        {
            var net = LocalNet;
            if (net && net.inMatch && net.IsSeated) net.CmdStand();
            seating.onSeatedChanged.RemoveListener(ShowSeated);
            PlayerNet.LocalSeatChanged -= OnServerSeat;
        }

        // 앉아 있을 때 Esc를 누르면 일어서기 요청
        private void Update()
        {
            var hud = GetComponentInChildren<BuyBidBlow.UI.HUD.HUDController>(true);
            if (hud && (hud.ModalOpen || hud.ModalClosedFrame == Time.frameCount)) return;
            if (IsSeated && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                RequestStand();
        }

        // 빈 슬롯이 있으면 매치 중엔 서버에, 오프라인이면 직접 착석 요청
        public void RequestSit(Seat seat)
        {
            if (IsSeated || !seat) return;
            int slot = seat.FreeSlot();
            if (slot < 0) return;
            var net = LocalNet;
            if (NetworkClient.active)
            {
                if (net && net.inMatch) net.CmdSit(seat.Id, slot);
                return;
            }
            seat.Interact(gameObject);
        }

        // 매치 중엔 서버에 요청하고, 오프라인이면 직접 일어서기
        public void RequestStand()
        {
            var net = LocalNet;
            if (net && net.inMatch && net.IsSeated) net.CmdStand();
            else if (!NetworkClient.active) Stand();
        }

        // 서버가 정한 좌석에 앉히거나 일어서게 함
        private void OnServerSeat(string seatId, int slot)
        {
            if (string.IsNullOrEmpty(seatId)) { Stand(); return; }
            var seat = Seat.Find(seatId);
            if (!seat) { RequestStand(); return; }
            if (IsSeated) Stand();
            seat.SitAt(seating, slot);
            if (!IsSeated) RequestStand();
        }

        // 출구로 일어서기를 시도하고 실패하면 강제로 일어섬
        private void Stand()
        {
            if (IsSeated && !seating.TryStand()) seating.ForceStand();
        }

        // 착석 여부에 맞는 애니메이션 상태 재생
        private void ShowSeated(bool seated)
        {
            var net = LocalNet;
            if (!seated && net && net.inMatch && net.IsSeated) net.CmdStand();
            if (!model) return;
            model.SetBool("isWalking", false);
            model.Play(seated ? SeatedState : IdleState, 0, 0);
        }
    }
}
