using BuyBidBlow.Common;
using UnityEngine;

namespace BuyBidBlow.Characters
{
    // On the local player's camera: what the player is looking at and could use with the interact key.
    // The HUD shows its prompt and PlayerInteraction uses it; neither looks at layers or concrete types.
    public sealed class InteractionSensor : MonoBehaviour
    {
        [SerializeField] private float detectDistance = 3.0f;
        private readonly RaycastHit[] hits = new RaycastHit[16];

        public IInteractable Target { get; private set; }
        public Collider TargetCollider { get; private set; }

        // 전방 레이캐스트로 상호작용 가능한 대상을 감지 (벽 너머의 대상은 감지하지 않음, 다른 플레이어는 가리지 않음)
        // Other players' bodies block movement but not sight: someone standing in front of a counter
        // must not stop the players behind them from using it.
        private void Update()
        {
            var ray = new Ray(transform.position, transform.forward);
            int count = Physics.RaycastNonAlloc(ray, hits, detectDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Collider nearest = null; float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var c = hits[i].collider;
                if (hits[i].distance >= best || c.GetComponentInParent<PlayerNet>()) continue;
                nearest = c; best = hits[i].distance;
            }
            Target = nearest && nearest.GetComponentInParent<IInteractable>() is { CanInteract: true } target ? target : null;
            TargetCollider = Target != null ? nearest : null;
        }
    }
}
