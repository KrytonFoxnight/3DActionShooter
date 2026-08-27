using System;
using Combat;
using Enemy.Movement;
using UnityEngine;

namespace Enemy.AI
{
    public class EnemyAI : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Ranges")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float rangeHysteresis = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showRanges = false;

        private EnemyMovement _movement;
        private EnemyMeleeAttack _attack;
        private IDamageable _targetDamageable;

        private EnemyAIStateType _state = EnemyAIStateType.Idle;

        public event Action<EnemyAIStateType> StateChanged;

        public EnemyAIStateType CurrentState => _state;

        #region Lifecycle

        public bool IsInitialized { get; private set; }

        public bool Init(EnemyMovement movement, EnemyMeleeAttack attack)
        {
            if (IsInitialized) return true;
            if (!movement || !attack) return false;

            _movement = movement;
            _attack = attack;

            SetTarget(target);

            IsInitialized = true;
            return true;
        }

        public void Tick()
        {
            ChangeState(DecideNext());

            switch (_state)
            {
                case EnemyAIStateType.Idle:
                    break;
                case EnemyAIStateType.Chase:
                    _movement.MoveTo(target.position);
                    break;
                case EnemyAIStateType.Attack:
                    _movement.FaceTowards(target.position);
                    _attack.TryAttack(target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_state), _state, null);
            }
        }

        public void Dispose()
        {
            StateChanged = null;
            IsInitialized = false;
        }

        #endregion

        public void SetTarget(Transform next)
        {
            target = next;
            _targetDamageable = next != null && next.TryGetComponent<IDamageable>(out var damageable) ? damageable : null;
        }

        private bool HasLivingTarget => target != null && _targetDamageable != null && !_targetDamageable.IsDepleted;

        private float PlanarDistanceToTarget()
        {
            var delta = target.position - transform.position;
            delta.y = 0f;

            return delta.magnitude;
        }

        private EnemyAIStateType DecideNext()
        {
            if (!HasLivingTarget) return EnemyAIStateType.Idle;

            var distance = PlanarDistanceToTarget();

            return _state switch
            {
                EnemyAIStateType.Idle => EnemyAIStateType.Chase,

                EnemyAIStateType.Chase =>
                    distance <= attackRange ? EnemyAIStateType.Attack : EnemyAIStateType.Chase,

                EnemyAIStateType.Attack =>
                    _attack.IsAttacking ? EnemyAIStateType.Attack :
                    distance > attackRange + rangeHysteresis ? EnemyAIStateType.Chase :
                    EnemyAIStateType.Attack,

                _ => throw new ArgumentOutOfRangeException(nameof(_state), _state, null)
            };
        }

        private void ChangeState(EnemyAIStateType next)
        {
            if (_state == next) return;

            _state = next;

            switch (next)
            {
                case EnemyAIStateType.Idle:
                    _movement.Halt();
                    break;
                case EnemyAIStateType.Chase:
                    break;
                case EnemyAIStateType.Attack:
                    _movement.Halt();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(next), next, null);
            }

            StateChanged?.Invoke(next);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showRanges) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
