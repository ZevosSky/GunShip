// ExplosionRing.cs
// Attach to a prefab with a SpriteRenderer using a circle sprite.
// The ring expands to a target size then fades out, and can deal damage.

using Enemies;
using RocketShip;
using UnityEngine;

namespace Utils
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ExplosionRing : MonoBehaviour
    {
        [Header("Size")]
        [SerializeField] private float targetRadius = 3f;
        [SerializeField] private float expandDuration = 0.2f;

        [Header("Fade")]
        [SerializeField] private float fadeDuration = 0.3f;

        [Header("Color")]
        [SerializeField] private Color ringColor = new Color(1f, 0.6f, 0.1f, 1f);

        [Header("Damage")]
        [SerializeField] private bool damageEnemies = true;
        [SerializeField] private bool damagePlayer = false;
        [SerializeField] private float damageAmount = 25f;

        private SpriteRenderer _sr;
        private float _elapsed;
        private enum Phase { Expanding, Fading, Done }
        private Phase _phase = Phase.Expanding;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _sr.color = ringColor;
            transform.localScale = Vector3.zero;
        }

        void Start()
        {
            if (damageAmount > 0f)
                ApplyDamage();
        }

        void ApplyDamage()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, targetRadius);
            foreach (var col in hits)
            {
                if (damageEnemies)
                {
                    var enemyHealth = col.GetComponentInParent<Health>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(damageAmount);
                        continue;
                    }
                }

                if (damagePlayer)
                {
                    var shipHealth = col.GetComponentInParent<ShipHealth>();
                    if (shipHealth != null)
                        shipHealth.TakeDamage(damageAmount);
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, targetRadius);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.1f);
            Gizmos.DrawSphere(transform.position, targetRadius);
        }

        void Update()
        {
            _elapsed += Time.deltaTime;

            switch (_phase)
            {
                case Phase.Expanding:
                    {
                        float t = expandDuration > 0f ? Mathf.Clamp01(_elapsed / expandDuration) : 1f;
                        float scale = Mathf.Lerp(0f, targetRadius * 2f, t);
                        transform.localScale = new Vector3(scale, scale, 1f);

                        if (t >= 1f)
                        {
                            _elapsed = 0f;
                            _phase = Phase.Fading;
                        }
                        break;
                    }

                case Phase.Fading:
                    {
                        float t = fadeDuration > 0f ? Mathf.Clamp01(_elapsed / fadeDuration) : 1f;
                        Color c = ringColor;
                        c.a = Mathf.Lerp(1f, 0f, t);
                        _sr.color = c;

                        if (t >= 1f)
                        {
                            _phase = Phase.Done;
                            Destroy(gameObject);
                        }
                        break;
                    }
            }
        }
    }
}

