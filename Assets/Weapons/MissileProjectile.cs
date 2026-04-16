// MissileProjectile.cs
// Phase 1: free-flight forward (targetLockDelay seconds)
// Phase 2: jerk/acceleration homing with limited turn rate

using UnityEngine;
using Enemies;

namespace Weapons
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MissileProjectile : MonoBehaviour
    {
        [Header("Flight")]
        public float initialSpeed    = 12f;
        public float jerkForce       = 20f;
        public float maxSpeed        = 35f;
        public float turnSpeed       = 150f;
        public float targetLockDelay = 0.25f;
        public float lifetime        = 8f;

        [Header("Damage")]
        public float damage          = 30f;
        public float explosionRadius = 2.5f;
        public bool  isPlayerMissile = true;

        [Header("VFX")]
        public GameObject explosionPrefab;

        private Rigidbody2D _rb;
        private Transform   _target;
        private float       _timer;
        private bool        _hit;

        void Awake() => _rb = GetComponent<Rigidbody2D>();

        void Start()
        {
            _rb.linearVelocity = transform.up * initialSpeed;
            Destroy(gameObject, lifetime);
        }

        void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;
            if (_timer < targetLockDelay) return;

            if (_target == null) _target = FindNearest();
            if (_target == null) return;

            Vector2 toTarget   = (Vector2)(_target.position - transform.position);
            float   wantAngle  = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0f, 0f, wantAngle),
                turnSpeed * Time.fixedDeltaTime);

            _rb.AddForce(transform.up * jerkForce, ForceMode2D.Force);

            if (_rb.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
                _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            if (_hit) return;

            bool hitEnemy = col.GetComponentInParent<Health>() != null;
            bool hitShip  = col.GetComponentInParent<RocketShip.ShipHealth>() != null;

            if (isPlayerMissile  && !hitEnemy) return;
            if (!isPlayerMissile && !hitShip)  return;

            _hit = true;
            ExplodeAt(transform.position);
        }

        void ExplodeAt(Vector2 pos)
        {
            var buffer = new Collider2D[32];
            int count  = Physics2D.OverlapCircleNonAlloc(pos, explosionRadius, buffer);
            for (int i = 0; i < count; i++)
            {
                var c = buffer[i];
                if (isPlayerMissile)
                {
                    var h = c.GetComponentInParent<Health>();
                    if (h != null) h.TakeDamage(damage);
                }
                else
                {
                    var sh = c.GetComponentInParent<RocketShip.ShipHealth>();
                    if (sh != null) sh.TakeDamage(damage);
                }
            }

            if (explosionPrefab != null)
                Instantiate(explosionPrefab, pos, Quaternion.identity);

            Destroy(gameObject);
        }

        Transform FindNearest()
        {
            Transform best     = null;
            float     bestDist = float.MaxValue;
            foreach (var h in Health.AllEnemies)
            {
                if (h == null) continue;
                float d = ((Vector2)h.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = h.transform; }
            }
            return best;
        }
    }
}

