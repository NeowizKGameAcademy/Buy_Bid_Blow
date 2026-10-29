using BuyBidBlow.Common;
using UnityEngine;

namespace BuyBidBlow.Characters
{
    // On the local player's camera: what the player is looking at and could use with the interact key.
    // The HUD shows its prompt and PlayerInteraction uses it; neither looks at layers or concrete types.
    public sealed class InteractionSensor : MonoBehaviour
    {
        [SerializeField] private float detectDistance = 3.0f;

        public IInteractable Target { get; private set; }
        public Collider TargetCollider { get; private set; }

        // 전방 레이캐스트로 상호작용 가능한 대상을 감지 (벽 너머의 대상은 감지하지 않음)
        private void Update()
        {
            var ray = new Ray(transform.position, transform.forward);
            Target = Physics.Raycast(ray, out var hit, detectDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<IInteractable>() is { CanInteract: true } target ? target : null;
            TargetCollider = Target != null ? hit.collider : null;
        }
    }
}
