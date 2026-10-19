using UnityEngine;
using UnityEngine.Events;

namespace BuyBidBlow.Hall
{
    public sealed class HallDoor : HallInteractable
    {
        public float openAngle = 95f;
        [Min(1)] public float degreesPerSecond = 90f;
        public UnityEvent<bool> onOpenChanged = new UnityEvent<bool>();
        public bool IsOpen { get; private set; }
        Quaternion closedRotation;
        bool initialized;
        public override string Prompt => IsOpen ? "Close" : "Open";
        void Awake() => Initialize();
        void Initialize() { if (initialized) return; closedRotation = transform.localRotation; initialized = true; }
        public void SetOpen(bool value) { Initialize(); IsOpen = value; onOpenChanged.Invoke(value); }
        public override bool Interact(HallSeatActor actor) { SetOpen(!IsOpen); return true; }
        public void Simulate(float dt)
        {
            Initialize();
            var target = closedRotation * Quaternion.Euler(0, IsOpen ? openAngle : 0, 0);
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, degreesPerSecond * dt);
        }
        void Update() => Simulate(Time.deltaTime);
    }
}
