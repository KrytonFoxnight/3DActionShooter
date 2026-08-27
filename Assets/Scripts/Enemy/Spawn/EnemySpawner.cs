#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Enemy.Spawn
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemySpawnPointProvider spawnPointProvider = null!;


        private int? _nextPointIndex;

        public bool IsInitialized { get; private set; }

        public bool Init()
        {
            if (IsInitialized) return true;
            if (!spawnPointProvider || !spawnPointProvider.Init()) return false;

            _nextPointIndex = 0;

            IsInitialized = true;

            return true;
        }

        public void Dispose()
        {
            if (spawnPointProvider && spawnPointProvider.IsInitialized) spawnPointProvider.Dispose();

            _nextPointIndex = null;

            IsInitialized = false;
        }

        public IReadOnlyList<EnemyCharacter> Spawn(EnemySpawnRequest request)
        {
            var spawned = new List<EnemyCharacter>();

            if (!IsInitialized || !request.IsValid) return spawned;

            for (var i = 0; i < request.Count; i++)
            {
                if (!TryResolveSpawnPose(out var position, out var rotation)) continue;

                spawned.Add(Instantiate(request.Enemy, position, rotation));
            }

            return spawned;
        }

        private bool TryResolveSpawnPose(out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (!_nextPointIndex.HasValue) return false;

            var points = spawnPointProvider.Points;
            if (points.Count == 0) return false;

            var point = points[_nextPointIndex.Value];
            _nextPointIndex = (_nextPointIndex + 1) % points.Count;

            if (!point) return false;

            if (!point.TryGetSpawnPose(out position, out rotation))
            {
                Debug.LogWarning($"Spawn Point Is Not On NavMesh: {point.name}", point);

                return false;
            }

            return true;
        }

    }
}
