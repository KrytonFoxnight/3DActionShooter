using System.Collections.Generic;
using UnityEngine;

namespace Enemy.Spawn
{
    public class EnemySpawnPointProvider : MonoBehaviour
    {
        [SerializeField] private List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();

        public bool IsInitialized { get; private set; }

        public IReadOnlyList<EnemySpawnPoint> Points => spawnPoints;

        public bool Init()
        {
            if (IsInitialized) return true;
            if (spawnPoints == null || spawnPoints.Count == 0) return false;

            for (var i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i]) continue;

                Debug.LogError($"Spawn Point Is Missing At Index {i}", this);

                return false;
            }

            IsInitialized = true;

            return true;
        }

        public void Dispose()
        {
            IsInitialized = false;
        }
    }
}
