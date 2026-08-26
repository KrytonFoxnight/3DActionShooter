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
    public class TrainingDummyMeleeAttack : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private TrainingDummyAnimationHandler animationHandler;
        [SerializeField] private AttackConfig attackConfig;

        private bool _initialized;

        private TrainingDummyHealth _health;
        private Coroutine _pendingHit;
        private float _attackCooldownTimer;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(TrainingDummyHealth health)
        {
            if (_initialized) return true;      // 이미 초기화된 것은 실패가 아니다
            if (!health) return false;
            if (!animationHandler || !attackConfig) return false;

            _health = health;
            _health.Damaged += CancelPendingHit;
            _initialized = true;

            return true;
        }

        public void Tick()
        {
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;
        }

        public void Dispose()
        {
            if (_health != null) _health.Damaged -= CancelPendingHit;

            _initialized = false;
        }

        #endregion

        // 경직·사망으로 인한 공격 차단 판단은 권한자(TrainingDummyState)가 조합해 호출부에 넘긴다.
        public bool TryAttack(Transform target)
        {
            if(_attackCooldownTimer > 0f) return false;
            if(_pendingHit != null) return false;
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
