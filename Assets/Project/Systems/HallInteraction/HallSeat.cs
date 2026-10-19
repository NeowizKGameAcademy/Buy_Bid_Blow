using UnityEngine;
using UnityEngine.Events;

namespace BuyBidBlow.Hall
{
    public sealed class HallSeat : HallInteractable
    {
        public Transform[] sittingPoints;
        public Transform[] exitPoints;
        public UnityEvent<bool> onOccupancyChanged = new UnityEvent<bool>();
        HallSeatActor[] occupants;
        public override string Prompt => "Sit";
        void Ensure() { if (occupants == null || occupants.Length != sittingPoints.Length) occupants = new HallSeatActor[sittingPoints.Length]; }
        public override bool Interact(HallSeatActor actor)
        {
            if (!actor || actor.IsSeated || sittingPoints == null) return false;
            Ensure();
            for (int i = 0; i < occupants.Length; i++)
                if (!occupants[i]) { occupants[i] = actor; actor.Sit(this, i, sittingPoints[i]); onOccupancyChanged.Invoke(true); return true; }
            return false;
        }
        public void Release(int slot, HallSeatActor actor)
        {
            Ensure();
            if (slot >= 0 && slot < occupants.Length && occupants[slot] == actor) occupants[slot] = null;
            onOccupancyChanged.Invoke(System.Array.Exists(occupants, a => a));
        }
        void OnDisable()
        {
            if (occupants == null) return;
            foreach (var actor in occupants) if (actor) actor.ForceStand();
        }
    }
}
