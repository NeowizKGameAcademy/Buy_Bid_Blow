using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BuyBidBlow.UI.HUD
{
    public class DetectObject : MonoBehaviour
    {
        [SerializeField] private Image cursor;
        [SerializeField] private Sprite[] cursorImage = new Sprite[2];
        [SerializeField] private float detectDistance = 3.0f;
        [SerializeField] private LayerMask[] detectLayer;

        public bool isDetected { get; private set; }
        public LayerMask detectingLayer { get; private set; }
        public Collider DetectedCollider { get; private set; }
        private Transform playerRoot;

        private void Awake()
        {
            var interaction=GetComponentInParent<BuyBidBlow.Characters.PlayerInteraction>();
            if(interaction)playerRoot=interaction.transform;
        }

        private void Update()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit=default;

            Debug.DrawRay(ray.origin, ray.direction * detectDistance, Color.red);

            int mask = 0;
            foreach (LayerMask layer in detectLayer)
                mask |= layer.value;

            // Looking down must not hit our own capsule. The nearest other collider still
            // occludes interaction, so walls and other players cannot be interacted through.
            bool found=false;float nearest=float.PositiveInfinity;
            foreach(var candidate in Physics.RaycastAll(ray,detectDistance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                if(playerRoot&&candidate.collider.transform.IsChildOf(playerRoot))continue;
                if(candidate.distance>=nearest)continue;
                hit=candidate;nearest=candidate.distance;found=true;
            }
            isDetected = found
                && ((mask & (1 << hit.collider.gameObject.layer)) != 0
                    || hit.collider.GetComponentInParent<BuyBidBlow.Common.IInteractable>() is { CanInteract: true });
            DetectedCollider = isDetected ? hit.collider : null;
            detectingLayer = isDetected ? hit.collider.gameObject.layer : 0;

            ChangeCursor(isDetected);
        }

        // The crosshair is HUD feedback only; detection still works without it.
        private void ChangeCursor(bool Detected)
        {
            if (cursor) cursor.sprite = cursorImage[Detected ? 1 : 0];
        }
    }
}
