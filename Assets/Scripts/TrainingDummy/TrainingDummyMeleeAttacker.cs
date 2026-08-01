using Combat;
using UnityEngine;

namespace TrainingDummy
{
    public class TrainingDummyMeleeAttacker : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float attackRange      = 2f;
        [SerializeField] private int attackDamage       = 5;
        [SerializeField] private float attackInterval   = 1.5f;

        [SerializeField] private bool doAttack          = true;
        [SerializeField] private bool showAttackRange   = false;

        private IDamageable _targetDamageable;
        private float _cooldownTimer;

        private void Awake()
        {
            if(target != null) _targetDamageable = target.GetComponent<IDamageable>();
        }

        private void Update()
        {
            if(_targetDamageable == null) return;
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
                return;
            }

            var sqrDist = (target.position - transform.position).sqrMagnitude;
            if (sqrDist > attackRange * attackRange) return;
            if (!doAttack) return;

            _targetDamageable.TakeDamage(attackDamage);
            _cooldownTimer = attackInterval;
        }

        private void OnDrawGizmos()
        {
            if (!showAttackRange) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
