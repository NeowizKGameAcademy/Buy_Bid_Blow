using UnityEngine;

namespace BuyBidBlow.Characters
{
    // Mouse look: yaw turns the body, pitch tilts the camera. Seated, the chair holds the body,
    // so only the camera turns, up to seatedYawLimit either side; standing up faces forward again.
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private PlayerSeating seating;
        // Degrees per mouse delta unit.
        [SerializeField] private float sensitivity = .1f;
        [SerializeField] private float upperLimit = 60f;
        [SerializeField] private float lowerLimit = 40f;
        [SerializeField] private float seatedYawLimit = 90f;

        private float pitch, seatedYaw;

        private bool IsSeated => seating && seating.IsSeated;

        private void Awake()
        {
            if (!playerCamera) TryGetComponent(out playerCamera);
            if (!seating) TryGetComponent(out seating);
        }

        // 마우스 이동량으로 몸(앉아 있으면 시선)과 상하 시선을 회전
        public void Look(Vector2 delta)
        {
            // The options menu scales mouse sensitivity (GameSettings).
            delta *= BuyBidBlow.UI.Menu.GameSettings.LookMultiplier;
            float yaw = delta.x * sensitivity;
            if (IsSeated) seatedYaw = Mathf.Clamp(seatedYaw + yaw, -seatedYawLimit, seatedYawLimit);
            else transform.Rotate(0, yaw, 0);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -upperLimit, lowerLimit);
            Apply();
        }

        // 일어서면 좌우 시선을 정면으로 되돌림
        private void LateUpdate()
        {
            if (seatedYaw != 0 && !IsSeated) { seatedYaw = 0; Apply(); }
        }

        private void Apply()
        {
            if (playerCamera) playerCamera.CameraTransform.localRotation = Quaternion.Euler(pitch, seatedYaw, 0);
        }
    }
}
