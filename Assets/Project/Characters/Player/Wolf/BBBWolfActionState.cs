using UnityEngine;

namespace BuyBidBlow.Characters
{
    // Consume requests once and bind the paddle to actual Animator state.
    public sealed class BBBWolfActionState : StateMachineBehaviour
    {
        public bool showPaddle;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetInteger(BBBWolfCharacter.ActionParameter, -1);
            var character = animator.GetComponentInParent<BBBWolfCharacter>();
            if (character) character.SetPaddleVisible(showPaddle);
        }

    }
}
