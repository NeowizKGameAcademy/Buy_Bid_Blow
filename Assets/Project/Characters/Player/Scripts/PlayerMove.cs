using UnityEngine;
using UnityEngine.InputSystem;

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
        private Vector2 mouseDelta;

        private bool isGround;
        private bool isJumpHeld;

        private float currentY;

        private void Awake()
        {
            if (playerRb == null)
                TryGetComponent(out playerRb);

            if (playerCamera == null)
                TryGetComponent(out playerCamera);
        }

        private void Update()
        {
            Rotate();
        }

        private void FixedUpdate()
        {
            Move();
            LockRotation();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            curMoveInput = context.ReadValue<Vector2>();
        }

        public void OnRotate(InputAction.CallbackContext context)
        {
            mouseDelta = context.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                isJumpHeld = true;

                if (isGround)
                    Jump();
            }
            else if (context.canceled)
            {
                isJumpHeld = false;
            }
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

        private void Rotate()
        {
            if (playerCamera == null)
                return;

            float mouseX = mouseDelta.x * rotateSpeed / 100;
            transform.Rotate(Vector3.up * mouseX);

            float mouseY = mouseDelta.y * rotateSpeed / 100;

            currentY -= mouseY;
            currentY = Mathf.Clamp(currentY, -upperLimit, lowerLimit);

            playerCamera.CameraTransform.localRotation = Quaternion.Euler(currentY, 0f, 0f);
        }

        private void LockRotation()
        {
            Vector3 rotation = playerRb.rotation.eulerAngles;

            playerRb.MoveRotation(Quaternion.Euler(0f, rotation.y, 0f));
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

            playerRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}
