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

        private void OnEnable()
        {
            seating.onSeatedChanged.AddListener(ShowSeated);
            PlayerNet.LocalSeatChanged += OnServerSeat;
        }

        private void OnDisable()
        {
            seating.onSeatedChanged.RemoveListener(ShowSeated);
            PlayerNet.LocalSeatChanged -= OnServerSeat;
        }

        private void Update()
        {
            if (IsSeated && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                RequestStand();
        }

        public void RequestSit(Seat seat)
        {
            if (IsSeated || !seat) return;
            int slot = seat.FreeSlot();
            if (slot < 0) return;
            var net = LocalNet;
            if (net && net.inMatch) net.CmdSit(seat.Id, slot);
            else seat.Interact(gameObject);
        }

        public void RequestStand()
        {
            if (!IsSeated) return;
            var net = LocalNet;
            if (net && net.inMatch) net.CmdStand();
            else Stand();
        }

        private void OnServerSeat(string seatId, int slot)
        {
            if (string.IsNullOrEmpty(seatId)) { Stand(); return; }
            var seat = Seat.Find(seatId);
            if (!seat) { RequestStand(); return; }
            if (IsSeated) Stand();
            seat.SitAt(seating, slot);
        }

        private void Stand()
        {
            if (IsSeated && !seating.TryStand()) seating.ForceStand();
        }

        private void ShowSeated(bool seated)
        {
            if (!model) return;
            model.SetBool("isWalking", false);
            model.Play(seated ? SeatedState : IdleState, 0, 0);
        }
    }
}
