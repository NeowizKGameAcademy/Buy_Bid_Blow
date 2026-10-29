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
            MixamoWalking, Defeat, HandRaising, Flair, HipHopDancing, Jump
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

        // 표정을 설정하고 애니메이터 파라미터에 반영
        public void SetExpression(Face value)
        {
            expression = value;
            if (animator) animator.SetInteger(ExpressionId, (int)value);
        }

        // 이동 블렌드 값을 0~1로 설정
        // Normalized speed: 0 = idle, 1 = walk. Gameplay owns actual movement.
        public void SetMovementSpeed(float value)
        {
            if (animator) animator.SetFloat(MovementId, Mathf.Clamp01(value));
        }

        // 재생 속도를 설정하고 지정 액션 재생 요청
        public void PlayAction(Action value)
        {
            if (!animator) return;
            animator.speed = speed;
            animator.SetInteger(ActionId, (int)value);
        }

        // 기본 이동 상태로 액션 되돌리기
        public void StopAction() => PlayAction(Action.Locomotion);

        // 앉은 자세 또는 기본 이동 상태로 즉시 전환
        // Chair-seated pose, switched instantly with no sit-down or stand-up motion.
        public void SetSeated(bool seated)
        {
            if (animator) animator.Play(seated ? "Seated" : "Locomotion", 0, 0);
        }

        // 애니메이션 재생 일시정지 또는 재개
        public void SetPlaybackPaused(bool paused)
        {
            if (animator) animator.speed = paused ? 0 : speed;
        }

        // 현재 애니메이션 상태를 지정한 정규화 시간으로 이동
        // Sandbox scrubbing also uses Animator, never SampleAnimation.
        public void Seek(float normalizedTime)
        {
            if (!animator || !animator.isInitialized) return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(state.fullPathHash, 0, Mathf.Clamp01(normalizedTime));
            animator.Update(0);
        }

        // 패들 오브젝트 표시 여부 설정
        public void SetPaddleVisible(bool visible)
        {
            if (paddle) paddle.SetActive(visible);
        }

        // 애니메이터·외형 참조를 찾고 패들을 숨기며 재생 속도와 표정 초기화
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

        // 비활성화 시 패들 숨김
        private void OnDisable() => SetPaddleVisible(false);
    }
}
