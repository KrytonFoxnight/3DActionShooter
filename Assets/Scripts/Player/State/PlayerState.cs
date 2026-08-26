using System;
using Player.Animation;
using UnityEngine;

namespace Player.State
{
    // 플레이어 유닛 상태의 권한자.
    // 하위 컴포넌트의 초기화·구동 순서를 통제하고, 유닛 상태를 단독으로 소유한다.
    // 하위는 사실(IsControlLocked, IsInHitStun, IsDepleted 등)만 노출하고 상태를 직접 바꾸지 않는다.
    public class PlayerState : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationHandler animationHandler;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerMeleeAttack meleeAttack;

        // 피격 모션 재생 주기. 이동에도 HP에도 속하지 않는 값이라 권한자가 들고 있는다.
        // 경직(hitStunDuration)은 움직임 제약이므로 PlayerMovement가 소유한다.
        [SerializeField] private float hitReactionCooldown = 1.2f;

        public event Action Damaged;
        public event Action Died;

        private PlayerStateType _state = PlayerStateType.Alive;

        // 남은 시간을 감산하지 않고 "언제부터"를 기록한다.
        // 감산 방식은 매 프레임 갱신이 필요해 컴포넌트 구동 순서에 의존하게 된다.
        private float _hitReactionReadyTime;

        public PlayerStateType CurrentState => _state;
        public bool IsAlive => _state == PlayerStateType.Alive;
        public bool IsDead => _state == PlayerStateType.Dead;

        // 이동은 경직 중에도 돌린다. 경직은 공격만 막는다.
        private bool IsActionAllowed => IsAlive && !movement.IsInHitStun && !movement.IsControlLocked;

        // 구동 가능 여부는 하위의 초기화 상태에서 파생된다. 권한자가 따로 플래그를 들지 않는다.
        private bool IsReady =>
            animationHandler != null &&
            health != null && health.IsInitialized &&
            movement != null && movement.IsInitialized &&
            meleeAttack != null && meleeAttack.IsInitialized;

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            if (IsReady) Tick();
        }

        private void OnDestroy()
        {
            // 부분 초기화 상태여도 성공한 것은 정리해야 한다. 조건은 컴포넌트마다 개별 판단한다.
            Dispose();
        }

        // 하위에 인스턴스를 구체 타입 그대로 주입
        // 다만 추후 과권한 문제가 생길 우려가 있을 때 인터페이스로 추출하는 방안 고려, 지금은 아직 필요성이 없음
        private bool Init()
        {
            var animationHandlerResult = animationHandler != null;
            var healthInitResult = health != null && health.Init(this);
            var movementInitResult = movement != null && movement.Init();
            var meleeAttackInitResult = meleeAttack != null && meleeAttack.Init(this);

            var result = animationHandlerResult && healthInitResult && movementInitResult && meleeAttackInitResult;

            if (!result) Debug.LogError("Player Init Failed", this);

            return result;
        }

        private void Tick()
        {
            // 사망 상태 확인
            if (health.IsDepleted)
            {
                ChangeState(PlayerStateType.Dead);
                return;
            }

            movement.Tick();

            if (IsActionAllowed) meleeAttack.Tick();
        }

        // 피격의 결과로 유닛이 무엇을 겪는지를 여기서 정한다. 지속시간은 각 소유자가 안다.
        public void ReceiveDamage()
        {
            movement.ApplyHitStun();
            PlayHitReaction();
            Damaged?.Invoke();
        }

        // 경직과 지속시간이 다르다. 연타로 맞아도 모션이 매번 처음부터 다시 재생되지 않도록 별도 쿨다운을 둔다.
        private void PlayHitReaction()
        {
            if (Time.time < _hitReactionReadyTime) return;

            _hitReactionReadyTime = Time.time + hitReactionCooldown;
            animationHandler.SetTrigger(PlayerAnimationStatus.GetHit);
        }

        private void ChangeState(PlayerStateType next)
        {
            if (_state == next) return;     // 같은 상태로의 재진입 처리 안함

            _state = next;

            switch (next)
            {
                case PlayerStateType.Dead:
                    animationHandler.SetTrigger(PlayerAnimationStatus.Death);
                    Died?.Invoke();
                    break;
                case PlayerStateType.Alive:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(next), next, null);
            }
        }

        // 초기화 역순으로 해제한다.
        private void Dispose()
        {
            if (meleeAttack != null && meleeAttack.IsInitialized) meleeAttack.Dispose();
            if (movement != null && movement.IsInitialized) movement.Dispose();
            if (health != null && health.IsInitialized) health.Dispose();
        }
    }
}
