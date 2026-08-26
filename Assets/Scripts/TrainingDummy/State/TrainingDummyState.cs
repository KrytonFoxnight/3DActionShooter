using TrainingDummy.AI;
using UnityEngine;

namespace TrainingDummy.State
{
    // 더미 유닛 상태의 권한자.
    // 하위 컴포넌트의 초기화·구동 순서를 통제하고, 유닛 상태를 단독으로 소유한다.
    // 하위는 사실(IsInHitStun 등)만 노출하고 상태를 직접 바꾸지 않는다.
    public class TrainingDummyState : MonoBehaviour
    {
        [SerializeField] private TrainingDummyHealth health;
        [SerializeField] private TrainingDummyMeleeAttack meleeAttack;
        [SerializeField] private TrainingDummyAI ai;

        private TrainingDummyStateType _state = TrainingDummyStateType.Alive;

        public bool IsAlive => _state == TrainingDummyStateType.Alive;
        private bool CanAttack => IsAlive && !health.IsInHitStun;

        // 구동 가능 여부는 하위의 초기화 상태에서 파생된다. 권한자가 따로 플래그를 들지 않는다.
        private bool IsReady =>
            health != null && health.IsInitialized &&
            meleeAttack != null && meleeAttack.IsInitialized &&
            ai != null && ai.IsInitialized;

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            if (IsReady) Tick();
        }

        private void OnDestroy()
        {
            // 부분 초기화 상태여도 성공한 것은 정리해야 한다. 조건은 컴포넌트마다 개별 판단한다.
            Dispose();
        }

        private bool Init()
        {
            var healthInitResult = health != null && health.Init();
            var meleeAttackInitResult = health != null && meleeAttack != null && meleeAttack.Init(health);
            var aiInitResult = meleeAttack != null && ai != null && ai.Init(meleeAttack);

            var result = healthInitResult && meleeAttackInitResult && aiInitResult;

            if (!result) Debug.LogError("TrainingDummy Init Failed", this);

            return result;
        }

        private void Tick()
        {
            // 체력 처리는 사망 여부와 무관하게 돌려야 한다. (부활같은거 고려)
            health.Tick();

            if (!IsAlive) return;   // 사망 시 공격 구동 중단

            meleeAttack.Tick();
            ai.Tick(CanAttack);
        }

        // 초기화 역순으로 해제한다. 하위가 앞의 참조를 들고 있으므로 순서가 중요하다.
        private void Dispose()
        {
            if (ai != null && ai.IsInitialized) ai.Dispose();
            if (meleeAttack != null && meleeAttack.IsInitialized) meleeAttack.Dispose();
            if (health != null && health.IsInitialized) health.Dispose();
        }
    }
}
