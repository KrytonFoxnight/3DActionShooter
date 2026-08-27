using System;
using Enemy.State;
using UnityEngine;

namespace Enemy.Spawn
{
    [Serializable]
    public struct EnemySpawnRequest
    {
        [SerializeField] private EnemyCharacter enemy;
        [SerializeField] private int count;

        public EnemySpawnRequest(EnemyCharacter enemy, int count)
        {
            this.enemy = enemy;
            this.count = count;
        }

        public EnemyCharacter Enemy => enemy;

        public int Count => count;

        public bool IsValid => enemy != null && count > 0;
    }
}
