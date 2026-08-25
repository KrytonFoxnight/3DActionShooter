using UnityEngine;

namespace TrainingDummy.Animation
{
    public class TrainingDummyAnimationHandler : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        public void SetTrigger(TrainingDummyAnimationStatus status) => animator.SetTrigger(status.GetAnimatorHash());
    }
}
