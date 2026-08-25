using System;
using Combat;
using TrainingDummy.Animation;
using UnityEngine;

namespace TrainingDummy
{
    public class TrainingDummyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private TrainingDummyAnimationHandler animationHandler;
        [SerializeField] private int maxHp = 100;
        [SerializeField] private float hitReactionCooldown = 1.2f;
        [SerializeField] private float hitStunDuration = 0.4f;

        private Health _health;
        private float _hitReactionTimer;
        private float _hitStunTimer;

        public event Action Damaged;
        public bool IsInHitStun => _hitStunTimer > 0f;

        private void Awake()
        {
            _health = new Health(maxHp);
        }

        private void Update()
        {
            if (_hitReactionTimer > 0f) _hitReactionTimer -= Time.deltaTime;
            if (_hitStunTimer > 0f) _hitStunTimer -= Time.deltaTime;
        }

        public void TakeDamage(int amount)
        {
            var applied = _health.ApplyDamage(amount);
            if (applied <= 0) return;

            Debug.Log($"Applied damage: {applied}");
            _hitStunTimer = hitStunDuration;
            Damaged?.Invoke();

            if(_hitReactionTimer > 0f) return;

            _hitReactionTimer = hitReactionCooldown;
            animationHandler.SetTrigger(TrainingDummyAnimationStatus.GetHit);
        }
    }
}
