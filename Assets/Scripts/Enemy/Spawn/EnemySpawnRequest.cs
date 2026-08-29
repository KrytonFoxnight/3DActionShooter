using System;
using UnityEngine;

namespace Enemy.Spawn
{
    [Serializable]
    public struct EnemySpawnRequest
    {
        [SerializeField] private EnemyCharacter enemy;

        public EnemySpawnRequest(EnemyCharacter enemy)
        {
            this.enemy = enemy;
        }

        public EnemyCharacter Enemy => enemy;

        public bool IsValid => enemy != null;
    }
}
