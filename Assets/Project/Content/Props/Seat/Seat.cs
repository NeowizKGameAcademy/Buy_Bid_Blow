using System.Collections.Generic;
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
        // Mezzanine audience chair: sitting here takes part in the auction.
        public bool auction;
        public UnityEvent<bool> onOccupancyChanged = new UnityEvent<bool>();
        PlayerSeating[] occupants;

        // In a match the server assigns seats by this id (the object name, unique in the hall).
        static readonly Dictionary<string, Seat> byId = new Dictionary<string, Seat>();
        public string Id => name;
        public static Seat Find(string id) => id != null && byId.TryGetValue(id, out var seat) ? seat : null;
        void OnEnable() => byId[Id] = this;

        public bool CanInteract => FreeSlot() >= 0;
        public string Prompt => "Sit";
        void Ensure() { if (occupants == null || occupants.Length != sittingPoints.Length) occupants = new PlayerSeating[sittingPoints.Length]; }

        // First slot nobody sits in, here or (in a match) on another client; -1 when full.
        public int FreeSlot()
        {
            if (sittingPoints == null) return -1;
            Ensure();
            for (int i = 0; i < occupants.Length; i++)
                if (!occupants[i] && !PlayerNet.IsSeatTaken(Id, i)) return i;
            return -1;
        }

        public bool Interact(GameObject player)
        {
            if (!player) return false;
            if (!player.TryGetComponent(out PlayerSeating actor)) actor = player.AddComponent<PlayerSeating>();
            if (actor.IsSeated) return false;
            int slot = FreeSlot();
            if (slot < 0) return false;
            SitAt(actor, slot);
            return true;
        }

        // Seats the actor in a slot already granted (by the server in a match).
        public void SitAt(PlayerSeating actor, int slot)
        {
            Ensure();
            if (slot < 0 || slot >= occupants.Length || actor.IsSeated) return;
            occupants[slot] = actor; actor.Sit(this, slot, sittingPoints[slot]); onOccupancyChanged.Invoke(true);
        }

        public void Release(int slot, PlayerSeating actor)
        {
            Ensure();
            if (slot >= 0 && slot < occupants.Length && occupants[slot] == actor) occupants[slot] = null;
            onOccupancyChanged.Invoke(System.Array.Exists(occupants, a => a));
        }
        void OnDisable()
        {
            if (byId.TryGetValue(Id, out var registered) && registered == this) byId.Remove(Id);
            if (occupants == null) return;
            foreach (var actor in occupants) if (actor) actor.ForceStand();
        }
    }
}
