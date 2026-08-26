using System.Collections;
using Combat;
using Player.Animation;
using UnityEngine;

namespace Player
{
    public class PlayerMeleeAttack : MonoBehaviour
    {
        private const int MaxHitTargets = 8;

        [Header("Components")]
        [SerializeField] private PlayerAnimationHandler animationHandler;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private AttackConfig attackConfig;

        private readonly Collider[] _hitBuffer = new Collider[MaxHitTargets];

        private bool _initialized;

        private PlayerHealth _health;
        private Coroutine _pendingHit;
        private float _attackCooldownTimer;

        private Vector3 HitboxCenter =>
            transform.position + transform.forward * attackConfig.AttackDistance + Vector3.up * attackConfig.AttackHeight;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(PlayerHealth health)
        {
            if(_initialized) return true;       // 이미 초기화된 것은 실패가 아니다
            if (!health) return false;
            if (!animationHandler || !inputReader || !attackConfig) return false;

            _health = health;
            _health.Damaged += CancelPendingHit;
            _initialized = true;

            return true;
        }

        public void Tick(bool canAttack)
        {
            // 공격 쿨다운 시간 남은 경우
            if (_attackCooldownTimer > 0)
            {
                _attackCooldownTimer -= Time.deltaTime;
                return;
            }

            if (!canAttack) return;

            if (_pendingHit != null) return;

            // 공격 버튼을 누르지 않은 경우
            if (!inputReader.AttackPressed)
            {
                return;
            }

            // 공격 수행 처리
            _attackCooldownTimer = attackConfig.AttackInterval;
            animationHandler.SetTrigger(PlayerAnimationStatus.Attack);
            _pendingHit = StartCoroutine(PerformAttackAfterDelay());
        }

        public void Dispose()
        {
            if (_health != null) _health.Damaged -= CancelPendingHit;

            _initialized = false;
        }

        #endregion

        // 도중에 어떠한 사유로 인해 공격이 중단된 경우, 데미지 반영이 되지 않도록 중단하는 처리
        private void CancelPendingHit()
        {
            if (!attackConfig.InterruptibleByHit) return;       // 중단 가능한 유닛임?
            if (_pendingHit == null) return;                    // 진행 중인 데미지 반영 작업이 없는 경우

            // 공격 중단 처리
            StopCoroutine(_pendingHit);
            _pendingHit = null;
        }

        // 공격 입력 프레임부터 delay만큼 기다리고 데미지를 반영함
        private IEnumerator PerformAttackAfterDelay()
        {
            yield return new WaitForSeconds(attackConfig.HitImpactDelay); // 애니메이션 시작 ~ 데미지 반영 시점까지의 wait

            // 공격 적용 완료
            _pendingHit = null;
            PerformAttack();
        }

        // 공격 수행 메서드
        // 지금은 단순하게 구 형태로 영역 처리를 해서 판정함
        private void PerformAttack()
        {
            var hitCount = Physics.OverlapSphereNonAlloc(HitboxCenter, attackConfig.AttackRadius, _hitBuffer);
            for (var i = 0; i < hitCount; i++)
            {
                var hit = _hitBuffer[i];
                if (hit.gameObject == gameObject) continue;                             // 자기 자신 제외
                if (!hit.TryGetComponent<IDamageable>(out var damageable)) continue;    // 데미지 받을 수 있는 유닛이 아닌 경우

                damageable.TakeDamage(attackConfig.Damage);                             // 데미지 처리
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (attackConfig == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(HitboxCenter, attackConfig.AttackRadius);
        }
    }
}
