using Combat;
using Enemy.State;
using UnityEngine;

namespace Enemy
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHp = 100;

        public Health Model { get; private set; }
        private bool _initialized;

        public bool IsDepleted => Model.IsDepleted;

        private EnemyState _state;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(EnemyState state)
        {
            if (_initialized) return true;
            if (!state) return false;

            _state = state;
            Model = new Health(maxHp);
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
            var applied = Model.ApplyDamage(amount);
            if (applied <= 0) return;

            Debug.Log($"Enemy took {applied} damage");

            if (Model.IsDepleted) return;

            _state.ReceiveDamage();
        }
    }
}
