using System;
using System.Collections.Generic;
using LostDungeon.Game.Pooling;
using UnityEngine;

namespace LostDungeon.Game.Enemies
{
    /// <summary>Spawns enemies from the pool and says when they are all dead.</summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Serializable]
        public class SpawnPoint
        {
            public EnemyController prefab;
            public Transform point;
        }

        [SerializeField] private List<SpawnPoint> spawns = new List<SpawnPoint>();
        [SerializeField] private bool spawnOnStart = true;

        private readonly List<EnemyController> alive = new List<EnemyController>();

        public event Action AllEnemiesDefeated;

        public int AliveCount => alive.Count;
        public int TotalCount => spawns.Count;

        private void Start()
        {
            if (spawnOnStart)
                SpawnAll();
        }

        public void SpawnAll()
        {
            foreach (SpawnPoint spawn in spawns)
            {
                if (spawn.prefab == null || spawn.point == null)
                    continue;

                EnemyController enemy = PoolManager.Instance.Spawn(spawn.prefab, spawn.point.position, Quaternion.identity);
                enemy.Died += OnEnemyDied;
                alive.Add(enemy);
            }
        }

        private void OnEnemyDied(EnemyController enemy)
        {
            enemy.Died -= OnEnemyDied;
            alive.Remove(enemy);
            if (alive.Count == 0)
                AllEnemiesDefeated?.Invoke();
        }

        private void OnDestroy()
        {
            foreach (EnemyController enemy in alive)
            {
                if (enemy != null)
                    enemy.Died -= OnEnemyDied;
            }
        }
    }
}
