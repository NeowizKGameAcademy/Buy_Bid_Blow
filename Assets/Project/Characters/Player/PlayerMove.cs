using System;
using UnityEngine;

namespace BuyBidBlow.Characters
{
    public class PlayerMove : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Rigidbody playerRb;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private LayerMask groundLayer;

        [Header("Move")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 5f;

        [Header("Rotate")]
        [SerializeField] private GameObject Head;
        [SerializeField] private float rotateSpeed = 0.1f;
        [SerializeField] private float upperLimit = 60f;
        [SerializeField] private float lowerLimit = 40f;
        // Seated, the chair holds the body, so only the head and camera turn, this far either side.
        [SerializeField] private float seatedYawLimit = 90f;

        private Vector2 curMoveInput;

        private bool isGround;
        private bool isJumpHeld;
        private float currentY;
        private float seatedYaw;

        private Quaternion headLocalRotation;

        // 짐 운반 등으로 이동 속도를 잠시 바꿀 때 쓴다. 1 = 기본 속도.
        public float SpeedMultiplier { get; set; } = 1f;

        public bool IsGrounded => isGround;
        public event Action<bool> GroundedChanged;
        public event Action Jumped;
        public event Action<Vector2> MoveInputChanged;
        public Vector2 MoveInput => curMoveInput;
        public float LookPitch => currentY;
        public float LookYaw => seatedYaw;

        private void Awake()
        {
            if (playerRb == null)
                TryGetComponent(out playerRb);

            if (playerCamera == null)
                TryGetComponent(out playerCamera);

            playerRb.constraints |= RigidbodyConstraints.FreezeRotation;

            if (Head != null)
                headLocalRotation = Head.transform.localRotation;
        }

        private bool IsSeated => TryGetComponent(out PlayerSeating seating) && seating.IsSeated;

        private void FixedUpdate()
        {
            if (IsSeated) return;
            Move();
        }

        // Animator updates bone poses before LateUpdate. Apply the look rotation afterwards
        // so animation clips cannot overwrite the mouse-controlled head rotation.
        private void LateUpdate()
        {
            // Standing up faces the body forward again; the camera follows it.
            if (seatedYaw != 0 && !IsSeated)
            {
                seatedYaw = 0;
                ApplyLook();
            }
            if (Head != null)
                Head.transform.localRotation = headLocalRotation;
        }

        public void SetMoveInput(Vector2 input)
        {
            if (IsSeated) input = Vector2.zero;
            if (curMoveInput == input) return;
            curMoveInput = input;
            MoveInputChanged?.Invoke(input);
        }

        public void Rotate(Vector2 mouseDelta)
        {
            if (playerCamera == null)
                return;

            float mouseX = mouseDelta.x * rotateSpeed * BuyBidBlow.UI.Menu.GameSettings.LookMultiplier / 100;
            if (IsSeated)
                seatedYaw = Mathf.Clamp(seatedYaw + mouseX, -seatedYawLimit, seatedYawLimit);
            else
                transform.Rotate(Vector3.up * mouseX);

            float mouseY = mouseDelta.y * rotateSpeed * BuyBidBlow.UI.Menu.GameSettings.LookMultiplier / 100;

            currentY -= mouseY;
            currentY = Mathf.Clamp(currentY, -upperLimit, lowerLimit);

            ApplyLook();
        }

        private void ApplyLook()
        {
            var look = Quaternion.Euler(currentY, seatedYaw, 0f);
            playerCamera.CameraTransform.localRotation = look;
            headLocalRotation = look;
        }

        public void ApplyNetworkLook(float pitch, float yaw)
        {
            currentY = Mathf.Clamp(pitch, -upperLimit, lowerLimit);
            seatedYaw = Mathf.Clamp(yaw, -seatedYawLimit, seatedYawLimit);
            var look = Quaternion.Euler(currentY, seatedYaw, 0f);
            headLocalRotation = look;
            if (Head != null)
                Head.transform.localRotation = look;
        }

        public void SetJumpHeld(bool isHeld)
        {
            if (IsSeated) { isJumpHeld = false; return; }
            isJumpHeld = isHeld;

            if (isJumpHeld && isGround)
                Jump();
        }

        private void Move()
        {
            Vector3 direction =
                transform.forward * curMoveInput.y +
                transform.right * curMoveInput.x;

            direction *= moveSpeed * SpeedMultiplier;

            direction.y = playerRb.linearVelocity.y;

            playerRb.linearVelocity = direction;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsGroundLayer(collision.gameObject.layer))
                return;

            SetGrounded(true);

            if (isJumpHeld)
                Jump();
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!IsGroundLayer(collision.gameObject.layer))
                return;

            SetGrounded(false);
        }

        private bool IsGroundLayer(int targetLayer)
        {
            return (groundLayer.value & (1 << targetLayer)) != 0;
        }

        private void Jump()
        {
            SetGrounded(false);

            playerRb.linearVelocity = new Vector3(
                playerRb.linearVelocity.x,
                0f,
                playerRb.linearVelocity.z
            );

            playerRb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );

            Jumped?.Invoke();
        }

        private void SetGrounded(bool grounded)
        {
            if (isGround == grounded)
                return;

            isGround = grounded;
            GroundedChanged?.Invoke(isGround);
        }
    }
}
