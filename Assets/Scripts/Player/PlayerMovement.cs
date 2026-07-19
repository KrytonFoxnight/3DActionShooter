using UnityEngine;

namespace Player
{
    [RequireComponent(
        typeof(Animator),
        typeof(PlayerInputReader),
        typeof(CharacterController)
    )]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Components")] [SerializeField]
        private Transform cameraTransform;

        // 플레이어 관련 파라미터들
        [SerializeField] private float baseMoveSpeed = 5f; // 움직임 속도
        [SerializeField] private float rotationSpeed = 12f; // 회전 속도
        [SerializeField] private float gravity = -22f; // 중력값
        [SerializeField] private float jumpHeight = 3f; // 점프 높이
        [SerializeField] private float fallMultiplier = 2f; // 하강 계수

        // Animator 관련 파라미터들
        private static readonly int MoveX = Animator.StringToHash("MoveX"); // 캐릭터 좌우 이동 성분
        private static readonly int MoveY = Animator.StringToHash("MoveY"); // 캐릭터 전후 이동 성분
        private static readonly int JumpTrigger = Animator.StringToHash("Jump"); // 점프 트리거
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded"); // 지면 상태 여부

        // Animator에서 사용되는 클립 위치값들
        // sprint의 경우, 다른 값으로 조정되면 여기 바꿔야 함
        private const float SprintAnimValue = 2f;
        private const float DefaultAnimValue = 1f;

        private Animator _animator;
        private PlayerInputReader _inputReader;
        private CharacterController _characterController;

        private float _verticalVelocity;
        private bool _isSprinting;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _inputReader = GetComponent<PlayerInputReader>();
            _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // 캐릭터 수직 이동 처리
            UpdatePlayerVerticalVelocity();

            // 이번 Frame 움직임 값, 내부적으로 충돌 처리까지 고려
            _characterController.Move(PlayerVelocity * Time.deltaTime);

            var characterMoveDir = CameraRelativeMove; // 카메라 기준 움직여야 하는 방향

            // 캐릭터 회전 처리
            UpdatePlayerRotation(characterMoveDir);

            // 캐릭터 애니메이터 처리
            UpdatePlayerAnimation(characterMoveDir);
        }

        private void UpdatePlayerVerticalVelocity()
        {
            // 캐릭터가 바닥에 있는 경우
            if (_characterController.isGrounded)
            {
                _verticalVelocity = -2f;                // 접지 상태 안정화 처리
                _isSprinting = _inputReader.IsSprint;   // 캐릭터의 sprint 상태 최신화

                // 점프 처리
                if (_inputReader.JumpPressed)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * -gravity); // 제곱근 음수 방지를 위해 gravity 부호 역전 처리
                    _animator.SetTrigger(JumpTrigger);
                }
            }
            // 공중에 있는 경우, 낙하 스케일 처리
            else
            {
                var gravityScale = _verticalVelocity < 0f ? fallMultiplier : 1f;
                _verticalVelocity += gravity * gravityScale * Time.deltaTime;
            }
        }

        private void UpdatePlayerRotation(Vector3 characterMoveDir)
        {
            // 캐릭터 회전 처리
            if (characterMoveDir.sqrMagnitude > 0.01f) // 입력이 있는 경우에만 회전되도록 처리 (입력 크기 0.1f 무시 처리)
            {
                // 캐릭터의 현재 rotation을 moveDir를 목표로, 매 프레임마다 단위 시간 업데이트를 수행
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(characterMoveDir),
                    rotationSpeed * Time.deltaTime);
            }
        }

        // Animator 관련 처리
        // IsGrounded 프로퍼티 사용을 위해서 반드시 character controller의 Move 호출 이후에 사용되어야 함
        private void UpdatePlayerAnimation(Vector3 characterMoveDir)
        {
            // 기존에 world space 기준으로 기술된 캐릭터 움직임 벡터를 캐릭터의 local space 기준으로 변환
            var localMove = transform.InverseTransformDirection(characterMoveDir);
            var sprintMultiplier = _isSprinting ? SprintAnimValue : DefaultAnimValue;

            // 캐릭터 기준으로 변환된 이동 벡터값을 각 parameter에 업데이트
            // 바로 바뀌면 너무 이상하니까 damp time 넣어줘서 점진적 변경되도록 처리
            _animator.SetFloat(MoveX, localMove.x * sprintMultiplier, 0.1f, Time.deltaTime);
            _animator.SetFloat(MoveY, localMove.z * sprintMultiplier, 0.1f, Time.deltaTime);
            _animator.SetBool(IsGrounded, _characterController.isGrounded);
        }

        // 플레이어 속력
        private float PlayerMoveSpeed => _isSprinting ? baseMoveSpeed * 2 : baseMoveSpeed;

        // 카메라 전방 수평 단위 벡터
        private Vector3 CameraForwardFlat => Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;

        // 카메라 우측 수평 단위 벡터
        private Vector3 CameraRightFlat => Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        // WASD 입력을 카메라 기준 월드 수평 이동 방향으로 합성
        private Vector3 CameraRelativeMove =>
            CameraForwardFlat * _inputReader.MoveInput.y + CameraRightFlat * _inputReader.MoveInput.x;

        // 플레이어 최종 속도
        private Vector3 PlayerVelocity =>
            CameraRelativeMove * PlayerMoveSpeed + Vector3.up * _verticalVelocity;
    }
}
