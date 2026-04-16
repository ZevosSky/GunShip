// ExplosionController.cs
// Attach to an explosion prefab. On Start: area damage + screen shake + auto-destroy.

using UnityEngine;
using World;
using RocketShip;

namespace Enemies
{
    public class ExplosionController : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] public float radius   = 4f;
        [SerializeField] public float damage   = 30f;
        [SerializeField] public bool  hurtPlayer  = true;
        [SerializeField] public bool  hurtEnemies;

        [Header("Screen Shake")]
        [SerializeField] private float shakeDuration  = 0.4f;
        [SerializeField] private float shakeMagnitude = 0.6f;

        [Header("Lifetime")]
        [SerializeField] private float destroyAfter = 2f;

        void Start()
        {
            var ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play();

            // Area damage — non-allocating overlap
            var buffer = new Collider2D[32];
            int count  = Physics2D.OverlapCircleNonAlloc(transform.position, radius, buffer);
            for (int i = 0; i < count; i++)
            {
                var c = buffer[i];
                if (hurtEnemies)
                {
                    var h = c.GetComponentInParent<Health>();
                    if (h != null) h.TakeDamage(damage);
                }
                if (hurtPlayer)
                {
                    var sh = c.GetComponentInParent<ShipHealth>();
                    if (sh != null) sh.TakeDamage(damage);
                }
            }

            // Camera shake
            ScreenShake.Instance?.Shake(shakeDuration, shakeMagnitude);

            Destroy(gameObject, destroyAfter);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }
}

