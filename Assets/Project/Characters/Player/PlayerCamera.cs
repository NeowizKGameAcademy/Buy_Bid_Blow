using UnityEngine;

namespace BuyBidBlow.Characters
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private GameObject cameraObject;

        [Header("Default Position")]
        [SerializeField]
        private Vector3 defaultLocalPosition = new Vector3(0f, 1.7f, 0f);

        [Header("Emotion View")]
        [SerializeField] private Vector3 emotionLocalPosition = new Vector3(0f, 2.1f, -3.5f);
        [SerializeField] private Vector3 emotionLookTarget = new Vector3(0f, 1f, 0f);
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.2f;
        private Vector3 savedPosition;
        private Quaternion savedRotation;
        private int savedCullingMask;
        private Camera viewCamera;
        public bool IsEmotionView { get; private set; }

        public Transform CameraTransform => cameraObject.transform;

        public void SetEmotionView(bool active)
        {
            if (active == IsEmotionView || !cameraObject)
                return;
            if (!viewCamera) viewCamera = cameraObject.GetComponent<Camera>();
            if (!viewCamera) return;
            if (active)
            {
                savedPosition = CameraTransform.localPosition;
                savedRotation = CameraTransform.localRotation;
                savedCullingMask = viewCamera.cullingMask;
                // Show the body parts first person hides (UI stays on its own layer).
                foreach (var body in GetComponentsInChildren<Renderer>(true))
                    if (!body.GetComponentInParent<Canvas>(true)) viewCamera.cullingMask |= 1 << body.gameObject.layer;
                IsEmotionView = true;
                LateUpdate();
            }
            else
            {
                IsEmotionView = false;
                CameraTransform.localPosition = savedPosition;
                CameraTransform.localRotation = savedRotation;
                viewCamera.cullingMask = savedCullingMask;
            }
        }

        private void LateUpdate()
        {
            if (!IsEmotionView || !cameraObject) return;
            var target = transform.TransformPoint(emotionLookTarget);
            var desired = transform.TransformPoint(emotionLocalPosition);
            var direction = desired - target;
            var distance = direction.magnitude;
            // Ignore the player's own colliders and triggers; keep walls in front of the camera.
            foreach (var hit in Physics.SphereCastAll(target, collisionRadius, direction.normalized,
                         distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform)) continue;
                distance = Mathf.Min(distance, Mathf.Max(0.05f, hit.distance - 0.05f));
            }
            CameraTransform.position = target + direction.normalized * distance;
            CameraTransform.rotation = Quaternion.LookRotation(target - CameraTransform.position, transform.up);
        }

        private void OnDisable() => SetEmotionView(false);

        private void Awake()
        {
            cameraObject.SetActive(true);
            cameraObject.transform.localPosition = defaultLocalPosition;
            cameraObject.transform.localRotation = Quaternion.identity;
        }
    }
}
