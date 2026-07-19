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

        public Vector2 MoveInput => _actions.Player.Move.ReadValue<Vector2>();  // 플레이어 이동 입력
        public bool IsSprint => _actions.Player.Sprint.IsPressed();             // 달리기 여부
        public Vector2 LookDelta => _actions.Player.Look.ReadValue<Vector2>();  //
        public bool JumpPressed => _actions.Player.Jump.WasPressedThisFrame();  // 플레이어 점프
    }
}
