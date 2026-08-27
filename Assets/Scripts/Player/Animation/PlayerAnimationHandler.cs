using UnityEngine;

namespace Player.Animation
{
    public class PlayerAnimationHandler : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        public void SetTrigger(PlayerAnimationStatus status) => animator.SetTrigger(status.GetAnimatorHash());

        public void ResetTrigger(PlayerAnimationStatus status) => animator.ResetTrigger(status.GetAnimatorHash());

        public void SetBool(PlayerAnimationStatus status, bool value) => animator.SetBool(status.GetAnimatorHash(), value);

        public void SetFloat(PlayerAnimationStatus status, float value) => animator.SetFloat(status.GetAnimatorHash(), value);

        public void SetFloat(PlayerAnimationStatus status, float value, float dampTime, float deltaTime) =>
            animator.SetFloat(status.GetAnimatorHash(), value, dampTime, deltaTime);
    }
}
