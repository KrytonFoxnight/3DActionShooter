using System;
using Player.Animation;
using UnityEngine;

namespace Player.State
{
    public class PlayerState : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationHandler animationHandler;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerMeleeAttack meleeAttack;

        [SerializeField] private float hitReactionCooldown = 1.2f;

        public event Action Damaged;
        public event Action Died;

        private PlayerStateType _state = PlayerStateType.Alive;

        private float _hitReactionReadyTime;

        public bool IsAlive => _state == PlayerStateType.Alive;

        private bool IsActionAllowed => IsAlive && !movement.IsInHitStun && !movement.IsControlLocked;

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
            Dispose();
        }

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
            if (health.IsDepleted)
            {
                ChangeState(PlayerStateType.Dead);
                return;
            }

            movement.Tick();

            if (IsActionAllowed) meleeAttack.Tick();
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
            animationHandler.SetTrigger(PlayerAnimationStatus.GetHit);
        }

        private void ChangeState(PlayerStateType next)
        {
            if (_state == next) return;     // 같은 상태로의 재진입 처리 방지

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

        private void Dispose()
        {
            if (meleeAttack != null && meleeAttack.IsInitialized) meleeAttack.Dispose();
            if (movement != null && movement.IsInitialized) movement.Dispose();
            if (health != null && health.IsInitialized) health.Dispose();
        }
    }
}
