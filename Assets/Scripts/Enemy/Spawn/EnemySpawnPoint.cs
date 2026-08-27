using UnityEngine;

namespace Enemy.Spawn
{
    public class EnemySpawnPoint : MonoBehaviour
    {
        public Vector3 Position => transform.position;

        public Quaternion Rotation => transform.rotation;
    }
}
