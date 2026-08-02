using Combat;
using Player.Animation;
using UnityEngine;

namespace Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private PlayerAnimationHandler animationHandler;

        [SerializeField] private int maxHp = 100;

        private Health _health;

        private void Awake()
        {
            _health = new Health(maxHp);
        }

        public void TakeDamage(int amount)
        {
            var applied = _health.ApplyDamage(amount);
            animationHandler.SetTrigger(PlayerAnimationStatus.GetHit);
            Debug.Log($"Player took {applied} damage");
        }
    }
}
