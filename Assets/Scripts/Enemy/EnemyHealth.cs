using Combat;
using Enemy.State;
using UnityEngine;

namespace Enemy
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHp = 100;

        private EnemyState _state;
        private Health _health;

        private bool _initialized;

        public bool IsDepleted => _health.IsDepleted;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(EnemyState state)
        {
            if (_initialized) return true;
            if (!state) return false;

            _state = state;
            _health = new Health(maxHp);
            _initialized = true;

            return true;
        }

        public void Dispose()
        {
            _initialized = false;
        }

        #endregion

        public void TakeDamage(int amount)
        {
            var applied = _health.ApplyDamage(amount);
            if (applied <= 0) return;

            Debug.Log($"Enemy took {applied} damage");

            if (_health.IsDepleted) return;

            _state.ReceiveDamage();
        }
    }
}
