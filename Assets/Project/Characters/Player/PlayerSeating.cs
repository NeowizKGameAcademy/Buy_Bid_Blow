using UnityEngine;
using UnityEngine.Events;
using BuyBidBlow.Props;

namespace BuyBidBlow.Characters
{
    // Attach to the moving player's root; sitting anchors represent the hip position.
    public sealed class PlayerSeating : MonoBehaviour
    {
        public float hipHeight = .85f;
        public UnityEvent<bool> onSeatedChanged = new UnityEvent<bool>();
        public bool IsSeated => seat;
        public Seat CurrentSeat => seat;
        Seat seat;
        int slot;
        Transform point;
        Vector3 previousPosition;
        Quaternion previousRotation;
        Rigidbody body;
        bool wasKinematic;
        Collider[] colliders;
        bool[] enabledBefore;
        CharacterController controller;
        bool controllerEnabled;
        CharacterMotor motor;
        // 이전 위치를 저장하고 물리·충돌을 끈 뒤 의자 앵커에 앉힘
        public void Sit(Seat target, int index, Transform anchor)
        {
            seat = target; slot = index; point = anchor;
            previousPosition = transform.position; previousRotation = transform.rotation;
            body = GetComponent<Rigidbody>();
            if (body) { wasKinematic = body.isKinematic; if (!wasKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; } body.isKinematic = true; }
            colliders = GetComponentsInChildren<Collider>(); enabledBefore = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++) { enabledBefore[i] = colliders[i].enabled; colliders[i].enabled = false; }
            controller = GetComponent<CharacterController>();
            controllerEnabled = controller && System.Array.IndexOf(colliders, controller) >= 0 && enabledBefore[System.Array.IndexOf(colliders, controller)];
            // The chair holds the body: the motor stops until the player stands up.
            if (TryGetComponent(out motor)) motor.Block(this);
            Snap(); onSeatedChanged.Invoke(true);
        }
        // 앉는 앵커에서 엉덩이 높이만큼 내린 위치로 배치
        void Snap() { if (point) transform.SetPositionAndRotation(point.position - Vector3.up * hipHeight, point.rotation); }
        // 앉아 있으면 매 프레임 앵커 위치에 고정
        void LateUpdate() { if (seat) Snap(); }
        // 바닥과 공간이 확보된 출구 지점에 일어서기 시도
        public bool TryStand()
        {
            if (!seat) return false;
            foreach (var exit in seat.exitPoints)
            {
                if (!exit) continue;
                if (!Physics.Raycast(exit.position + Vector3.up * .5f, Vector3.down, out var floor, 1.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || floor.normal.y < .65f) continue;
                var standing = floor.point + Vector3.up * .02f;
                // Check a standing capsule before restoring player collision.
                if (!Physics.CheckCapsule(standing + Vector3.up * .35f, standing + Vector3.up * 1.5f, .27f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                { StandAt(standing, exit.rotation); return true; }
            }
            return false;
        }
        // 앉기 전 위치로 강제로 일어서기
        public void ForceStand() { if (seat) StandAt(previousPosition, previousRotation); }
        // 지정 위치에 세우고 충돌·물리를 복원한 뒤 좌석 해제
        void StandAt(Vector3 position, Quaternion rotation)
        {
            var oldSeat = seat; seat = null; point = null;
            transform.SetPositionAndRotation(position, rotation);
            if (colliders != null) for (int i = 0; i < colliders.Length; i++) if (colliders[i]) colliders[i].enabled = enabledBefore[i];
            if (controller) controller.enabled = controllerEnabled;
            if (body) body.isKinematic = wasKinematic;
            if (motor) { motor.Unblock(this); motor.Teleport(position, rotation); }
            if (oldSeat) oldSeat.Release(slot, this);
            onSeatedChanged.Invoke(false);
        }
        // 비활성화 시 강제로 일어서기
        void OnDisable() => ForceStand();
    }
}
