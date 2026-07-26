using UnityEngine;

namespace Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        private InputSystem_Actions _actions;

        private void Awake()
        {
            _actions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            _actions.Player.Enable();
        }

        private void OnDisable()
        {
            _actions.Player.Disable();
        }

        private void OnDestroy()
        {
            _actions?.Dispose();
        }

        public Vector2 MoveInput => _actions.Player.Move.ReadValue<Vector2>();  // 플레이어 이동 입력 전체
        public bool IsSprint => _actions.Player.Sprint.IsPressed();             // 달리기 여부 판정
        public Vector2 LookDelta => _actions.Player.Look.ReadValue<Vector2>();  // 카메라 회전 입력값, 마우스 delta 전체로 함, 스틱 관련이 기본으로 있긴 한데, 지금은 넘어갑니다.
        public bool JumpPressed => _actions.Player.Jump.WasPressedThisFrame();  // 플레이어 점프 입력
    }
}
