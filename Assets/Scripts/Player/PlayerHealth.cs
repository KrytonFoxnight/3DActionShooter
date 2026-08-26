using System;
using Combat;
using Player.Animation;
using UnityEngine;

namespace Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private PlayerAnimationHandler animationHandler;

        [SerializeField] private int maxHp = 100;
        [SerializeField] private float hitReactionCooldown = 1.2f;
        [SerializeField] private float hitStunDuration = 0.4f;

        private bool _initialized;

        private Health _health;
        private float _hitReactionTimer;
        private float _hitStunTimer;

        public event Action Damaged;
        public bool IsInHitStun => _hitStunTimer > 0f;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init()
        {
            if (_initialized) return true;      // 이미 초기화된 것은 실패가 아니다
            if (!animationHandler) return false;

            _health = new Health(maxHp);
            _initialized = true;

            return true;
        }

        public void Tick()
        {
            if(_hitReactionTimer > 0f) _hitReactionTimer -= Time.deltaTime;
            if(_hitStunTimer > 0f) _hitStunTimer -= Time.deltaTime;         // 경직 상태 관련
        }

        public void Dispose()
        {
            _initialized = false;
        }

        #endregion

        public void TakeDamage(int amount)
        {
            var applied = _health.ApplyDamage(amount);
            if(applied <= 0) return; // 적용된 데미지가 없는 경우

            // 데미지 처리
            Debug.Log($"Player took {applied} damage");
            Damaged?.Invoke();

            // 스턴 처리
            _hitStunTimer = hitStunDuration;

            if (_hitReactionTimer > 0f) return; // 피격 처리 중인 경우

            _hitReactionTimer = hitReactionCooldown;
            animationHandler.SetTrigger(PlayerAnimationStatus.GetHit);
        }
    }
}
