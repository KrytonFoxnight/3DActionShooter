using System.Collections;
using Combat;
using Player.Animation;
using UnityEngine;

namespace Player
{
    public class PlayerMeleeAttack : MonoBehaviour
    {
        private const int MaxHitTargets = 8;

        [Header("Animation Handler"), SerializeField] private PlayerAnimationHandler animationHandler;

        [Header("Config"), SerializeField] private AttackConfig attackConfig;


        private readonly Collider[] _hitBuffer = new Collider[MaxHitTargets];

        private PlayerInputReader _inputReader;
        private PlayerMovement _movement;
        private float _cooldownTimer;

        private void Awake()
        {
            _inputReader = GetComponent<PlayerInputReader>();
            _movement = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            if (_cooldownTimer > 0)
            {
                _cooldownTimer -= Time.deltaTime;
                return;
            }

            if (_movement.IsControlLocked) return;

            if (!_inputReader.AttackPressed)
            {
                return;
            }

            _cooldownTimer = attackConfig.AttackInterval;
            animationHandler.SetTrigger(PlayerAnimationStatus.Attack);
            StartCoroutine(PerformAttackAfterDelay());
        }

        private IEnumerator PerformAttackAfterDelay()
        {
            yield return new WaitForSeconds(attackConfig.HitDelay);
            PerformAttack();
        }

        private void PerformAttack()
        {
            var hitCount = Physics.OverlapSphereNonAlloc(HitboxCenter, attackConfig.AttackRadius, _hitBuffer);
            for (var i = 0; i < hitCount; i++)
            {
                var hit = _hitBuffer[i];
                if (hit.gameObject == gameObject) continue;
                if (!hit.TryGetComponent<IDamageable>(out var damageable)) continue;

                damageable.TakeDamage(attackConfig.Damage);
            }
        }

        private Vector3 HitboxCenter =>
            transform.position + transform.forward * attackConfig.AttackDistance + Vector3.up * attackConfig.AttackHeight;

        private void OnDrawGizmosSelected()
        {
            if (attackConfig == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(HitboxCenter, attackConfig.AttackRadius);
        }
    }
}
