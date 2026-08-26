using Combat;
using Player.State;
using UnityEngine;

namespace Player
{
    // 데미지 진입점과 HP 보관만 담당한다.
    // 피격의 결과로 유닛이 무엇을 겪는지(경직·피격 모션)는 권한자(PlayerState)가 정한다.
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHp = 100;

        private PlayerState _state;
        private Health _health;
        private bool _initialized;

        public bool IsDepleted => _health.IsDepleted;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(PlayerState state)
        {
            if (_initialized) return true;      // 이미 초기화된 것은 실패가 아니다
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
            if (applied <= 0) return;   // 적용된 데미지가 없는 경우

            Debug.Log($"Player took {applied} damage");

            // HP가 바닥난 타격은 피격이 아니라 사망이다. 사망 전이는 권한자가 다음 Tick에서 판단한다.
            if (_health.IsDepleted) return;

            _state.ReceiveDamage();
        }
    }
}
