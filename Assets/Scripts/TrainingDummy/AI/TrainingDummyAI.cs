using UnityEngine;

namespace TrainingDummy.AI
{
    [RequireComponent(typeof(TrainingDummyMeleeAttacker))]
    public class TrainingDummyAI : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [SerializeField] private float attackRange          = 2f;
        [SerializeField] private bool doAttack              = true;
        [SerializeField] private bool showAttackRange       = false;

        private TrainingDummyMeleeAttacker _attacker;

        private void Awake()
        {
            _attacker = GetComponent<TrainingDummyMeleeAttacker>();
        }

        private void Update()
        {
            if (!doAttack) return;
            if(target == null) return;

            var sqrDist = (target.position - transform.position).sqrMagnitude;
            if(sqrDist > attackRange * attackRange) return;

            _attacker.TryAttack(target);
        }

        private void OnDrawGizmos()
        {
            if(!showAttackRange) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
