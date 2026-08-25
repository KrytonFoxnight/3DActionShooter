using System.Collections;
using Combat;
using TrainingDummy.Animation;
using UnityEngine;

namespace TrainingDummy
{
    /// <summary>
    /// 데미지 연산, 최소한의 기능 확인을 위해 구현된 연습용 더미의 공격 컴포넌트
    /// Player의 컴포넌트와 유사하게 처리됨
    /// </summary>
    [RequireComponent(typeof(TrainingDummyHealth))]
    public class TrainingDummyMeleeAttack : MonoBehaviour
    {
        [Header("Animation Handler"), SerializeField] private TrainingDummyAnimationHandler animationHandler;
        [Header("Combat Config"), SerializeField] private AttackConfig attackConfig;

        private TrainingDummyHealth _health;
        private Coroutine _pendingHit;
        private float _attackCooldownTimer;

        private void Awake()
        {
            _health = GetComponent<TrainingDummyHealth>();
        }

        private void OnEnable() => _health.Damaged += CancelPendingHit;
        private void OnDisable() => _health.Damaged -= CancelPendingHit;

        private void Update()
        {
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;
        }

        public bool TryAttack(Transform target)
        {
            if(_attackCooldownTimer > 0f) return false;
            if(_health.IsInHitStun) return false;
            if(!target.TryGetComponent<IDamageable>(out var damageable)) return false;

            animationHandler.SetTrigger(TrainingDummyAnimationStatus.Attack);
            _attackCooldownTimer = attackConfig.AttackInterval;
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
            yield return new WaitForSeconds(attackConfig.HitImpactDelay);
            _pendingHit = null;

            if (target == null) yield break;

            // 목표가 움직여서 HitRange 이상으로 떨어진 곳에 있는 경우, 데미지 반영을 하지 않음
            var sqrDist = (target.position - transform.position).sqrMagnitude;
            if(sqrDist > attackConfig.HitRange * attackConfig.HitRange) yield break;

            damageable.TakeDamage(attackConfig.Damage);
        }
    }
}
