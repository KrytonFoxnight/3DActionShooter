using UnityEngine;

namespace Combat
{
    [CreateAssetMenu(fileName = "AttackConfig", menuName = "Combat/AttackConfig")]
    public class AttackConfig : ScriptableObject
    {
        [Header("Common")]
        [SerializeField] private int damage                 = 10;
        [SerializeField] private float attackInterval       = 0.5f;
        [SerializeField] private float hitDelay             = 0.3f;

        [Header("Melee Hitbox")]
        [SerializeField] private float attackDistance       = 1.5f;
        [SerializeField] private float attackRadius         = 1f;
        [SerializeField] private float attackHeight         = 1f;

        [Header("Range Recheck")]
        [SerializeField] private float hitRange             = 2.5f;

        [Header("Interrupt")]
        [SerializeField] private bool interruptibleByHit    = true;

        public int Damage => damage;
        public float AttackInterval => attackInterval;
        public float HitDelay => hitDelay;
        public float AttackDistance => attackDistance;
        public float AttackRadius => attackRadius;
        public float AttackHeight => attackHeight;
        public float HitRange => hitRange;
        public bool InterruptibleByHit => interruptibleByHit;
    }
}
