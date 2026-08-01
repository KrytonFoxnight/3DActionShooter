using Combat;
using UnityEngine;

namespace Player
{
    public class PlayerMeleeAttack : MonoBehaviour
    {
        private const int MaxHitTargets = 8;

        [SerializeField] private float attackDistance = 1.5f;
        [SerializeField] private float attackRadius = 1f;
        [SerializeField] private float attackHeight = 1f;
        [SerializeField] private int attackDamage = 10;
        [SerializeField] private float attackInterval = 0.5f;

        private readonly Collider[] _hitBuffer = new Collider[MaxHitTargets];

        private static readonly int Attack = Animator.StringToHash("Attack");

        private PlayerInputReader _inputReader;
        private PlayerMovement _movement;
        private Animator _animator;
        private float _cooldownTimer;

        private void Awake()
        {
            _inputReader = GetComponent<PlayerInputReader>();
            _movement = GetComponent<PlayerMovement>();
            _animator = GetComponent<Animator>();
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

            _cooldownTimer = attackInterval;
            _animator.SetTrigger(Attack);
            PerformAttack();
        }

        private void PerformAttack()
        {
            var hitCount = Physics.OverlapSphereNonAlloc(HitboxCenter, attackRadius, _hitBuffer);
            for (var i = 0; i < hitCount; i++)
            {
                var hit = _hitBuffer[i];
                if (hit.gameObject == gameObject) continue;
                if (!hit.TryGetComponent<IDamageable>(out var damageable)) continue;

                damageable.TakeDamage(attackDamage);
            }
        }

        private Vector3 HitboxCenter =>
            transform.position + transform.forward * attackDistance + Vector3.up * attackHeight;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(HitboxCenter, attackRadius);
        }
    }
}
