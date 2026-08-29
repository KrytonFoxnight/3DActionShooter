using System;
using Core;
using Player.Animation;
using Player.State;
using UI.HealthBarUI.PlayerUI.NearestEnemyHpBar;
using UnityEngine;

namespace Player
{
    public class PlayerCharacter : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationHandler animationHandler;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerMeleeAttack meleeAttack;
        [SerializeField] private NearestEnemyScanner nearestEnemyScanner;

        [SerializeField] private NearestEnemyHpBarPresenter nearestEnemyHpBarPresenter;

        [SerializeField] private float hitReactionCooldown = 1.2f;

        [Header("Debug")]
        [SerializeField] private bool invincible;

        public event Action Damaged;
        public event Action Died;

        private PlayerStateType _state = PlayerStateType.Alive;

        private float _hitReactionReadyTime;

        public bool IsAlive => _state == PlayerStateType.Alive;

        public bool IsInvincible => invincible;

        private bool IsActionAllowed => IsAlive && !movement.IsInHitStun && !movement.IsControlLocked;

        private bool IsReady =>
            animationHandler != null &&
            health != null && health.IsInitialized &&
            movement != null && movement.IsInitialized &&
            meleeAttack != null && meleeAttack.IsInitialized &&
            nearestEnemyScanner != null && nearestEnemyScanner.IsInitialized &&
            nearestEnemyHpBarPresenter != null && nearestEnemyHpBarPresenter.IsInitialized;

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

            var nearestEnemyDetectorResult = nearestEnemyScanner != null && nearestEnemyScanner.Init();
            var nearestEnemyHpBarResult = nearestEnemyHpBarPresenter != null && nearestEnemyHpBarPresenter.Init();

            var result = animationHandlerResult && healthInitResult && movementInitResult && meleeAttackInitResult &&
                         nearestEnemyDetectorResult && nearestEnemyHpBarResult;

            if (!result)
            {
                LogManager.LogError("Player Init Failed\n" +
                                    $"animationHandler: {animationHandlerResult}\n" +
                                    $"health: {healthInitResult}\n" +
                                    $"movement: {movementInitResult}\n" +
                                    $"meleeAttack: {meleeAttackInitResult}\n" +
                                    $"nearestEnemyScanner: {nearestEnemyDetectorResult}\n" +
                                    $"nearestEnemyHpBarPresenter: {nearestEnemyHpBarResult}", this);
            }

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
            nearestEnemyScanner.Tick();
            nearestEnemyHpBarPresenter.SetTarget(nearestEnemyScanner.NearestHealth, nearestEnemyScanner.NearestDisplayName);

            if (IsActionAllowed) meleeAttack.Tick();
        }

        public void ResetState()
        {
            if (!IsReady) return;
            if (IsAlive) return;

            ChangeState(PlayerStateType.Alive);
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
                    animationHandler.ResetTrigger(PlayerAnimationStatus.Attack);
                    animationHandler.ResetTrigger(PlayerAnimationStatus.GetHit);
                    animationHandler.ResetTrigger(PlayerAnimationStatus.Dash);
                    animationHandler.ResetTrigger(PlayerAnimationStatus.Jump);
                    animationHandler.SetTrigger(PlayerAnimationStatus.Death);
                    Died?.Invoke();
                    break;
                case PlayerStateType.Alive:
                    health.ResetState();
                    movement.ResetMotionState();
                    animationHandler.Rebind();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(next), next, null);
            }
        }

        private void Dispose()
        {
            if (nearestEnemyScanner != null && nearestEnemyScanner.IsInitialized) nearestEnemyScanner.Dispose();
            if (nearestEnemyHpBarPresenter != null && nearestEnemyHpBarPresenter.IsInitialized) nearestEnemyHpBarPresenter.Dispose();
            if (meleeAttack != null && meleeAttack.IsInitialized) meleeAttack.Dispose();
            if (movement != null && movement.IsInitialized) movement.Dispose();
            if (health != null && health.IsInitialized) health.Dispose();
        }
    }
}
