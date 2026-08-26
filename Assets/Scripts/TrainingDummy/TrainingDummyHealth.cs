using Combat;
using TrainingDummy.State;
using UnityEngine;

namespace TrainingDummy
{
    public class TrainingDummyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHp = 100;

        private TrainingDummyState _state;
        private Health _health;

        private bool _initialized;

        public bool IsDepleted => _health.IsDepleted;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(TrainingDummyState state)
        {
            if (_initialized) return true;      // 이미 초기화된 것은 실패가 아닌 것으로 처리, 다만 필요시 Enum 전환으로 상세하게 변경할 수 있음
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

            Debug.Log($"Applied damage: {applied}");

            if (_health.IsDepleted) return;

            _state.ReceiveDamage();
        }
    }
}
