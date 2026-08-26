using System.Collections;
using Combat;
using TrainingDummy.Animation;
using TrainingDummy.State;
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


        private TrainingDummyState _state;
        private Coroutine _pendingHit;

        private bool _initialized;
        private float _attackReadyTime;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(TrainingDummyState state)
        {
            if (_initialized) return true;      // 이미 초기화된 것은 실패가 아니다
            if (!state) return false;
            if (!animationHandler || !attackConfig) return false;

            _state = state;

            _state.Damaged += OnDamaged;
            _state.Died += OnDeath;

            _initialized = true;
            return true;
        }

        public void Dispose()
        {
            if (_state != null)
            {
                _state.Damaged -= OnDamaged;
                _state.Died -= OnDeath;
            }

            CancelPendingHit();     // 잔여 공격 처리 정리

            _initialized = false;
        }

        #endregion

        private bool CanAttack => Time.time >= _attackReadyTime && _pendingHit == null;

        public bool TryAttack(Transform target)
        {
            if(!CanAttack) return false;
            if(!target.TryGetComponent<IDamageable>(out var damageable)) return false;
            if(damageable.IsDepleted) return false;

            // 공격 처리 수행
            animationHandler.SetTrigger(TrainingDummyAnimationStatus.Attack);
            _attackReadyTime = Time.time + attackConfig.AttackInterval;
            _pendingHit = StartCoroutine(DealDamageAfterDelay(target, damageable));

            return true;
        }

        private void OnDamaged()
        {
            // 피해입었을때, 취소가 가능한 공격인지 체크
            if (!attackConfig.InterruptibleByHit) return;

            CancelPendingHit();
        }

        private void OnDeath()
        {
            CancelPendingHit();
        }

        private void CancelPendingHit()
        {
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
