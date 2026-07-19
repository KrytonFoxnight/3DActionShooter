using Player;
using UnityEngine;

namespace Cameras
{
    /// <summary>
    /// 매 프레임마다 pitch와 yaw를 계산하여 카메라를 위치시키는 컴포넌트
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Components")] [SerializeField] private Transform targetTransform;

        [SerializeField] private PlayerInputReader playerInputReader;

        [Header("Parameters")] [SerializeField]
        private float distance = 2f;

        [SerializeField] private float pivotHeight = 1.5f;
        [SerializeField] private float sensitivity = 0.15f;
        [SerializeField] private float pitchMin = -30f;
        [SerializeField] private float pitchMax = 60f;

        private Camera _camera;

        private CameraRigProperty _rig;
        private Vector3 _shakeOffset;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            UpdateRig();
            ApplyToCamera(_rig);
        }

        private void UpdateRig()
        {
            var lookDelta = playerInputReader.LookDelta;
            _rig.Yaw += lookDelta.x * sensitivity;
            _rig.Pitch = Mathf.Clamp(_rig.Pitch - lookDelta.y * sensitivity, pitchMin, pitchMax);
            _rig.Distance = distance;
            _rig.PivotHeight = pivotHeight;
            _rig.TargetPosition = targetTransform.position;
        }

        private void ApplyToCamera(CameraRigProperty rig)
        {
            var rotation = Quaternion.Euler(rig.Pitch, rig.Yaw, 0f);
            var pivot = rig.TargetPosition + Vector3.up * rig.PivotHeight;
            var position = pivot - rotation * Vector3.forward * rig.Distance + _shakeOffset;

            _camera.transform.SetPositionAndRotation(position, rotation);
        }

        private void OnDrawGizmosSelected()
        {
            if (targetTransform == null) return;

            var pivot = targetTransform.position + Vector3.up * pivotHeight;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(pivot, 0.15f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pivot, transform.position);
        }
    }
}
