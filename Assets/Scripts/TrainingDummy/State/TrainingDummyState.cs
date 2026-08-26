using System;
using TrainingDummy.AI;
using TrainingDummy.Animation;
using UnityEngine;

namespace TrainingDummy.State
{
    public class TrainingDummyState : MonoBehaviour
    {
        [SerializeField] private TrainingDummyAnimationHandler animationHandler;
        [SerializeField] private TrainingDummyHealth health;
        [SerializeField] private TrainingDummyMeleeAttack meleeAttack;
        [SerializeField] private TrainingDummyAI ai;

        // 마땅히 둘만한 곳이 없어서 이곳에 위치
        [SerializeField] private float hitStunDuration = 0.4f;
        [SerializeField] private float hitReactionCooldown = 1.2f;

        public event Action Damaged;
        public event Action Died;

        private TrainingDummyStateType _state = TrainingDummyStateType.Alive;
        private float _hitStunEndTime;
        private float _hitReactionReadyTime;

        public bool IsAlive => _state == TrainingDummyStateType.Alive;
        public bool IsInHitStun => Time.time < _hitStunEndTime;
        private bool IsActionAllowed => IsAlive && !IsInHitStun;

        private bool IsReady =>
            animationHandler != null &&
            health != null && health.IsInitialized &&
            meleeAttack != null && meleeAttack.IsInitialized &&
            ai != null && ai.IsInitialized;

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
            Dispose();
        }

        private bool Init()
        {
            var animationHandlerResult = animationHandler != null;
            var healthInitResult = health != null && health.Init(this);
            var meleeAttackInitResult = meleeAttack != null && meleeAttack.Init(this);
            var aiInitResult = meleeAttack != null && ai != null && ai.Init(meleeAttack);

            var result = animationHandlerResult && healthInitResult && meleeAttackInitResult && aiInitResult;

            if (!result) Debug.LogError("TrainingDummy Init Failed", this);

            return result;
        }

        private void Tick()
        {
            // 사망 상태 확인
            if (health.IsDepleted)
            {
                ChangeState(TrainingDummyStateType.Dead);
                return;
            }

            if(IsActionAllowed) ai.Tick();
        }

        public void ReceiveDamage()
        {
            RefreshStun(hitStunDuration);
            PlayHitReaction();
            Damaged?.Invoke();
        }

        // 짧은 지속시간이 이미 걸린 긴 지속시간을 덮어쓰지 않도록 Max로 갱신한다.
        private void RefreshStun(float duration) => _hitStunEndTime = Mathf.Max(_hitStunEndTime, Time.time + duration);

        // 경직과 지속시간이 다르다. 연타로 맞아도 모션이 매번 처음부터 다시 재생되지 않도록 별도 쿨다운을 둔다.
        private void PlayHitReaction()
        {
            if (Time.time < _hitReactionReadyTime) return;

            _hitReactionReadyTime = Time.time + hitReactionCooldown;
            animationHandler.SetTrigger(TrainingDummyAnimationStatus.GetHit);
        }

        private void ChangeState(TrainingDummyStateType next)
        {
            if (_state == next) return;     // 같은 상태로의 재진입 처리 안함

            _state = next;

            switch (next)
            {
                case TrainingDummyStateType.Dead:
                    animationHandler.SetTrigger(TrainingDummyAnimationStatus.Death);
                    Died?.Invoke();
                    break;
                case TrainingDummyStateType.Alive:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(next), next, null);
            }
        }

        private void Dispose()
        {
            if (ai != null && ai.IsInitialized) ai.Dispose();
            if (meleeAttack != null && meleeAttack.IsInitialized) meleeAttack.Dispose();
            if (health != null && health.IsInitialized) health.Dispose();
        }
    }
}
