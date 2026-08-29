using System;
using System.Collections.Generic;
using Enemy.Spawn;
using UnityEngine;

namespace Game
{
    [Serializable]
    public class WaveDefinition
    {
        [SerializeField] private List<EnemySpawnRequest> spawns = new List<EnemySpawnRequest>();

        public IReadOnlyList<EnemySpawnRequest> Spawns => spawns;

        public bool IsValid => spawns != null && spawns.Count > 0;
    }
}
