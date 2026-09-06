using UnityEngine;

namespace Enemy.Animation
{
    public class EnemyAnimationHandler : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        public void SetTrigger(EnemyAnimationStatus status) => animator.SetTrigger(status.GetAnimatorHash());

        public void ResetTrigger(EnemyAnimationStatus status) => animator.ResetTrigger(status.GetAnimatorHash());

        public void SetFloat(EnemyAnimationStatus status, float value) => animator.SetFloat(status.GetAnimatorHash(), value);

        public void SetFloat(EnemyAnimationStatus status, float value, float dampTime, float deltaTime) =>
            animator.SetFloat(status.GetAnimatorHash(), value, dampTime, deltaTime);
    }
}
