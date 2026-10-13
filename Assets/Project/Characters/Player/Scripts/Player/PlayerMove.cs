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
        [SerializeField] private float rotateSpeed = 0.1f;
        [SerializeField] private float upperLimit = 60f;
        [SerializeField] private float lowerLimit = 40f;

        private Vector2 curMoveInput;

        private bool isGround;
        private bool isJumpHeld;

        private float currentY;

        private void Awake()
        {
            if (playerRb == null)
                TryGetComponent(out playerRb);

            if (playerCamera == null)
                TryGetComponent(out playerCamera);

            playerRb.constraints |= RigidbodyConstraints.FreezeRotation;
        }

        private void FixedUpdate()
        {
            Move();
        }

        public void SetMoveInput(Vector2 input)
        {
            curMoveInput = input;
        }

        public void Rotate(Vector2 mouseDelta)
        {
            if (playerCamera == null)
                return;

            float mouseX = mouseDelta.x * rotateSpeed;
            transform.Rotate(Vector3.up * mouseX);

            float mouseY = mouseDelta.y * rotateSpeed;

            currentY -= mouseY;
            currentY = Mathf.Clamp(currentY, -lowerLimit, upperLimit);

            playerCamera.CameraTransform.localRotation =
                Quaternion.Euler(currentY, 0f, 0f);
        }

        public void SetJumpHeld(bool isHeld)
        {
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

            isGround = true;

            if (isJumpHeld)
                Jump();
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!IsGroundLayer(collision.gameObject.layer))
                return;

            isGround = false;
        }

        private bool IsGroundLayer(int targetLayer)
        {
            return (groundLayer.value & (1 << targetLayer)) != 0;
        }

        private void Jump()
        {
            isGround = false;

            playerRb.linearVelocity = new Vector3(
                playerRb.linearVelocity.x,
                0f,
                playerRb.linearVelocity.z
            );

            playerRb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );
        }
    }
}
