// EnemyBase.cs — basic chasing enemy.
// Requires a Health component. EnemySpawner sets 'target' before activation.

using UnityEngine;
using World;

namespace Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class EnemyBase : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] protected float moveSpeed  = 5f;
        [SerializeField] protected float turnSpeed  = 240f;
        // stopRadius removed — physics collision is the natural stop condition,
        // which ensures damage radius matches the Circle Collider 2D exactly.

        [Header("Contact Damage")]
        [SerializeField] protected float contactDamage     = 10f;
        [SerializeField] protected float contactCooldown   = 0.5f;
        [SerializeField] protected float knockbackStrength = 8f;   // impulse pushed into ship's custom physics

        [Header("World")]
        [SerializeField] public TorusWorld world;

        [HideInInspector] public Transform target;

        protected Rigidbody2D _rb;
        protected Health      _health;
        private   float       _contactTimer;

        protected virtual void Awake()
        {
            _rb     = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
        }

        protected virtual void Start()
        {
            // Find world if not assigned
            if (world == null)
            {
                var wrap = FindFirstObjectByType<TorusWrap>();
                if (wrap != null) world = wrap.world;
            }

            if (target == null)
            {
                var sc = FindFirstObjectByType<RocketShip.ShipController>();
                if (sc != null) target = sc.transform;
            }
        }

        protected virtual void FixedUpdate()
        {
            if (target == null) return;

            // ── Torus-correct direction ──────────────────────────────────
            Vector2 toTarget = world != null
                ? world.ShortestDelta(transform.position, target.position)
                : (Vector2)(target.position - transform.position);

            float dist = toTarget.magnitude;

            // Always move toward target — physics collision stops us naturally
            if (dist > 0.01f)
            {
                float desiredAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.Euler(0f, 0f, desiredAngle),
                    turnSpeed * Time.fixedDeltaTime);

                _rb.linearVelocity = transform.up * moveSpeed;
            }

            // ── Self-wrap position on torus — only teleport when actually crossing a boundary ──
            if (world != null)
            {
                Vector2 wrapped = world.Wrap(transform.position);
                if ((wrapped - (Vector2)transform.position).sqrMagnitude > 0.001f)
                    _rb.MovePosition(wrapped);
            }

            // ── Contact damage cooldown tick ─────────────────────────────
            if (_contactTimer > 0f) _contactTimer -= Time.fixedDeltaTime;
        }

        // ── Contact damage — solid collision only; trigger ignored
        //    (ship uses a large trigger for render culling, not as a hitbox)
        protected virtual void OnCollisionEnter2D(Collision2D col) => TryContactDamage(col.gameObject);

        private void TryContactDamage(GameObject other)
        {
            if (_contactTimer > 0f) return;
            // Search up the hierarchy in case the collider is on a child object
            var sh = other.GetComponentInParent<RocketShip.ShipHealth>();
            if (sh == null) return;

            sh.TakeDamage(contactDamage);
            _contactTimer = contactCooldown;

            // Push ship away through its custom physics
            var sc = other.GetComponentInParent<RocketShip.ShipController>();
            if (sc != null)
            {
                Vector2 dir = ((Vector2)(other.transform.position - transform.position)).normalized;
                sc.ApplyKnockback(dir * knockbackStrength);
            }

            // Bounce enemy back
            _rb.linearVelocity = -_rb.linearVelocity * 0.5f;
        }
    }
}
