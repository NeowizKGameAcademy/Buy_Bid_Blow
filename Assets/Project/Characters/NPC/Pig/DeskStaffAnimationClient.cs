using UnityEngine;

namespace BuyBidBlow.Characters
{
    // Personal first-visit greeting. No private trading data or network state is changed.
    public sealed class DeskStaffAnimationClient : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip greeting;
        public bool HasGreeted { get; private set; }
        public bool IsGreeting { get; private set; }
        float endsAt;

        public void GreetOnce()
        {
            if (HasGreeted || !animator || !greeting) return;
            HasGreeted = IsGreeting = true;
            endsAt = Time.time + greeting.length;
            animator.applyRootMotion = false;
            animator.CrossFadeInFixedTime("Base Layer.Greeting", .12f, 0);
        }

        void Update()
        {
            if (!IsGreeting || Time.time < endsAt) return;
            IsGreeting = false;
            animator.CrossFadeInFixedTime("Base Layer.Standing Idle", .12f, 0);
        }
    }
}
