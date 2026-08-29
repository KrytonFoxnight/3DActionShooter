using Player.Animation;
using UnityEngine;

namespace Player
{
    [RequireComponent(
        typeof(PlayerInputReader),
        typeof(CharacterController)
    )]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerAnimationHandler animationHandler;

        // 플레이어 관련 파라미터들
        [Header("Movement Params")]
        [SerializeField] private float runMoveSpeed = 12f; // 달리기 속도
        [SerializeField] private float walkMoveSpeed = 5f; // 걷기 속도
        [SerializeField] private float walkToggleHoldTime = 0.75f; // 걷기 달리기 전환 홀드 시간
        [SerializeField] private float rotationSpeed = 12f; // 회전 속도
        [SerializeField] private float gravity = -22f; // 중력값
        [SerializeField] private float jumpHeight = 3f; // 점프 높이
        [SerializeField] private float fallMultiplier = 2f; // 하강 계수

        [Header("Dash Params")] [SerializeField] private float dashDistance = 5f;
        [SerializeField] private float dashDuration = 0.4f;
        [SerializeField] private float dashCooldown = 0.5f;
        [SerializeField] private float dashRecoveryTime = 0.08f;

        [Header("Hit Params")]
        [SerializeField] private float hitStunDuration = 0.4f;

        private bool _initialized;

        // 블렌드트리 링 좌표. 안쪽 링(1)=걷기, 바깥 링(2)=달리기
        private const float WalkAnimValue = 1f;
        private const float RunAnimValue = 2f;

        private float _verticalVelocity;
        private bool _isWalking;

        // 걷기 토글 관련
        private float _walkHoldTimer;
        private bool _walkToggleConsumed;

        // 남은 시간을 감산하지 않고 "언제까지 / 언제부터"를 기록한다.
        // 감산 방식은 매 프레임 갱신이 필요해 컴포넌트 구동 순서에 의존하게 된다.
        // 특히 IsControlLocked는 권한자(PlayerState)가 공격 게이트로 읽으므로 갱신 시점에 의존하면 안 된다.

        // 대시 관련
        private float _dashEndTime;         // 대시가 끝나는 시각
        private float _dashReadyTime;       // 대시를 다시 쓸 수 있는 시각
        private Vector3 _dashDirection;     // 대시 방향

        // 조작 잠금 관련
        private float _controlLockEndTime;  // 시점 조작을 제외한 컨트롤 잠금이 풀리는 시각

        // 피격 경직 관련
        private float _hitStunEndTime;      // 피격 경직이 풀리는 시각

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init()
        {
            if (_initialized) return true;      // 이미 초기화된 것은 실패가 아니다
            if(!cameraTransform || !inputReader || !characterController || !animationHandler) return false;

            _initialized = true;

            return true;
        }

        public void Tick()
        {
            // 대시 관련 처리
            UpdateDash();

            // 달리기, 걷기 관련 전환 처리
            UpdateMoveModeToggle();

            // 캐릭터 수직 이동 처리
            UpdatePlayerVerticalVelocity();

            // 이번 Frame 움직임 값, 내부적으로 충돌 처리까지 고려
            characterController.Move(PlayerVelocity * Time.deltaTime);

            var characterMoveDir = CameraRelativeMove; // 카메라 기준 움직여야 하는 방향

            if (IsDashing || IsControlLocked)
            {
                animationHandler.SetBool(PlayerAnimationStatus.IsGrounded, characterController.isGrounded);
                return;
            }

            // 캐릭터 회전 처리
            UpdatePlayerRotation(characterMoveDir);

            // 캐릭터 애니메이터 처리
            UpdatePlayerAnimation(characterMoveDir);
        }

        public void Dispose()
        {
            _initialized = false;
        }

        #endregion

        // Dash 관련 처리
        private void UpdateDash()
        {
            if (IsDashing) return;                                      // 이미 대시 중인 경우
            if (IsControlLocked) return;                                // 조작이 잠긴 경우
            if (!inputReader.DashPressed) return;                       // 이번 프레임에 대시가 없는 경우
            if (Time.time < _dashReadyTime) return;                     // 대시 쿨다운이 남은 경우
            if (!characterController.isGrounded) return;                // 접지 상태가 아닌 경우

            // 댜시할 방향 결정, 움직이는 경우에는 카메라 방향으로 대시하고 정지 상태면 바라보는 방향으로 대시
            _dashDirection = CameraRelativeMove.sqrMagnitude > 0.01f
                ? CameraRelativeMove.normalized
                : transform.forward;

            // 대시 상태 활성화 및 관련 변수들 초기화
            _dashEndTime = Time.time + dashDuration;
            _controlLockEndTime = Time.time + dashDuration + dashRecoveryTime;
            _dashReadyTime = Time.time + dashCooldown;

            var localDash = transform.InverseTransformDirection(_dashDirection);

            animationHandler.SetFloat(PlayerAnimationStatus.DashX, localDash.x);
            animationHandler.SetFloat(PlayerAnimationStatus.DashY, localDash.z);
            animationHandler.SetTrigger(PlayerAnimationStatus.Dash);
        }

        // 기본 이동 토글 처리
        private void UpdateMoveModeToggle()
        {
            if (inputReader.IsWalkKeyHeld)
            {
                _walkHoldTimer += Time.deltaTime;
                if (!_walkToggleConsumed && _walkHoldTimer >= walkToggleHoldTime)
                {
                    _isWalking = !_isWalking;
                    _walkToggleConsumed = true;
                }
            }
            else
            {
                _walkHoldTimer = 0f;
                _walkToggleConsumed = false;
            }
        }

        // 플레이어 수직 속도 처리
        private void UpdatePlayerVerticalVelocity()
        {
            // 캐릭터가 바닥에 있는 경우
            if (characterController.isGrounded)
            {
                _verticalVelocity = -2f; // 접지 상태 안정화 처리

                // 점프 처리, 조작 잠금 중에는 점프 입력 무시
                if (!IsControlLocked && inputReader.JumpPressed)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * -gravity); // 제곱근 음수 방지를 위해 gravity 부호 역전 처리
                    animationHandler.SetTrigger(PlayerAnimationStatus.Jump);
                }
            }
            // 공중에 있는 경우, 낙하 스케일 처리
            else
            {
                var gravityScale = _verticalVelocity < 0f ? fallMultiplier : 1f;
                _verticalVelocity += gravity * gravityScale * Time.deltaTime;
            }
        }

        // 플레이어 회전 처리
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
            var moveAnimValue = _isWalking ? WalkAnimValue : RunAnimValue;

            // 캐릭터 기준으로 변환된 이동 벡터값을 각 parameter에 업데이트
            // 바로 바뀌면 너무 이상하니까 damp time 넣어줘서 점진적 변경되도록 처리
            animationHandler.SetFloat(PlayerAnimationStatus.MoveX, localMove.x * moveAnimValue, 0.1f, Time.deltaTime);
            animationHandler.SetFloat(PlayerAnimationStatus.MoveY, localMove.z * moveAnimValue, 0.1f, Time.deltaTime);
            animationHandler.SetBool(PlayerAnimationStatus.IsGrounded, characterController.isGrounded);
        }

        // 플레이어 조작 잠금. 권한자가 행동 가능 여부를 판단할 때 읽는 사실이다.
        public bool IsControlLocked => Time.time < _controlLockEndTime;

        // 피격 경직도 조작 잠금과 같은 "움직임 제약" 계열이므로 여기서 소유한다.
        // 경직을 걸지 말지는 권한자(PlayerState)가 정하고, 얼마나 지속되는지는 여기가 안다.
        public bool IsInHitStun => Time.time < _hitStunEndTime;

        // 짧은 지속시간이 이미 걸린 긴 지속시간을 덮어쓰지 않도록 Max로 갱신한다.
        public void ApplyHitStun() => _hitStunEndTime = Mathf.Max(_hitStunEndTime, Time.time + hitStunDuration);

        public void ResetMotionState()
        {
            _dashEndTime = 0f;
            _dashReadyTime = 0f;
            _controlLockEndTime = 0f;
            _hitStunEndTime = 0f;
            _verticalVelocity = 0f;
        }

        // 대시 진행 중 여부
        private bool IsDashing => Time.time < _dashEndTime;

        // 플레이어 속력
        private float PlayerMoveSpeed => _isWalking ? walkMoveSpeed : runMoveSpeed;

        // 카메라 전방 수평 단위 벡터
        private Vector3 CameraForwardFlat => Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;

        // 카메라 우측 수평 단위 벡터
        private Vector3 CameraRightFlat => Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        // WASD 입력을 카메라 기준 월드 수평 이동 방향으로 합성
        private Vector3 CameraRelativeMove =>
            CameraForwardFlat * inputReader.MoveInput.y + CameraRightFlat * inputReader.MoveInput.x;

        // 플레이어 대시 속력
        private float DashSpeed => dashDistance / dashDuration;

        // 플레이어 최종 속도
        private Vector3 PlayerVelocity => HorizontalVelocity + VerticalVelocity;

        // 플레이어 수평 속도
        private Vector3 HorizontalVelocity
        {
            get
            {
                if(IsDashing) return _dashDirection * DashSpeed;
                if(IsControlLocked) return _dashDirection * DashSpeed * 0.3f;
                return CameraRelativeMove * PlayerMoveSpeed;
            }
        }

        // 플레이어 수직 속도
        private Vector3 VerticalVelocity => Vector3.up * _verticalVelocity;
    }
}
