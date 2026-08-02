using System.Collections;
using Combat;
using TrainingDummy.Animation;
using UnityEngine;

namespace TrainingDummy
{
    public class TrainingDummyMeleeAttack : MonoBehaviour
    {
        [Header("Animation Handler"), SerializeField] private TrainingDummyAnimationHandler animationHandler;
        [Header("Combat Config"), SerializeField] private AttackConfig attackConfig;

        private float _cooldownTimer;

        private void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        }

        public bool TryAttack(Transform target)
        {
            if(_cooldownTimer > 0f) return false;
            if(!target.TryGetComponent<IDamageable>(out var damageable)) return false;

            animationHandler.SetTrigger(TrainingDummyAnimationStatus.Attack);
            _cooldownTimer = attackConfig.AttackInterval;
            StartCoroutine(DealDamageAfterDelay(target, damageable));
            return true;
        }

        private IEnumerator DealDamageAfterDelay(Transform target, IDamageable damageable)
        {
            yield return new WaitForSeconds(attackConfig.HitDelay);

            if (target == null) yield break;

            var sqrDist = (target.position - transform.position).sqrMagnitude;
            if(sqrDist > attackConfig.HitRange * attackConfig.HitRange) yield break;

            damageable.TakeDamage(attackConfig.Damage);
        }
    }
}
