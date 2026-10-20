using System;
using System.Linq;
using UnityEngine;

namespace BuyBidBlow.Props
{
    // Shared discovery avoids a scene search per rope and supports late network spawns.
    internal static class PlayerPropContacts
    {
        static CapsuleCollider[] players = Array.Empty<CapsuleCollider>();
        static float nextRefresh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            players = Array.Empty<CapsuleCollider>();
            nextRefresh = float.NegativeInfinity;
        }

        internal static CapsuleCollider[] Current
        {
            get
            {
                if (Time.unscaledTime >= nextRefresh)
                {
                    nextRefresh = Time.unscaledTime + .25f;
                    players = GameObject.FindGameObjectsWithTag("Player")
                        .Select(player => player.GetComponent<CapsuleCollider>())
                        .Where(body => body && body.enabled && body.gameObject.activeInHierarchy)
                        .ToArray();
                }
                return players;
            }
        }

        internal static CapsuleCollider[] Combine(CapsuleCollider[] assigned, CapsuleCollider[] automatic)
        {
            return (assigned ?? Array.Empty<CapsuleCollider>()).Concat(automatic)
                .Where(body => body && body.enabled && body.gameObject.activeInHierarchy)
                .Distinct().ToArray();
        }
    }
}
