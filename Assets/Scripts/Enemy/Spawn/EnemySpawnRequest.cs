using System;
using UnityEngine;

namespace Enemy.Spawn
{
    [Serializable]
    public struct EnemySpawnRequest
    {
        [SerializeField] private string displayName;
        [SerializeField] private EnemyCharacter enemy;

        public EnemySpawnRequest(string displayName, EnemyCharacter enemy)
        {
            this.displayName = displayName;
            this.enemy = enemy;
        }

        public string DisplayName => displayName;

        public EnemyCharacter Enemy => enemy;

        public bool IsValid => enemy != null;
    }
}
