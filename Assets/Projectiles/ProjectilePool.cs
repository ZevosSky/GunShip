
// Projectile System: Simple Object Pooling System for projectiles to avoid instantiation



namespace Projectiles
{
    
    using System.Collections.Generic;
    using UnityEngine;

    namespace Projectiles
    {
        public class ProjectilePool : MonoBehaviour
        {
            public static ProjectilePool Instance { get; private set; }

            #region Inspector

            [Tooltip("How many instances to pre-warm per prefab on first request.")] [SerializeField] [Range(10, 500)]
            private int prewarmPerPrefab = 50;
            
            
            [Tooltip("Optional List of Prefabs to Prewarm at start")] [SerializeField]
            private List<GameObject> prefabsToPrewarm = new List<GameObject>();
            

            #endregion


            #region Private

            // Key   = prefab GameObject (identity, not a scene instance)
            // Value = queue of inactive pooled instances
            private readonly Dictionary<GameObject, Queue<Projectile>> _pools =
                new Dictionary<GameObject, Queue<Projectile>>();

            // Reverse map so Return() can find the right queue in O(1)
            private readonly Dictionary<Projectile, GameObject> _ownerPrefab =
                new Dictionary<Projectile, GameObject>();

            #endregion


            #region Unity Functions

            private void Awake()
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                Instance = this;
            }

            private void Start()
            {
                for (int i = 0; i < prefabsToPrewarm.Count; ++i)
                {
                    Prewarm(prefabsToPrewarm[i], prewarmPerPrefab);
                }
            }
            
            #endregion


            #region Public API

            /// <summary>
            /// Retrieve an inactive projectile that matches <paramref name="prefab"/>.
            /// Spawns a new instance if the pool is empty.
            /// </summary>
            public Projectile Get(GameObject prefab, Vector3 position, Quaternion rotation)
            {
                Queue<Projectile> queue = GetOrCreateQueue(prefab);

                Projectile projectile;
                if (queue.Count > 0)
                {
                    projectile = queue.Dequeue();
                    projectile.transform.SetPositionAndRotation(position, rotation);
                }
                else
                {
                    projectile = CreateInstance(prefab, position, rotation);
                }

                projectile.gameObject.SetActive(true);
                return projectile;
            }

            /// <summary>
            /// Return a projectile to its pool. Call this instead of Destroy.
            /// </summary>
            public void Return(Projectile projectile)
            {
                projectile.gameObject.SetActive(false);
                projectile.transform.SetParent(transform); // keep hierarchy tidy

                if (_ownerPrefab.TryGetValue(projectile, out GameObject prefab))
                    _pools[prefab].Enqueue(projectile);
                else
                    Destroy(projectile.gameObject); // safety: wasn't spawned by this pool
            }

            /// <summary>
            /// Pre-warm the pool for a given prefab so the first shots have no
            /// instantiation cost. Call this from weapon Awake/Start.
            /// </summary>
            public void Prewarm(GameObject prefab, int count = -1)
            {
                int amount = count > 0 ? count : prewarmPerPrefab;
                Queue<Projectile> queue = GetOrCreateQueue(prefab);

                for (int i = 0; i < amount; i++)
                {
                    Projectile p = CreateInstance(prefab, Vector3.zero, Quaternion.identity);
                    p.gameObject.SetActive(false);
                    queue.Enqueue(p);
                }
            }

            #endregion


            #region Helpers

            private Queue<Projectile> GetOrCreateQueue(GameObject prefab)
            {
                
                if (!_pools.TryGetValue(prefab, out Queue<Projectile> queue))
                {
                    queue = new Queue<Projectile>();
                    _pools[prefab] = queue;
                }

                return queue;
            }

            private Projectile CreateInstance(GameObject prefab, Vector3 position, Quaternion rotation)
            {
                GameObject go = Instantiate(prefab, position, rotation, transform);
                Projectile p = go.GetComponent<Projectile>();
                _ownerPrefab[p] = prefab; // register reverse lookup
                return p;
            }

            #endregion
        }
    }
}