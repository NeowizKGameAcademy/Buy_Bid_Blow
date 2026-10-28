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

        public bool IsGrounded => isGround;
        public event Action<bool> GroundedChanged;
        public event Action Jumped;

        // 리지드바디·카메라 참조를 찾고 회전을 고정하며 머리 초기 회전 저장
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

        // 앉아 있지 않으면 이동 처리
        private void FixedUpdate()
        {
            if (IsSeated) return;
            Move();
        }

        // 일어서면 좌우 시선을 되돌리고 애니메이션 이후 머리 회전을 덮어씀
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

        // 이동 입력 값을 저장
        public void SetMoveInput(Vector2 input)
        {
            curMoveInput = input;
        }

        // 마우스 이동량으로 몸(앉아 있으면 시선)과 상하 시선을 회전
        public void Rotate(Vector2 mouseDelta)
        {
            if (playerCamera == null)
                return;

            float mouseX = mouseDelta.x * rotateSpeed / 100;
            if (IsSeated)
                seatedYaw = Mathf.Clamp(seatedYaw + mouseX, -seatedYawLimit, seatedYawLimit);
            else
                transform.Rotate(Vector3.up * mouseX);

            float mouseY = mouseDelta.y * rotateSpeed / 100;

            currentY -= mouseY;
            currentY = Mathf.Clamp(currentY, -upperLimit, lowerLimit);

            ApplyLook();
        }

        // 현재 시선 각도를 카메라와 머리 회전에 적용
        private void ApplyLook()
        {
            var look = Quaternion.Euler(currentY, seatedYaw, 0f);
            playerCamera.CameraTransform.localRotation = look;
            headLocalRotation = look;
        }

        // 점프 키 유지 상태를 설정하고 땅에 있으면 점프 (앉아 있으면 무시)
        public void SetJumpHeld(bool isHeld)
        {
            if (IsSeated) { isJumpHeld = false; return; }
            isJumpHeld = isHeld;

            if (isJumpHeld && isGround)
                Jump();
        }

        // 입력 방향으로 리지드바디 수평 속도를 설정
        private void Move()
        {
            Vector3 direction =
                transform.forward * curMoveInput.y +
                transform.right * curMoveInput.x;

            direction *= moveSpeed;

            direction.y = playerRb.linearVelocity.y;

            playerRb.linearVelocity = direction;
        }

        // 땅에 닿으면 착지 처리하고 점프 키 유지 중이면 다시 점프
        private void OnCollisionEnter(Collision collision)
        {
            if (!IsGroundLayer(collision.gameObject.layer))
                return;

            SetGrounded(true);

            if (isJumpHeld)
                Jump();
        }

        // 땅에서 떨어지면 공중 상태로 전환
        private void OnCollisionExit(Collision collision)
        {
            if (!IsGroundLayer(collision.gameObject.layer))
                return;

            SetGrounded(false);
        }

        // 레이어가 땅 레이어 마스크에 포함되는지 확인
        private bool IsGroundLayer(int targetLayer)
        {
            return (groundLayer.value & (1 << targetLayer)) != 0;
        }

        // 수직 속도를 없애고 위로 힘을 가해 점프한 뒤 이벤트 발생
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

        // 착지 상태가 바뀌면 갱신하고 변경 이벤트 발생
        private void SetGrounded(bool grounded)
        {
            if (isGround == grounded)
                return;

            isGround = grounded;
            GroundedChanged?.Invoke(isGround);
        }
    }
}
