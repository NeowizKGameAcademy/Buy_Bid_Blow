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
        // 초기화 수행
        void Awake() => Initialize();
        // 닫힌 회전값을 한 번만 저장
        void Initialize() { if (initialized) return; closedRotation = transform.localRotation; initialized = true; }
        // 열림 상태를 설정하고 변경 이벤트 발생
        public void SetOpen(bool value) { Initialize(); IsOpen = value; onOpenChanged.Invoke(value); }
        // 상호작용 가능하면 열림/닫힘 토글
        public bool Interact(GameObject player) { if (!interactable) return false; SetOpen(!IsOpen); return true; }
        // 목표 각도로 문을 점진적으로 회전
        public void Simulate(float dt)
        {
            Initialize();
            var target = closedRotation * Quaternion.Euler(0, IsOpen ? openAngle : 0, 0);
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, degreesPerSecond * dt);
        }
        // 매 프레임 문 회전 갱신
        void Update() => Simulate(Time.deltaTime);
    }
}
