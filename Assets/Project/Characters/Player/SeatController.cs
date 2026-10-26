using BuyBidBlow.Props;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Characters
{
    // The local player's chairs: sit and stand requests come from PlayerInteraction and PlayerInputHandler.
    // In a match the server grants the seat first (PlayerNet.hallSeat); offline the seat is taken directly.
    public sealed class SeatController : MonoBehaviour
    {
        [SerializeField] private PlayerSeating seating;

        private static PlayerNet LocalNet =>
            NetworkClient.active && NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;

        public bool IsSeated => seating && seating.IsSeated;

        private void OnEnable()
        {
            PlayerNet.LocalSeatChanged += OnServerSeat;
        }

        private void OnDisable()
        {
            PlayerNet.LocalSeatChanged -= OnServerSeat;
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
    }
}
