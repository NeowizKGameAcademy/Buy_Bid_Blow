using System;
using UnityEngine;

namespace BuyBidBlow
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

        private Vector2 curMoveInput;

        private bool isGround;
        private bool isJumpHeld;
        private float currentY;

        private Quaternion headLocalRotation;

        public bool IsGrounded => isGround;
        public event Action<bool> GroundedChanged;
        public event Action Jumped;

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

        private void FixedUpdate()
        {
            var seatActor = GetComponent<BuyBidBlow.Hall.HallSeatActor>();
            if (seatActor != null && seatActor.IsSeated) return;
            Move();
        }

        // Animator updates bone poses before LateUpdate. Apply the look rotation afterwards
        // so animation clips cannot overwrite the mouse-controlled head rotation.
        private void LateUpdate()
        {
            if (Head != null)
                Head.transform.localRotation = headLocalRotation;
        }

        public void SetMoveInput(Vector2 input)
        {
            curMoveInput = input;
        }

        public void Rotate(Vector2 mouseDelta)
        {
            var seatActor = GetComponent<BuyBidBlow.Hall.HallSeatActor>();
            if (seatActor != null && seatActor.IsSeated) return;
            if (playerCamera == null)
                return;

            float mouseX = mouseDelta.x * rotateSpeed / 100;
            transform.Rotate(Vector3.up * mouseX);

            float mouseY = mouseDelta.y * rotateSpeed / 100;

            currentY -= mouseY;
            currentY = Mathf.Clamp(currentY, -upperLimit, lowerLimit);

            playerCamera.CameraTransform.localRotation = Quaternion.Euler(currentY, 0f, 0f);
            headLocalRotation = Quaternion.Euler(currentY, 0f, 0f);
        }

        public void SetJumpHeld(bool isHeld)
        {
            var seatActor = GetComponent<BuyBidBlow.Hall.HallSeatActor>();
            if (seatActor != null && seatActor.IsSeated) { isJumpHeld = false; return; }
            isJumpHeld = isHeld;

            if (isJumpHeld && isGround)
                Jump();
        }

        private void Move()
        {
            Vector3 direction =
                transform.forward * curMoveInput.y +
                transform.right * curMoveInput.x;

            direction *= moveSpeed;

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
