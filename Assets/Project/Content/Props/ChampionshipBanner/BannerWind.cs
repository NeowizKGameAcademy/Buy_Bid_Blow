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
        CapsuleCollider[] players;
        void Awake() { cloth = GetComponent<Cloth>(); SetContacts(new CapsuleCollider[0]); }
        void Update()
        {
            if (!cloth) return;
            cloth.externalAcceleration = wind + Vector3.forward * (Mathf.Sin(Time.time * .7f) * gust);
            // The banner reacts to every player's body as well as its support colliders.
            var current = PlayerContacts.Get();
            if (current != players) { players = current; SetContacts(current); }
        }
        public void SetContacts(CapsuleCollider[] capsules)
        {
            if (!cloth) cloth = GetComponent<Cloth>();
            cloth.capsuleColliders = supportColliders.Concat(capsules ?? new CapsuleCollider[0]).Where(c=>c).Distinct().ToArray();
        }
    }
}
