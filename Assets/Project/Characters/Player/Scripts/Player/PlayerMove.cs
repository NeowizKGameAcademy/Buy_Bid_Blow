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

        [Header("Optional stable sandbox controls")]
        [SerializeField] private bool stableControls;
        [SerializeField] private float groundAcceleration=35f;
        [SerializeField] private float airAcceleration=10f;
        private readonly RaycastHit[] groundHits=new RaycastHit[12];
        private float queuedJumpUntil=-1,lastGroundTime=-10,pendingYaw;
        private CapsuleCollider capsule;
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

            capsule=GetComponent<CapsuleCollider>();
            playerRb.constraints |= RigidbodyConstraints.FreezeRotation;
        }

        private void FixedUpdate()
        {
            if(stableControls){StableMove();return;}
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

            float mouseX = mouseDelta.x * rotateSpeed / 100;
            if(stableControls)pendingYaw+=mouseX;else transform.Rotate(Vector3.up * mouseX);

            float mouseY = mouseDelta.y * rotateSpeed / 100;

            currentY -= mouseY;
            currentY = Mathf.Clamp(currentY, -upperLimit, lowerLimit);

            playerCamera.CameraTransform.localRotation = Quaternion.Euler(currentY, 0f, 0f);
            Head.transform.localRotation = Quaternion.Euler(currentY, 0f, 0f);
        }

        public void SetJumpHeld(bool isHeld)
        {
            if(stableControls){if(isHeld&&!isJumpHeld)queuedJumpUntil=Time.time+.12f;isJumpHeld=isHeld;return;}
            isJumpHeld = isHeld;

            if (isJumpHeld && isGround)
                Jump();
        }

        private void StableMove()
        {
            if(playerRb.isKinematic)return;
            if(pendingYaw!=0){playerRb.MoveRotation(playerRb.rotation*Quaternion.Euler(0,pendingYaw,0));pendingYaw=0;}
            bool grounded=false;
            if(capsule&&playerRb.linearVelocity.y<=.15f){var bounds=capsule.bounds;float radius=Mathf.Min(bounds.extents.x,bounds.extents.z)*.9f;var origin=new Vector3(bounds.center.x,bounds.min.y+radius+.06f,bounds.center.z);int count=Physics.SphereCastNonAlloc(origin,radius,Vector3.down,groundHits,.16f,groundLayer,QueryTriggerInteraction.Ignore);for(int i=0;i<count;i++)if(groundHits[i].collider.attachedRigidbody!=playerRb&&groundHits[i].normal.y>=.65f){grounded=true;break;}}
            if(grounded)lastGroundTime=Time.time;
            Vector2 input=Vector2.ClampMagnitude(curMoveInput,1);Vector3 desired=(transform.forward*input.y+transform.right*input.x)*moveSpeed;var velocity=playerRb.linearVelocity;var planar=Vector3.MoveTowards(new Vector3(velocity.x,0,velocity.z),desired,(grounded?groundAcceleration:airAcceleration)*Time.fixedDeltaTime);velocity.x=planar.x;velocity.z=planar.z;
            if(queuedJumpUntil>=Time.time&&Time.time-lastGroundTime<=.10f){velocity.y=jumpForce;queuedJumpUntil=-1;lastGroundTime=-10;}
            playerRb.linearVelocity=velocity;
        }
        private void OnDisable(){curMoveInput=Vector2.zero;isJumpHeld=false;queuedJumpUntil=-1;pendingYaw=0;lastGroundTime=-10;}
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
            if(stableControls)return;
            if (!IsGroundLayer(collision.gameObject.layer))
                return;

            isGround = true;

            if (isJumpHeld)
                Jump();
        }

        private void OnCollisionExit(Collision collision)
        {
            if(stableControls)return;
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
