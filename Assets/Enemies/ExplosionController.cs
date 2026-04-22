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
            float blastRadius = GetBlastRadius();
            var buffer = new Collider2D[32];
            int count  = Physics2D.OverlapCircleNonAlloc(transform.position, blastRadius, buffer);
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

        /// Single source of truth for blast radius.
        /// Prefers CircleCollider2D.radius * world scale; falls back to the serialized radius field.
        public float GetBlastRadius()
        {
            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
                return col.radius * scale;
            }
            return radius;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            float blastRadius = GetBlastRadius();
            Vector3 pos = transform.position;

            UnityEditor.Handles.color = new Color(1f, 0.4f, 0f, 0.18f);
            UnityEditor.Handles.DrawSolidDisc(pos, Vector3.forward, blastRadius);
            UnityEditor.Handles.color = new Color(1f, 0.4f, 0f, 0.85f);
            UnityEditor.Handles.DrawWireDisc(pos, Vector3.forward, blastRadius);
            UnityEditor.Handles.Label(pos + Vector3.right * blastRadius,
                $" Blast: {blastRadius:0.##}", UnityEditor.EditorStyles.miniLabel);
        }
#endif
    }
}

