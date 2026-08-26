using System.Collections;
using Combat;
using Player.Animation;
using Player.State;
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

        private PlayerState _state;
        private Coroutine _pendingHit;

        // 남은 시간을 감산하지 않고 "언제부터 가능한가"를 기록한다.
        // 감산 방식은 매 프레임 갱신이 필요해 컴포넌트 구동 순서에 의존하게 된다.
        private float _attackReadyTime;

        private Vector3 HitboxCenter =>
            transform.position + transform.forward * attackConfig.AttackDistance + Vector3.up * attackConfig.AttackHeight;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(PlayerState state)
        {
            if(_initialized) return true;       // 이미 초기화된 것은 실패가 아니다
            if (!state) return false;
            if (!animationHandler || !inputReader || !attackConfig) return false;

            _state = state;

            _state.Damaged += OnDamaged;
            _state.Died += OnDeath;

            _initialized = true;

            return true;
        }

        public void Tick()
        {
            if (!CanAttack) return;
            if (!inputReader.AttackPressed) return;     // 공격 버튼을 누르지 않은 경우

            // 공격 수행 처리
            animationHandler.SetTrigger(PlayerAnimationStatus.Attack);
            _attackReadyTime = Time.time + attackConfig.AttackInterval;
            _pendingHit = StartCoroutine(PerformAttackAfterDelay());
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

        // 행동 층의 준비 상태. 유닛이 행동해도 되는지(생사·경직·조작 잠금)는 권한자가 따로 판단한다.
        private bool CanAttack => Time.time >= _attackReadyTime && _pendingHit == null;

        private void OnDamaged()
        {
            // 피해입었을때, 취소가 가능한 공격인지 체크
            if (!attackConfig.InterruptibleByHit) return;

            CancelPendingHit();
        }

        // 사망은 무기 설정과 무관하게 무조건 취소한다.
        // 슈퍼아머(InterruptibleByHit = false)는 "피격이 안 끊는다"는 규칙이지 "죽어도 안 끊는다"가 아니다.
        private void OnDeath()
        {
            CancelPendingHit();
        }

        // 도중에 어떠한 사유로 인해 공격이 중단된 경우, 데미지 반영이 되지 않도록 중단하는 처리
        private void CancelPendingHit()
        {
            if (_pendingHit == null) return;                    // 진행 중인 데미지 반영 작업이 없는 경우

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
