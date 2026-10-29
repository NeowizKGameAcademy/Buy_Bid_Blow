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

        // 전방 레이캐스트로 상호작용 대상을 감지하고 결과에 따라 커서 이미지 갱신
        private void Update()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * detectDistance, Color.red);

            int mask = 0;
            foreach (LayerMask layer in detectLayer)
                mask |= layer.value;

            // 벽 너머의 NPC는 감지하지 않습니다.
            isDetected = Physics.Raycast(ray, out hit, detectDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && ((mask & (1 << hit.collider.gameObject.layer)) != 0
                    || hit.collider.GetComponentInParent<BuyBidBlow.Common.IInteractable>() is { CanInteract: true });
            DetectedCollider = isDetected ? hit.collider : null;
            detectingLayer = isDetected ? hit.collider.gameObject.layer : 0;

            ChangeCursor(isDetected);
        }

        // 감지 여부에 따라 커서 스프라이트 교체
        private void ChangeCursor(bool Detected)
        {
            if(Detected)
                cursor.sprite = cursorImage[1];
            else
                cursor.sprite = cursorImage[0];
        }
    }
}
