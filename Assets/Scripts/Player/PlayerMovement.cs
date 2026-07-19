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

        [Header("Parameters")] [SerializeField]
        private float baseMoveSpeed = 5f;

        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float jumpHeight = 3f;
        [SerializeField] private float fallMultiplier = 2f;

        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int JumpTrigger = Animator.StringToHash("Jump");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");

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
            if (_characterController.isGrounded)
            {
                _verticalVelocity = -2f;
                _isSprinting = _inputReader.IsSprint;

                if (_inputReader.JumpPressed)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    _animator.SetTrigger(JumpTrigger);
                }
            }
            else
            {
                var gravityScale = _verticalVelocity < 0f ? fallMultiplier : 1f;
                _verticalVelocity += gravity * gravityScale * Time.deltaTime;
            }

            _characterController.Move(PlayerVelocity * Time.deltaTime);

            var moveDir = CameraRelativeMove;
            if (moveDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir),
                    rotationSpeed * Time.deltaTime);
            }

            var localMove = transform.InverseTransformDirection(moveDir);
            var sprintMultiplier = _isSprinting ? 2f : 1f;
            _animator.SetFloat(MoveX, localMove.x * sprintMultiplier, 0.1f, Time.deltaTime);
            _animator.SetFloat(MoveY, localMove.z * sprintMultiplier, 0.1f, Time.deltaTime);
            _animator.SetBool(IsGrounded, _characterController.isGrounded);
        }

        private float PlayerMoveSpeed => _isSprinting ? baseMoveSpeed * 2 : baseMoveSpeed; // 플레이어 속력
        private Vector3 CameraForwardFlat => Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        private Vector3 CameraRightFlat => Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        private Vector3 CameraRelativeMove =>
            CameraForwardFlat * _inputReader.MoveInput.y + CameraRightFlat * _inputReader.MoveInput.x;

        private Vector3 PlayerVelocity =>
            CameraRelativeMove * PlayerMoveSpeed + Vector3.up * _verticalVelocity; // 플레이어 최종 속도
    }
}
