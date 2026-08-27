using Combat;
using Core;
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

        private EnemyCharacter _character;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(EnemyCharacter character)
        {
            if (_initialized) return true;
            if (!character) return false;

            _character = character;
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

            LogManager.Log($"Enemy took {applied} damage");

            if (Model.IsDepleted) return;

            _character.ReceiveDamage();
        }
    }
}
