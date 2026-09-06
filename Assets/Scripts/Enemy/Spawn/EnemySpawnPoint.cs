using UnityEngine;
using UnityEngine.AI;

namespace Enemy.Spawn
{
    public class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private float navMeshSampleDistance = 2f;

        [Header("Debug")]
        [SerializeField] private bool showGizmo = true;

        [SerializeField] private Color gizmoColor = new Color(1f, 0.45f, 0f);

        public Vector3 Position => transform.position;

        public Quaternion Rotation => transform.rotation;

        public bool TryGetSpawnPose(out Vector3 position, out Quaternion rotation)
        {
            position = Position;
            rotation = Rotation;

            if (!NavMesh.SamplePosition(Position, out var hit, navMeshSampleDistance, NavMesh.AllAreas)) return false;

            position = hit.position;

            return true;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo || navMeshSampleDistance <= 0f) return;

            Gizmos.color = gizmoColor;

            Gizmos.DrawWireSphere(Position, navMeshSampleDistance);
            Gizmos.DrawSphere(Position, 0.12f);
            Gizmos.DrawRay(Position, Rotation * Vector3.forward * navMeshSampleDistance);
        }
    }
}
