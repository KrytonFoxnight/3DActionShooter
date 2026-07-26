using UnityEngine;

namespace Cameras
{
    /// <summary>
    /// 카메라의 구도를 기술하는 구조체
    /// 계산 용이성 및 외부 효과 Blending 용이성을 확보하기 위해 최종 Transform 계산 전까지는 Rig 방식으로 계산함
    /// GC 부담으로부터 자유롭도록 구조체 채택
    /// </summary>
    public struct CameraRigProperty
    {
        public Vector3 TargetPosition;  // 따라갈 대상의 world space에서의 좌표
        public float Yaw;               // 좌우 회전 각도 (degree)
        public float Pitch;             // 상하 회전 각도 (degree, +가 아랫방향)
        public float Distance;          // 궤도 중심으로부터의 거리
        public float PivotHeight;       // 따라가는 대상 기준 궤도 중심 높이
    }
}
