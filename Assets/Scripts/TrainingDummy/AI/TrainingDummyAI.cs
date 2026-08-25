using UnityEngine;

namespace TrainingDummy.AI
{
    [RequireComponent(typeof(TrainingDummyMeleeAttack))]
    public class TrainingDummyAI : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [SerializeField] private float attackRange          = 2f;
        [SerializeField] private bool doAttack              = true;
        [SerializeField] private bool showAttackRange       = false;

        private TrainingDummyMeleeAttack _attack;

        private void Awake()
        {
            _attack = GetComponent<TrainingDummyMeleeAttack>();
        }

        private void Update()
        {
            if (!doAttack) return;
            if(target == null) return;

            var sqrDist = (target.position - transform.position).sqrMagnitude;
            if(sqrDist > attackRange * attackRange) return;

            _attack.TryAttack(target);
        }

        private void OnDrawGizmos()
        {
            if(!showAttackRange) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
