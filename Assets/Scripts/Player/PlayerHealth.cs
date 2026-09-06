using Combat;
using Core;
using Player.State;
using UnityEngine;

namespace Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHp = 100;

        private PlayerCharacter _character;
        public Health Model { get; private set; }
        private bool _initialized;

        public bool IsDepleted => Model.IsDepleted;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(PlayerCharacter character)
        {
            if (_initialized) return true;      // 이미 초기화된 것은 실패가 아니다
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
            if (_character.IsInvincible) return;

            var applied = Model.ApplyDamage(amount);
            if (applied <= 0) return;   // 적용된 데미지가 없는 경우

            LogManager.Log($"Player took {applied} damage");

            if (Model.IsDepleted) return;

            _character.ReceiveDamage();
        }

        public void ResetState()
        {
            if (!_initialized) return;

            Model.ResetToFull();
        }
    }
}
