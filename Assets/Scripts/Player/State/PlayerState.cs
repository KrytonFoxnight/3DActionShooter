using UnityEngine;

namespace Player.State
{
    // 플레이어 유닛 상태의 권한자.
    // 하위 컴포넌트의 초기화·구동 순서를 통제하고, 유닛 상태를 단독으로 소유한다.
    // 하위는 사실(IsControlLocked, IsInHitStun 등)만 노출하고 상태를 직접 바꾸지 않는다.
    public class PlayerState : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerMeleeAttack meleeAttack;

        private PlayerStateType _state = PlayerStateType.Alive;

        public bool IsAlive => _state == PlayerStateType.Alive;
        private bool CanAttack => IsAlive && !movement.IsControlLocked && !health.IsInHitStun;

        // 구동 가능 여부는 하위의 초기화 상태에서 파생된다. 권한자가 따로 플래그를 들지 않는다.
        private bool IsReady =>
            health != null && health.IsInitialized &&
            movement != null && movement.IsInitialized &&
            meleeAttack != null && meleeAttack.IsInitialized;

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
            var movementInitResult = movement != null && movement.Init();
            var meleeAttackInitResult = health != null && meleeAttack != null && meleeAttack.Init(health);

            var result = healthInitResult && movementInitResult && meleeAttackInitResult;

            if (!result) Debug.LogError("Player Init Failed", this);

            return result;
        }

        private void Tick()
        {
            // 체력 처리는 사망 여부와 무관하게 돌려야 한다. (부활같은거 고려)
            health.Tick();

            if (!IsAlive) return;   // 사망 시 이동·공격 구동 중단

            movement.Tick();
            meleeAttack.Tick(CanAttack);
        }

        // 초기화 역순으로 해제한다. meleeAttack이 health를 참조하므로 순서가 중요하다.
        private void Dispose()
        {
            if (meleeAttack != null && meleeAttack.IsInitialized) meleeAttack.Dispose();
            if (movement != null && movement.IsInitialized) movement.Dispose();
            if (health != null && health.IsInitialized) health.Dispose();
        }
    }
}
