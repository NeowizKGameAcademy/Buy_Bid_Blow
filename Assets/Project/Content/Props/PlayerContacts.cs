using System.Linq;
using UnityEngine;

namespace BuyBidBlow.Props
{
    // Body capsules of every active "Player"-tagged object, shared by props that react to players.
    // Players are spawned by the network after the level starts, so the list is rescanned periodically.
    public static class PlayerContacts
    {
        const float ScanInterval = .5f;
        static CapsuleCollider[] cache = new CapsuleCollider[0];
        static float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { cache = new CapsuleCollider[0]; nextScan = 0; }

        // Returns the same array instance until the set of players changes.
        public static CapsuleCollider[] Get()
        {
            if (Time.time < nextScan && !HasDestroyed()) return cache;
            nextScan = Time.time + ScanInterval;
            var found = GameObject.FindGameObjectsWithTag("Player")
                .Select(p => p.GetComponent<CapsuleCollider>()).Where(c => c).ToArray();
            if (!found.SequenceEqual(cache)) cache = found;
            return cache;
        }

        static bool HasDestroyed()
        {
            foreach (var c in cache) if (!c) return true;
            return false;
        }
    }
}
