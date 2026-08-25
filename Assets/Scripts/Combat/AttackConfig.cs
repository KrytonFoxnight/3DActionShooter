using UnityEngine;

namespace Combat
{
    [CreateAssetMenu(fileName = "AttackConfig", menuName = "Combat/AttackConfig")]
    public class AttackConfig : ScriptableObject
    {
        [Header("Common")]
        [SerializeField] private int damage                 = 10;           // 공격으로 부여할 데미지
        [SerializeField] private float attackInterval       = 0.5f;         // 공격과 공격간의 간격
        [SerializeField] private float hitImpactDelay       = 0.3f;         // 공격 시작 프레임부터 실제 데미지 연산 처리 사이에 있을 대기 시간

        /// <summary>
        /// 근접 공격 판정용 sphere 파라미터
        /// 나중에 더 나은 hitbox 방식으로 변경 시 변경될 수 있음
        /// </summary>
        [Header("Melee Hitbox")]
        [SerializeField] private float attackDistance       = 1.5f;         // 공격 거리
        [SerializeField] private float attackRadius         = 1f;           // 공격 반지름
        [SerializeField] private float attackHeight         = 1f;           // 공격 높이

        [Header("Range Recheck")]
        [SerializeField] private float hitRange             = 2.5f;         // 데미지 반영 순간, 목표가 해당 이상으로 벗어난 경우 데미지를 반영하지 않는 거리

        [Header("Interrupt")]
        [SerializeField] private bool interruptibleByHit    = true;         // 피격 판정이 현재 진행 중인 공격 처리를 중단할 수 있는가?

        // Getters
        public int Damage => damage;
        public float AttackInterval => attackInterval;
        public float HitImpactDelay => hitImpactDelay;
        public float AttackDistance => attackDistance;
        public float AttackRadius => attackRadius;
        public float AttackHeight => attackHeight;
        public float HitRange => hitRange;
        public bool InterruptibleByHit => interruptibleByHit;
    }
}
