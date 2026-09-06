using System.Collections;
using Combat;
using Enemy.Animation;
using Enemy.State;
using UnityEngine;

namespace Enemy
{
    public class EnemyMeleeAttack : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private EnemyAnimationHandler animationHandler;
        [SerializeField] private AttackConfig attackConfig;

        private EnemyCharacter _character;
        private Coroutine _pendingHit;

        private bool _initialized;
        private float _attackReadyTime;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(EnemyCharacter character)
        {
            if (_initialized) return true;
            if (!character) return false;
            if (!animationHandler || !attackConfig) return false;

            _character = character;

            _character.Damaged += OnDamaged;
            _character.Died += OnDeath;

            _initialized = true;
            return true;
        }

        public void Dispose()
        {
            if (_character != null)
            {
                _character.Damaged -= OnDamaged;
                _character.Died -= OnDeath;
            }

            CancelPendingHit();

            _initialized = false;
        }

        #endregion

        public bool IsAttacking => _pendingHit != null;

        private bool CanAttack => Time.time >= _attackReadyTime && _pendingHit == null;

        public bool TryAttack(Transform target)
        {
            if (!CanAttack) return false;
            if (!target.TryGetComponent<IDamageable>(out var damageable)) return false;
            if (damageable.IsDepleted) return false;

            animationHandler.SetTrigger(EnemyAnimationStatus.Attack);
            _attackReadyTime = Time.time + attackConfig.AttackInterval;
            _pendingHit = StartCoroutine(DealDamageAfterDelay(target, damageable));

            return true;
        }

        private void OnDamaged()
        {
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
            if (damageable.IsDepleted) yield break;

            var sqrDist = (target.position - transform.position).sqrMagnitude;
            if (sqrDist > attackConfig.HitRange * attackConfig.HitRange) yield break;

            damageable.TakeDamage(attackConfig.Damage);
        }
    }
}
