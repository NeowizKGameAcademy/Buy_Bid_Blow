using UnityEngine;
using UnityEngine.Events;

namespace BuyBidBlow.Hall
{
    // Attach to the moving player's root; sitting anchors represent the hip position.
    public sealed class HallSeatActor : MonoBehaviour
    {
        public float hipHeight = .85f;
        public UnityEvent<bool> onSeatedChanged = new UnityEvent<bool>();
        public bool IsSeated => seat;
        HallSeat seat;
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
        public void Sit(HallSeat target, int index, Transform anchor)
        {
            seat = target; slot = index; point = anchor;
            previousPosition = transform.position; previousRotation = transform.rotation;
            body = GetComponent<Rigidbody>();
            if (body) { wasKinematic = body.isKinematic; if (!wasKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; } body.isKinematic = true; }
            colliders = GetComponentsInChildren<Collider>(); enabledBefore = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++) { enabledBefore[i] = colliders[i].enabled; colliders[i].enabled = false; }
            controller = GetComponent<CharacterController>();
            controllerEnabled = controller && System.Array.IndexOf(colliders, controller) >= 0 && enabledBefore[System.Array.IndexOf(colliders, controller)];
            Snap(); onSeatedChanged.Invoke(true);
        }
        void Snap() { if (point) transform.SetPositionAndRotation(point.position - Vector3.up * hipHeight, point.rotation); }
        void LateUpdate() { if (seat) Snap(); }
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
        public void ForceStand() { if (seat) StandAt(previousPosition, previousRotation); }
        void StandAt(Vector3 position, Quaternion rotation)
        {
            var oldSeat = seat; seat = null; point = null;
            transform.SetPositionAndRotation(position, rotation);
            if (colliders != null) for (int i = 0; i < colliders.Length; i++) if (colliders[i]) colliders[i].enabled = enabledBefore[i];
            if (controller) controller.enabled = controllerEnabled;
            if (body) body.isKinematic = wasKinematic;
            if (oldSeat) oldSeat.Release(slot, this);
            onSeatedChanged.Invoke(false);
        }
        void OnDisable() => ForceStand();
        public static bool TryInteract(Transform player, Transform view, float distance = 3f)
        {
            var actor = player.GetComponent<HallSeatActor>();
            if (actor && actor.IsSeated) return actor.TryStand();
            if (!view || !Physics.Raycast(view.position, view.forward, out var hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
            var target = hit.collider.GetComponentInParent<HallInteractable>();
            if (!target) return false;
            if (!actor) actor = player.gameObject.AddComponent<HallSeatActor>();
            return target.Interact(actor);
        }
    }
}
