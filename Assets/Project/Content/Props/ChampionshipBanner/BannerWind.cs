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
        // 천 컴포넌트를 가져오고 지지 콜라이더만으로 충돌 설정
        void Awake() { cloth = GetComponent<Cloth>(); SetContacts(new CapsuleCollider[0]); }
        // 플레이어 몸 캡슐 콜라이더를 천 충돌 대상에 추가
        // The banner reacts to the player's body as well as its support colliders.
        void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player && player.TryGetComponent(out CapsuleCollider body)) SetContacts(new[] { body });
        }
        // 사인파 돌풍을 더한 바람 가속도를 천에 적용
        void Update() { if (cloth) cloth.externalAcceleration = wind + Vector3.forward * (Mathf.Sin(Time.time * .7f) * gust); }
        // 지지 콜라이더와 주어진 캡슐을 합쳐 천 충돌 콜라이더로 설정
        public void SetContacts(CapsuleCollider[] capsules)
        {
            if (!cloth) cloth = GetComponent<Cloth>();
            cloth.capsuleColliders = supportColliders.Concat(capsules ?? new CapsuleCollider[0]).Where(c=>c).Distinct().ToArray();
        }
    }
}
