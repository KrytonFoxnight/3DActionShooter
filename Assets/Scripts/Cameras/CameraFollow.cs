using Player;
using UnityEngine;

namespace Cameras
{
    /// <summary>
    /// Player의 입력으로부터 구도(Rig)를 계산하고 최종 Transform으로 Camera의 위치를 바꾸는 컴포넌트
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        // 외부 연결 컴포넌트들
        [SerializeField] private Transform targetTransform;             // 목표 transform
        [SerializeField] private PlayerInputReader playerInputReader;   // 플레이어 입력 (시점 관련 처리용)

        // 파라미터들
        [SerializeField] private float distance = 3f;                   // 카메라의 pivot으로부터의 거리
        [SerializeField] private float pivotHeight = 1.5f;              // 카메라 궤도의 중심 높이
        [SerializeField] private float sensitivity = 0.25f;             // 화면 이동에 따른 회전 각도 전환 배율값
        [SerializeField] private float pitchMin = -30f;                 // pitch 최소값 (상하 회전)
        [SerializeField] private float pitchMax = 60f;                  // pitch 최대값

        private Camera _camera;

        private CameraRigProperty _rig;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            UpdateRig();
            ApplyToCamera(_rig);
        }

        /// <summary>
        /// 카메라의 Rig 파라미터 업데이트 수행
        /// Player의 이동 수행이 Update 생명주기 함수에서 이루어짐
        /// 같은 생명주기 함수에서 호출 순서가 보장되지 않으므로 카메라 처리는 LateUpdate를 사용하기로 결정
        /// </summary>
        private void UpdateRig()
        {
            if (!targetTransform) return;
            if (!playerInputReader) return;

            var lookDelta = playerInputReader.LookDelta;
            _rig.Yaw += lookDelta.x * sensitivity;

            // 마우스 delta값 방향 맞추기 위해서 부호 반전 처리
            _rig.Pitch = Mathf.Clamp(_rig.Pitch - lookDelta.y * sensitivity, pitchMin, pitchMax);
            _rig.Distance = distance;
            _rig.PivotHeight = pivotHeight;
            _rig.TargetPosition = targetTransform.position;
        }

        /// <summary>
        /// 최종 연산된 Rig 파라미터들로 카메라 Transform 적용
        /// </summary>
        /// <param name="rig"></param>
        private void ApplyToCamera(CameraRigProperty rig)
        {
            // Rig 방식으로 기술된 정보를 토대로, Unity에서 사용되는 회전 정보인 Quaternion으로 변환
            // Roll은 사용처가 아직 없으므로 0f로 고정 처리
            var rotation = Quaternion.Euler(rig.Pitch, rig.Yaw, 0f);

            // Rig 기반으로 Pivot의 world space 좌표 계산
            var pivot = rig.TargetPosition + Vector3.up * rig.PivotHeight;

            // pivot을 바라보는 방향의 반대로 distance만큼 떨어진 방향으로 position 설정
            var position = pivot - rotation * Vector3.forward * rig.Distance;

            // 카메라의 transform 으로 최종 적용
            _camera.transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>
        /// 씬 뷰 디버그용
        /// 궤도 피벗 및 피벗과 카메라 사이의 연결선을 그림
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (targetTransform == null) return;

            // 플레이 상황에서는 _rig가 계산 SSOT이므로 다음과 같이 분기 처리
            // Editor에서는 그냥 SerializeField 값을 기준으로 pivot 설정
            var pivot = Application.isPlaying
                ? _rig.TargetPosition + Vector3.up * _rig.PivotHeight
                : targetTransform.position + Vector3.up * pivotHeight;

            // pivot 위치 그리기
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(pivot, 0.15f);

            // pivot과 카메라 간의 연결선 그리기
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pivot, transform.position);
        }
    }
}
