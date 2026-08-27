using Enemy.Animation;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.Movement
{
    public class EnemyMovement : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private EnemyAnimationHandler animationHandler;

        [Header("Rotation")]
        [SerializeField] private float faceRotateSensitivity = 0.001f;

        [Header("Hit Params")]
        [SerializeField] private float hitStunDuration = 0.4f;

        [Header("Pathing")]
        [SerializeField] private float repathThreshold = 0.5f;

        [Header("Animation")]
        [SerializeField] private float moveSpeedDampTime = 0.01f;
        [SerializeField] private float referenceRunClipSpeed = 10f;         // enemy의 이동 애니메이션이 원래 어떤 속도를 전제했는지 넣는 매직넘버

        private bool _initialized;
        private float _hitStunEndTime;

        private Vector3 _lastDestination;
        private bool _hasDestination;

        #region Lifecycle

        public bool IsInitialized => _initialized;

        public bool Init()
        {
            if (_initialized) return true;
            if (!agent || !animationHandler) return false;
            if (referenceRunClipSpeed <= 0.01f) return false;

            _initialized = true;
            return true;
        }

        public void Tick()
        {
            if (agent.isOnNavMesh) agent.isStopped = IsInHitStun;

            animationHandler.SetFloat(EnemyAnimationStatus.MoveSpeed, NormalizedSpeed, moveSpeedDampTime, Time.deltaTime);
            animationHandler.SetFloat(EnemyAnimationStatus.MoveSpeedMultiplier, LocomotionPlaybackSpeed);
        }

        public void Dispose()
        {
            _initialized = false;
        }

        #endregion

        public bool IsInHitStun => Time.time < _hitStunEndTime;

        // navmesh agent의 최대 속도값과 현재 속도값으로 현재 속도를 정규화
        private float NormalizedSpeed => agent.speed > 0f ? agent.velocity.magnitude / agent.speed : 0f;

        // 이동 애니메이션 속도 배율값
        private float LocomotionPlaybackSpeed => agent.speed / referenceRunClipSpeed;

        public void ApplyHitStun() => _hitStunEndTime = Mathf.Max(_hitStunEndTime, Time.time + hitStunDuration);

        // NavAgent 기능을 활용해 목표 지점으로 이동
        public void MoveTo(Vector3 destination)
        {
            if (!agent.isOnNavMesh) return;

            // 목적지가 설정된 상태면서 이전 목적지와 큰 차이가 없는 상태인 경우, 상대적으로 비싼 navmesh agent의 목적지 설정을 안하도록 처리
            if (_hasDestination && (destination - _lastDestination).sqrMagnitude < repathThreshold * repathThreshold) return;

            _lastDestination = destination;
            _hasDestination = true;

            agent.SetDestination(destination);
        }

        // 현재 움직임 상태를 중단
        public void Halt()
        {
            _hasDestination = false;

            if (!agent.isOnNavMesh) return;

            agent.ResetPath();                  // 경로 폐기
            agent.velocity = Vector3.zero;      // 속도 폐기
        }

        // enemy 오브젝트가 position을 바라보도록 회전 처리
        public void FaceTowards(Vector3 position)
        {
            var dir = position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < faceRotateSensitivity) return;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), agent.angularSpeed * Time.deltaTime);
        }
    }
}
