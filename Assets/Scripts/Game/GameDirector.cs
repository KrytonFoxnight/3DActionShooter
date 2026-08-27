using Core;
using Enemy.Spawn;
using UnityEngine;

namespace Game
{
    public class GameDirector : MonoBehaviour
    {
        [SerializeField] private EnemySpawner enemySpawner;

        [SerializeField] private Transform enemyTarget;

        [SerializeField] private EnemySpawnRequest initialEnemySpawn;

        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private bool Init()
        {
            var spawnerInitResult = enemySpawner != null && enemySpawner.Init();

            if (!spawnerInitResult)
            {
                LogManager.LogError("GameDirector Init Failed", this);

                return false;
            }

            RequestEnemySpawn(initialEnemySpawn);

            return true;
        }

        private void RequestEnemySpawn(EnemySpawnRequest request)
        {
            var spawned = enemySpawner.Spawn(request);

            for (var i = 0; i < spawned.Count; i++)
            {
                spawned[i].SetTarget(enemyTarget);
            }
        }

        private void Dispose()
        {
            if (enemySpawner != null && enemySpawner.IsInitialized) enemySpawner.Dispose();
        }
    }
}
