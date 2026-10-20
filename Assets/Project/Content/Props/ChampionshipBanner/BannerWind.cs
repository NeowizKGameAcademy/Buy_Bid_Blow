using UnityEngine;
using System.Linq;

namespace BuyBidBlow.Props
{
    [RequireComponent(typeof(Cloth))]
    public sealed class BannerWind : MonoBehaviour
    {
        public Vector3 wind = new Vector3(0, 0, .25f);
        public float gust = .15f;
        public CapsuleCollider[] supportColliders = new CapsuleCollider[0];
        Cloth cloth;
        CapsuleCollider[] assignedContacts = new CapsuleCollider[0];
        CapsuleCollider[] lastPlayers;
        void Awake() { cloth = GetComponent<Cloth>(); SetContacts(new CapsuleCollider[0]); }
        void Update()
        {
            var players = PlayerPropContacts.Current;
            if (!ReferenceEquals(players, lastPlayers))
            {
                lastPlayers = players;
                ApplyContacts(players);
            }
            if (cloth) cloth.externalAcceleration = wind + Vector3.forward * (Mathf.Sin(Time.time * .7f) * gust);
        }
        public void SetContacts(CapsuleCollider[] capsules)
        {
            assignedContacts = capsules ?? new CapsuleCollider[0];
            ApplyContacts(Application.isPlaying ? PlayerPropContacts.Current : new CapsuleCollider[0]);
        }
        void ApplyContacts(CapsuleCollider[] players)
        {
            if (!cloth) cloth = GetComponent<Cloth>();
            cloth.capsuleColliders = PlayerPropContacts.Combine(
                (supportColliders ?? new CapsuleCollider[0]).Concat(assignedContacts).ToArray(), players);
        }
    }
}
