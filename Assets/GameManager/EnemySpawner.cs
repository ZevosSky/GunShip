// @author: Gary Yang
// @description: Systematic Scalable Enemy Spawner will take prefabs and updatable
//               Configs for logic around spawning. Enemies

using System;
using System.Collections.Generic;
using UnityEngine;
using Enemies;
using GameManager;

namespace GameManager
{
    public class EnemySpawner : MonoBehaviour
    {
        [Serializable]
        public struct EnemySpawnInfo
        {
            [Range(0f, 100f)] public float      spawnWeight;
            [Range(1,  100)]  public int        spawnLimit;
            public GameObject                   prefab;
            [Tooltip("Mark this entry as the boss. It spawns only once and never again after it is fully defeated.")]
            public bool                         isBoss;
        }

        [Header("Spawn Config")]
        [SerializeField] private List<EnemySpawnInfo> enemySpawnInfos = new();
        [SerializeField] private float                spawnInterval   = 3f;
        [SerializeField] private int                  spawnBatchSize  = 1;   // enemies per tick
        [SerializeField] private Vector2              spawnRadiusBand = new Vector2(15f, 25f);

        [Header("Boss Encounter")]
        /// <summary>True once any boss entry in the list has been fully cleared.</summary>
        public bool BossCleared { get; private set; }
        /// <summary>True while a boss entry is alive in the world.</summary>
        public bool BossAlive   { get; private set; }

        [Header("Spawning")]
        [Tooltip("Pause / resume all spawning at runtime without disabling the component.")]
        [SerializeField] public bool spawnPaused = false;

        [Header("World")]
        [SerializeField] private World.TorusWorld     world;

        [HideInInspector] public Transform target;

        private float   _timer;
        private int[]   _spawnCounts;
        private float   _totalWeight;

        void Start()
        {
            _spawnCounts = new int[enemySpawnInfos.Count];
            RecalcWeight();

            // Fallback: if GameManager hasn't wired a target, find the player ourselves
            if (target == null)
            {
                var sc = FindFirstObjectByType<RocketShip.ShipController>();
                if (sc != null)
                    target = sc.transform;
                else
                    Debug.LogWarning("[EnemySpawner] No target and no ShipController found — enemies will not spawn.");
            }

            if (HasBossEntry())
                Enemies.BossController.OnBossFullyCleared += HandleBossCleared;
        }

        void OnDestroy()
        {
            if (HasBossEntry())
                Enemies.BossController.OnBossFullyCleared -= HandleBossCleared;
        }

        bool HasBossEntry()
        {
            foreach (var info in enemySpawnInfos)
                if (info.isBoss) return true;
            return false;
        }

        void HandleBossCleared()
        {
            BossAlive   = false;
            BossCleared = true;
        }

        void RecalcWeight()
        {
            _totalWeight = 0f;
            foreach (var info in enemySpawnInfos)
                _totalWeight += info.spawnWeight;
        }

        void Update()
        {
            // Keep retrying until we find a target
            if (target == null)
            {
                var sc = FindFirstObjectByType<RocketShip.ShipController>();
                if (sc != null) target = sc.transform;
                return;
            }

            if (spawnPaused) return;

            _timer += Time.deltaTime;
            if (_timer >= spawnInterval)
            {
                _timer = 0f;
                for (int b = 0; b < spawnBatchSize; b++)
                    TrySpawn();
            }
        }

        void TrySpawn()
        {
            if (_totalWeight <= 0f || enemySpawnInfos.Count == 0) return;

            // Pick a random type by weight
            float roll = UnityEngine.Random.Range(0f, _totalWeight);
            float acc  = 0f;
            int   idx  = 0;

            for (int i = 0; i < enemySpawnInfos.Count; i++)
            {
                acc += enemySpawnInfos[i].spawnWeight;
                if (roll <= acc) { idx = i; break; }
            }

            var info = enemySpawnInfos[idx];
            if (info.prefab == null) return;
            if (_spawnCounts[idx] >= info.spawnLimit) return;

            // Boss entry: never re-spawn once cleared, and don't double-spawn while alive.
            if (info.isBoss && BossCleared) return;
            if (info.isBoss && BossAlive)   return;

            // Random position in the radius band around target
            float   angle    = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float   dist     = UnityEngine.Random.Range(spawnRadiusBand.x, spawnRadiusBand.y);
            Vector2 spawnPos = (Vector2)target.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

            GameObject go = Instantiate(info.prefab,
                                        new Vector3(spawnPos.x, spawnPos.y, 0f),
                                        Quaternion.identity);

            // Wire target and world
            var enemy = go.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.target = target;
                if (world != null) enemy.world = world;
            }

            // Track count — decrease when the enemy dies
            _spawnCounts[idx]++;
            int capturedIdx = idx;
            var hp = go.GetComponent<Health>();
            if (hp != null)
                hp.OnDeath += () => _spawnCounts[capturedIdx]--;

            if (info.isBoss) BossAlive = true;

            PopupTextSpawner.Instance?.Show("Enemy!", spawnPos + Vector2.up, Color.yellow);
        }
    }
}
