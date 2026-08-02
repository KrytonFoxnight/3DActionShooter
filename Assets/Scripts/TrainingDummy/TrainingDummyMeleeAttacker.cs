using Combat;
using TrainingDummy.Animation;
using UnityEngine;

namespace TrainingDummy
{
    public class TrainingDummyMeleeAttacker : MonoBehaviour
    {
        [SerializeField] private TrainingDummyAnimationHandler animationHandler;

        [SerializeField] private int attackDamage       = 5;
        [SerializeField] private float attackInterval   = 1.5f;

        private float _cooldownTimer;

        private void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        }

        public bool TryAttack(Transform target)
        {
            if(_cooldownTimer > 0f) return false;
            if(!target.TryGetComponent<IDamageable>(out var damageable)) return false;

            damageable.TakeDamage(attackDamage);
            animationHandler.SetTrigger(TrainingDummyAnimationStatus.Attack);
            _cooldownTimer = attackInterval;
            return true;
        }
    }
}
