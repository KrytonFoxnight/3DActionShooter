using System.Collections;
using Combat;
using TrainingDummy.Animation;
using UnityEngine;

namespace TrainingDummy
{
    [RequireComponent(typeof(TrainingDummyHealth))]
    public class TrainingDummyMeleeAttack : MonoBehaviour
    {
        [Header("Animation Handler"), SerializeField] private TrainingDummyAnimationHandler animationHandler;
        [Header("Combat Config"), SerializeField] private AttackConfig attackConfig;

        private TrainingDummyHealth _health;
        private Coroutine _pendingHit;
        private float _cooldownTimer;

        private void Awake()
        {
            _health = GetComponent<TrainingDummyHealth>();
        }

        private void OnEnable() => _health.Damaged += CancelPendingHit;
        private void OnDisable() => _health.Damaged -= CancelPendingHit;

        private void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        }

        public bool TryAttack(Transform target)
        {
            if(_cooldownTimer > 0f) return false;
            if(_health.IsInHitStun) return false;
            if(!target.TryGetComponent<IDamageable>(out var damageable)) return false;

            animationHandler.SetTrigger(TrainingDummyAnimationStatus.Attack);
            _cooldownTimer = attackConfig.AttackInterval;
            _pendingHit = StartCoroutine(DealDamageAfterDelay(target, damageable));
            return true;
        }

        private void CancelPendingHit()
        {
            if (!attackConfig.InterruptibleByHit) return;
            if (_pendingHit == null) return;

            StopCoroutine(_pendingHit);
            _pendingHit = null;
        }

        private IEnumerator DealDamageAfterDelay(Transform target, IDamageable damageable)
        {
            yield return new WaitForSeconds(attackConfig.HitDelay);
            _pendingHit = null;

            if (target == null) yield break;

            var sqrDist = (target.position - transform.position).sqrMagnitude;
            if(sqrDist > attackConfig.HitRange * attackConfig.HitRange) yield break;

            damageable.TakeDamage(attackConfig.Damage);
        }
    }
}
