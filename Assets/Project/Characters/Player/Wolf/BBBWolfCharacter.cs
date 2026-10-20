using UnityEngine;

namespace BuyBidBlow.Characters
{
    // Animation API. Clothing and materials belong to BBBWolfAppearance.
    public class BBBWolfCharacter : MonoBehaviour
    {
        public enum Face { Happy, Suspicious, Shocked, BlankBluff }
        public enum Action
        {
            Locomotion, Walk, RaiseArms, Wave, Sit, Bid,
            MixamoWalking, Defeat, HandRaising, Flair, HipHopDancing
        }

        public const string ActionParameter = "Action";
        public const string ExpressionParameter = "Expression";
        public const string MovementParameter = "MoveSpeed";
        private static readonly int ActionId = Animator.StringToHash(ActionParameter);
        private static readonly int ExpressionId = Animator.StringToHash(ExpressionParameter);
        private static readonly int MovementId = Animator.StringToHash(MovementParameter);

        public Transform model;
        public Animator animator;
        public BBBWolfAppearance appearance;
        public GameObject paddle;
        public Face expression = Face.BlankBluff;
        [Min(0)] public float speed = 1;

        public void SetExpression(Face value)
        {
            expression = value;
            if (animator) animator.SetInteger(ExpressionId, (int)value);
        }

        // Normalized speed: 0 = idle, 1 = walk. Gameplay owns actual movement.
        public void SetMovementSpeed(float value)
        {
            if (animator) animator.SetFloat(MovementId, Mathf.Clamp01(value));
        }

        public void PlayAction(Action value)
        {
            if (!animator) return;
            animator.speed = speed;
            animator.SetInteger(ActionId, (int)value);
        }

        public void StopAction() => PlayAction(Action.Locomotion);

        // Chair-seated pose, switched instantly with no sit-down or stand-up motion.
        public void SetSeated(bool seated)
        {
            if (animator) animator.Play(seated ? "Seated" : "Locomotion", 0, 0);
        }

        public void SetPlaybackPaused(bool paused)
        {
            if (animator) animator.speed = paused ? 0 : speed;
        }

        // Sandbox scrubbing also uses Animator, never SampleAnimation.
        public void Seek(float normalizedTime)
        {
            if (!animator || !animator.isInitialized) return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(state.fullPathHash, 0, Mathf.Clamp01(normalizedTime));
            animator.Update(0);
        }

        public void SetPaddleVisible(bool visible)
        {
            if (paddle) paddle.SetActive(visible);
        }

        private void Awake()
        {
            if (!animator && model) animator = model.GetComponent<Animator>();
            if (!appearance) appearance = GetComponent<BBBWolfAppearance>();
            SetPaddleVisible(false);
            if (animator)
            {
                animator.speed = speed;
                SetExpression(expression);
            }
        }

        private void OnDisable() => SetPaddleVisible(false);
    }
}
