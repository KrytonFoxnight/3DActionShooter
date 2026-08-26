using UnityEngine;

namespace TrainingDummy.AI
{
    public class TrainingDummyAI : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [SerializeField] private float attackRange          = 2f;
        [SerializeField] private bool doAttack              = true;
        [SerializeField] private bool showAttackRange       = false;

        private bool _initialized;

        private TrainingDummyMeleeAttack _attack;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init(TrainingDummyMeleeAttack attack)
        {
            if (_initialized) return true;      // 이미 초기화된 것은 실패가 아니다
            if (!attack) return false;

            _attack = attack;
            _initialized = true;

            return true;
        }

        // canAttack 판단은 권한자(TrainingDummyState)가 조합해서 넘긴다.
        public void Tick(bool canAttack)
        {
            if (!canAttack) return;
            if (!doAttack) return;
            if(target == null) return;

            var sqrDist = (target.position - transform.position).sqrMagnitude;
            if(sqrDist > attackRange * attackRange) return;

            _attack.TryAttack(target);
        }

        public void Dispose()
        {
            _initialized = false;
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
