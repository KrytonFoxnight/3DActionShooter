using UnityEngine;

namespace Player
{
    public class DashStateBehaviour : StateMachineBehaviour
    {
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.TryGetComponent<PlayerMovement>(out var playerMovement)) playerMovement.OnDashAnimationEnd();
        }
    }
}
