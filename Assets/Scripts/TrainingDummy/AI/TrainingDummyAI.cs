using UnityEngine;

namespace TrainingDummy.AI
{
    public class TrainingDummyAI : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float attackRange          = 2f;       // 공격 범위
        [SerializeField] private bool doAttack              = true;     // 자동 공격 활성화 (디버그 용)
        [SerializeField] private bool showAttackRange       = false;    // 공격 범위 (디버그 용)

        private TrainingDummyMeleeAttack _attack;

        #region Lifecycle

        public bool IsInitialized { get; private set; }

        public bool Init(TrainingDummyMeleeAttack attack)
        {
            if (IsInitialized) return true;      // 이미 초기화된 것은 실패 아닌 것으로 처리
            if (!attack) return false;

            _attack = attack;
            IsInitialized = true;

            return true;
        }

        public void Tick()
        {
            // 공격 가능한 상태인지 확인
            if (!doAttack) return;
            if(target == null) return;

            // 공격 처리 중에 거리가 다시 멀어진 경우 확인
            var sqrDist = (target.position - transform.position).sqrMagnitude;
            if(sqrDist > attackRange * attackRange) return;

            _attack.TryAttack(target);
        }

        public void Dispose()
        {
            IsInitialized = false;
        }

        #endregion

        private void OnDrawGizmos()
        {
            if(!showAttackRange) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
