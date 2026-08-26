using System;
using Enemy.AI;
using Enemy.Animation;
using Enemy.Movement;
using UnityEngine;

namespace Enemy.State
{
    public class EnemyState : MonoBehaviour
    {
        [SerializeField] private EnemyAnimationHandler animationHandler;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyMovement movement;
        [SerializeField] private EnemyMeleeAttack meleeAttack;
        [SerializeField] private EnemyAI ai;

        [SerializeField] private float hitReactionCooldown = 1.2f;

        public event Action Damaged;
        public event Action Died;

        private EnemyStateType _state = EnemyStateType.Alive;
        private float _hitReactionReadyTime;

        public bool IsAlive => _state == EnemyStateType.Alive;

        private bool IsActionAllowed => IsAlive && !movement.IsInHitStun;

        private bool IsReady =>
            animationHandler != null &&
            health != null && health.IsInitialized &&
            movement != null && movement.IsInitialized &&
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
            var movementInitResult = movement != null && movement.Init();
            var meleeAttackInitResult = meleeAttack != null && meleeAttack.Init(this);
            var aiInitResult = ai != null && movement != null && meleeAttack != null && ai.Init(movement, meleeAttack);

            var result = animationHandlerResult && healthInitResult && movementInitResult && meleeAttackInitResult && aiInitResult;

            if (!result) Debug.LogError("Enemy Init Failed", this);

            return result;
        }

        private void Tick()
        {
            if (health.IsDepleted)
            {
                ChangeState(EnemyStateType.Dead);
                return;
            }

            movement.Tick();

            if (IsActionAllowed) ai.Tick();
        }

        public void ReceiveDamage()
        {
            movement.ApplyHitStun();
            PlayHitReaction();
            Damaged?.Invoke();
        }

        private void PlayHitReaction()
        {
            if (Time.time < _hitReactionReadyTime) return;

            _hitReactionReadyTime = Time.time + hitReactionCooldown;
            animationHandler.SetTrigger(EnemyAnimationStatus.GetHit);
        }

        private void ChangeState(EnemyStateType next)
        {
            if (_state == next) return;

            _state = next;

            switch (next)
            {
                case EnemyStateType.Dead:
                    movement.Halt();
                    animationHandler.SetFloat(EnemyAnimationStatus.MoveSpeed, 0f);
                    animationHandler.SetTrigger(EnemyAnimationStatus.Death);
                    Died?.Invoke();
                    break;
                case EnemyStateType.Alive:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(next), next, null);
            }
        }

        private void Dispose()
        {
            if (ai != null && ai.IsInitialized) ai.Dispose();
            if (meleeAttack != null && meleeAttack.IsInitialized) meleeAttack.Dispose();
            if (movement != null && movement.IsInitialized) movement.Dispose();
            if (health != null && health.IsInitialized) health.Dispose();
        }
    }
}
