using UnityEngine;
using UnityEngine.Events;
using BuyBidBlow.Common;

namespace BuyBidBlow.Props
{
    public sealed class Door : MonoBehaviour, IInteractable
    {
        public float openAngle = 95f;
        [Min(1)] public float degreesPerSecond = 90f;
        public UnityEvent<bool> onOpenChanged = new UnityEvent<bool>();
        // Door use is undecided; off until the design turns it on (per prefab or here).
        public bool interactable;
        public bool IsOpen { get; private set; }
        Quaternion closedRotation;
        bool initialized;
        public bool CanInteract => interactable;
        public string Prompt => IsOpen ? "Close" : "Open";
        void Awake() => Initialize();
        void Initialize() { if (initialized) return; closedRotation = transform.localRotation; initialized = true; }
        public void SetOpen(bool value) { Initialize(); IsOpen = value; onOpenChanged.Invoke(value); }
        public bool Interact(GameObject player) { if (!interactable) return false; SetOpen(!IsOpen); return true; }
        public void Simulate(float dt)
        {
            Initialize();
            var target = closedRotation * Quaternion.Euler(0, IsOpen ? openAngle : 0, 0);
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, degreesPerSecond * dt);
        }
        void Update() => Simulate(Time.deltaTime);
    }
}
