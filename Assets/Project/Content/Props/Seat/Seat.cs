using UnityEngine;
using UnityEngine.Events;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;

namespace BuyBidBlow.Props
{
    public sealed class Seat : MonoBehaviour, IInteractable
    {
        public Transform[] sittingPoints;
        public Transform[] exitPoints;
        public UnityEvent<bool> onOccupancyChanged = new UnityEvent<bool>();
        PlayerSeating[] occupants;
        public bool CanInteract => true;
        public string Prompt => "Sit";
        void Ensure() { if (occupants == null || occupants.Length != sittingPoints.Length) occupants = new PlayerSeating[sittingPoints.Length]; }
        public bool Interact(GameObject player)
        {
            if (!player) return false;
            if (!player.TryGetComponent(out PlayerSeating actor)) actor = player.AddComponent<PlayerSeating>();
            if (actor.IsSeated || sittingPoints == null || sittingPoints.Length == 0) return false;
            Ensure();
            for (int i = 0; i < occupants.Length; i++)
                if (!occupants[i]) { occupants[i] = actor; actor.Sit(this, i, sittingPoints[i]); onOccupancyChanged.Invoke(true); return true; }
            return false;
        }
        public void Release(int slot, PlayerSeating actor)
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
