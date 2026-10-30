using System;
using System.Collections.Generic;
using UnityEngine;

namespace BuyBidBlow.Characters
{
    // What the character wants to do this frame. Produced by the local input, later also by bots or abilities.
    public struct CharacterIntent
    {
        // World-space horizontal direction, magnitude 0..1.
        public Vector3 Move;
        public bool Jump;
    }

    // Moves a kinematic capsule with collide-and-slide: casts the capsule along the move, slides along what it
    // hits, steps up small ledges, snaps to the ground going down and never climbs steep walls.
    // Gameplay that needs to move or stop the body goes through this API instead of the Rigidbody or colliders:
    // Block/Unblock by source (seated, UI, later stun), AddImpulse (knockback), ForceMove (dash), Teleport.
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        [SerializeField] private CapsuleCollider capsule;
        [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;

        [Header("Move")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float groundAcceleration = 50f;
        [SerializeField] private float airAcceleration = 12f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 1.25f;
        [SerializeField] private float gravity = 20f;
        [SerializeField] private float maxFallSpeed = 30f;

        [Header("Ground")]
        [SerializeField, Range(0, 89)] private float maxSlope = 45f;
        [SerializeField] private float stepHeight = .35f;
        [SerializeField] private float groundSnap = .3f;
        [SerializeField] private float skin = .02f;

        [Header("External")]
        // How fast knockback velocity fades, in m/s per second.
        [SerializeField] private float impulseDamping = 12f;

        const int MaxSlides = 4;
        private readonly HashSet<object> blockers = new HashSet<object>();
        private readonly Dictionary<object, float> speedModifiers = new Dictionary<object, float>();
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private readonly Collider[] overlaps = new Collider[16];
        private Collider[] ownColliders;
        private CharacterIntent intent;
        private Vector3 velocity, impulse, forcedVelocity;
        private float forcedUntil;

        public float MoveSpeed => moveSpeed;
        public Vector3 Velocity => velocity + impulse;
        public bool IsGrounded { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public bool IsBlocked => blockers.Count > 0;
        public event Action Jumped, Landed;
        public event Action<bool> GroundedChanged;

        private float MinGroundY => Mathf.Cos(maxSlope * Mathf.Deg2Rad);

        private void Awake()
        {
            if (!capsule) capsule = GetComponent<CapsuleCollider>();
            ownColliders = GetComponentsInChildren<Collider>(true);
            if (TryGetComponent(out Rigidbody body)) { body.isKinematic = true; body.interpolation = RigidbodyInterpolation.None; }
        }

        private void OnEnable() { velocity = impulse = Vector3.zero; SetGrounded(ProbeGround(transform.position, skin * 2, out _)); }

        // 이번 프레임의 이동 의도 설정
        public void SetIntent(CharacterIntent value) => intent = value;

        // 출처별로 이동을 막거나 풀기 (착석·UI·기절 등). 막는 동안 속도는 0
        public void Block(object source) { if (blockers.Add(source)) velocity = impulse = Vector3.zero; }
        public void Unblock(object source) => blockers.Remove(source);

        // 출처별 이동 속도 배수 설정/해제 (감속 등)
        public void SetSpeedModifier(object source, float multiplier) => speedModifiers[source] = multiplier;
        public void ClearSpeedModifier(object source) => speedModifiers.Remove(source);

        // 넉백처럼 감쇠하는 외부 속도 추가. 위쪽 성분은 땅에서 떼어 냄
        public void AddImpulse(Vector3 value)
        {
            impulse += new Vector3(value.x, 0, value.z);
            if (value.y > 0) { velocity.y = Mathf.Max(velocity.y, value.y); SetGrounded(false); }
        }

        // 대시처럼 일정 시간 동안 의도를 무시하고 지정 수평 속도로 이동
        public void ForceMove(Vector3 horizontalVelocity, float seconds)
        {
            forcedVelocity = new Vector3(horizontalVelocity.x, 0, horizontalVelocity.z);
            forcedUntil = Time.time + seconds;
        }

        // 지정 위치로 순간이동하고 속도를 비운 뒤 겹침을 해소
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            velocity = impulse = Vector3.zero; forcedUntil = 0;
            Physics.SyncTransforms();
            transform.position = Depenetrate(transform.position);
            SetGrounded(ProbeGround(transform.position, groundSnap, out _));
        }

        private void Update()
        {
            if (IsBlocked) return;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (dt > 0) Simulate(dt);
        }

        // 의도와 외부 속도로 한 스텝 이동
        private void Simulate(float dt)
        {
            var position = Depenetrate(transform.position);

            float multiplier = 1;
            foreach (var m in speedModifiers.Values) multiplier *= m;
            var desired = Time.time < forcedUntil ? forcedVelocity : Vector3.ClampMagnitude(intent.Move, 1) * moveSpeed * multiplier;
            var horizontal = Vector3.MoveTowards(new Vector3(velocity.x, 0, velocity.z), desired,
                (IsGrounded ? groundAcceleration : airAcceleration) * dt);
            velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);

            bool jumped = false;
            if (IsGrounded && intent.Jump)
            {
                velocity.y = Mathf.Sqrt(2 * gravity * jumpHeight);
                SetGrounded(false); jumped = true;
            }
            intent.Jump = false;
            if (IsGrounded) velocity.y = 0;
            else velocity.y = Mathf.Max(velocity.y - gravity * dt, -maxFallSpeed);

            var move = (new Vector3(velocity.x, 0, velocity.z) + impulse) * dt;
            if (IsGrounded) move = Vector3.ProjectOnPlane(move, GroundNormal);
            position = MoveAlong(position, move, IsGrounded);

            if (IsGrounded)
            {
                // Follow the ground down slopes and stairs; walking off an edge starts a fall.
                if (ProbeGround(position, groundSnap, out var ground)) position += Vector3.down * Mathf.Max(0, ground.distance - skin);
                else SetGrounded(false);
            }
            else
            {
                position = MoveVertical(position, velocity.y * dt);
            }

            transform.position = position;
            impulse = Vector3.MoveTowards(impulse, Vector3.zero, impulseDamping * dt);
            if (jumped) Jumped?.Invoke();
        }

        // 수평 이동: 막히면 계단 오르기를 시도해 더 멀리 간 쪽을 택함
        private Vector3 MoveAlong(Vector3 position, Vector3 move, bool grounded)
        {
            var direct = Slide(position, move, grounded, out _);
            var wanted = Flat(move).magnitude;
            if (!grounded || stepHeight <= 0 || wanted < 1e-5f || Flat(direct - position).magnitude >= wanted * .95f) return direct;

            float up = CastDistance(position, Vector3.up, stepHeight);
            var raised = Slide(position + Vector3.up * up, Flat(move), true, out _);
            if (!CastCapsule(raised, Vector3.down, up + groundSnap, out var floor) || SurfaceNormal(floor, move).y < MinGroundY) return direct;
            var stepped = raised + Vector3.down * Mathf.Max(0, floor.distance - skin);
            // Per-frame moves are tiny at high frame rates, so any real gain over the blocked move counts.
            return Flat(stepped - position).magnitude > Flat(direct - position).magnitude + 1e-4f ? stepped : direct;
        }

        // 공중 수직 이동: 걸을 수 있는 바닥에 닿으면 착지, 천장에 닿으면 상승 중단
        private Vector3 MoveVertical(Vector3 position, float dy)
        {
            if (Mathf.Abs(dy) < 1e-6f) return position;
            var dir = dy > 0 ? Vector3.up : Vector3.down;
            if (!CastCapsule(position, dir, Mathf.Abs(dy) + skin, out var hit)) return position + Vector3.up * dy;
            position += dir * Mathf.Max(0, hit.distance - skin);
            if (dy < 0 && hit.normal.y >= MinGroundY) { velocity.y = 0; GroundNormal = hit.normal; SetGrounded(true); }
            else if (dy > 0) velocity.y = 0;
            else position = Slide(position, Vector3.ProjectOnPlane(dir * (Mathf.Abs(dy) - hit.distance), hit.normal), false, out _);
            return position;
        }

        // 캡슐을 이동 방향으로 캐스트하고 부딪힌 면을 따라 남은 이동을 미끄러뜨림
        private Vector3 Slide(Vector3 position, Vector3 move, bool grounded, out bool blocked)
        {
            blocked = false;
            for (int i = 0; i < MaxSlides && move.sqrMagnitude > 1e-10f; i++)
            {
                float distance = move.magnitude; var dir = move / distance;
                if (!CastCapsule(position, dir, distance + skin, out var hit)) { position += move; break; }
                float travel = Mathf.Max(0, hit.distance - skin);
                position += dir * travel;
                var normal = hit.normal;
                // On the ground a steep surface is a wall: slide around it, never up it.
                if (grounded && normal.y < MinGroundY) { normal = Flat(normal).normalized; if (normal == Vector3.zero) break; }
                move = Vector3.ProjectOnPlane(dir * (distance - travel), normal);
                if (grounded && normal.y < MinGroundY) move.y = Mathf.Min(move.y, 0);
                blocked = true;
            }
            return position;
        }

        // 발밑으로 구를 쏴서 걸을 수 있는 바닥을 찾음
        private bool ProbeGround(Vector3 position, float distance, out RaycastHit ground)
        {
            bool found = CastCapsule(position, Vector3.down, distance + skin, out ground) && ground.normal.y >= MinGroundY;
            if (found) GroundNormal = ground.normal;
            return found;
        }

        // 캡슐이 모서리에 걸치면 접촉 법선이 기울어 나오므로, 접촉점 바로 위에서 레이를 내려 실제 윗면 법선을 구함
        private Vector3 SurfaceNormal(RaycastHit hit, Vector3 move)
        {
            var origin = hit.point + Vector3.up * .05f + Flat(move).normalized * .01f;
            return Physics.Raycast(origin, Vector3.down, out var surface, .1f, collisionMask, QueryTriggerInteraction.Ignore) && !IsOwn(surface.collider)
                ? surface.normal : hit.normal;
        }

        private float CastDistance(Vector3 position, Vector3 dir, float distance) =>
            CastCapsule(position, dir, distance + skin, out var hit) ? Mathf.Max(0, hit.distance - skin) : distance;

        // 자기 콜라이더를 제외하고 가장 가까운 충돌을 반환 (시작부터 겹친 것은 Depenetrate가 처리)
        private bool CastCapsule(Vector3 position, Vector3 dir, float distance, out RaycastHit nearest)
        {
            Ends(position, out var bottom, out var top, out float radius);
            int count = Physics.CapsuleCastNonAlloc(bottom, top, radius, dir, hits, distance, collisionMask, QueryTriggerInteraction.Ignore);
            nearest = default; bool found = false;
            for (int i = 0; i < count; i++)
            {
                var h = hits[i];
                if (IsOwn(h.collider) || h.distance <= 0) continue;
                if (!found || h.distance < nearest.distance) { nearest = h; found = true; }
            }
            return found;
        }

        // 다른 콜라이더와 겹쳐 있으면 밀어냄
        private Vector3 Depenetrate(Vector3 position)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                Ends(position, out var bottom, out var top, out float radius);
                int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps, collisionMask, QueryTriggerInteraction.Ignore);
                bool moved = false;
                for (int i = 0; i < count; i++)
                {
                    var other = overlaps[i];
                    if (IsOwn(other)) continue;
                    if (Physics.ComputePenetration(capsule, position, transform.rotation, other, other.transform.position, other.transform.rotation, out var dir, out float depth))
                    {
                        position += dir * (depth + .001f); moved = true;
                    }
                }
                if (!moved) break;
            }
            return position;
        }

        // 캡슐 양 끝 구의 중심과 반지름 (스킨만큼 줄인 반지름)
        private void Ends(Vector3 position, out Vector3 bottom, out Vector3 top, out float radius)
        {
            var scale = transform.lossyScale;
            radius = Mathf.Max(.01f, capsule.radius * Mathf.Max(scale.x, scale.z) - skin);
            float half = Mathf.Max(capsule.height * scale.y / 2 - capsule.radius * Mathf.Max(scale.x, scale.z), 0);
            var center = position + transform.rotation * Vector3.Scale(capsule.center, scale);
            bottom = center + Vector3.down * half; top = center + Vector3.up * half;
        }

        private bool IsOwn(Collider c) => Array.IndexOf(ownColliders, c) >= 0;
        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        // 착지 상태가 바뀌면 이벤트 발생
        private void SetGrounded(bool grounded)
        {
            if (IsGrounded == grounded) return;
            IsGrounded = grounded;
            if (!grounded) GroundNormal = Vector3.up;
            GroundedChanged?.Invoke(grounded);
            if (grounded) Landed?.Invoke();
        }
    }
}
