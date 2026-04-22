// MissileProjectile.cs
// Phase 1 (Drift):    coast on initial velocity, no steering.
// Phase 2 (Reorient): rotate toward target via torus-shortest delta, no thrust.
// Phase 3 (Boost):    accelerate hard along forward, limited-turn homing via torus delta.
// All direction/distance math goes through TorusWorld.ShortestDelta to stay correct at seams.

using UnityEngine;
using System.Collections;
using Enemies;
using World;

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
        public float targetLockDelay = 0.25f; // legacy: used when isHoming = false
        public float lifetime        = 8f;

        [Header("Homing Phases (isHoming = true)")]
        public bool  isHoming            = false;
        public float driftDuration       = 0.6f;
        [Tooltip("Drag applied during Drift — bleeds the missile to a near-stop")]
        public float driftDrag           = 4f;
        public float reorientDuration    = 0.4f;
        [Tooltip("Instant impulse at the moment Boost fires — creates the snap-forward feel")]
        public float boostImpulse        = 60f;
        [Tooltip("Continuous force per FixedUpdate during Boost")]
        public float boostForce          = 45f;
        [Tooltip("Speed cap during Boost — set much higher than maxSpeed so the burst isn't killed immediately")]
        public float boostMaxSpeed       = 120f;
        public float boostDuration       = 1.5f;
        [System.Obsolete("No longer used — boost direction is locked and drag is zeroed at phase entry.")]
        public float boostTurnSpeed      = 15f; // retained for serialization compat only

        [Header("Damage")]
        [Tooltip("Direct impact damage dealt to whatever the missile physically hits")]
        public float damage          = 30f;
        public bool  isPlayerMissile = true;

        [Header("VFX")]
        public GameObject explosionPrefab;

        [Header("World")]
        [Tooltip("Assign the TorusWorld asset so homing math wraps correctly at seams")]
        public TorusWorld world;

        // ── Private state ────────────────────────────────────────────────────────
        private enum Phase { Drift, Reorient, Boost, Legacy }

        private Rigidbody2D _rb;
        private Transform   _target;
        private float       _timer;
        private bool        _hit;
        private Phase       _phase;
        private Vector2     _boostDir; // direction locked in at start of Boost — never changes

        // ── Unity ────────────────────────────────────────────────────────────────
        void Awake() => _rb = GetComponent<Rigidbody2D>();

        void Start()
        {
            _rb.linearVelocity  = transform.up * initialSpeed;
            _rb.linearDamping   = 0f; // start clean; Drift will apply its own drag
            _phase = isHoming ? Phase.Drift : Phase.Legacy;
            StartCoroutine(LifetimeTimeout());
        }

        void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;

            if (isHoming)
                TickHoming();
            else
                TickLegacy();
        }

        // ── Homing 3-phase state machine ─────────────────────────────────────────
        void TickHoming()
        {
            switch (_phase)
            {
                case Phase.Drift:
                    // Apply drag to bleed the missile down to nearly a stop.
                    _rb.linearDamping = driftDrag;

                    if (_timer >= driftDuration)
                    {
                        _timer  = 0f;
                        _phase  = Phase.Reorient;
                        _target = FindNearest();
                        // Kill remaining velocity so reorient starts from near-zero.
                        _rb.linearVelocity = Vector2.zero;
                        _rb.linearDamping  = 0f;
                    }
                    break;

                case Phase.Reorient:
                    // Rotate to face locked target — missile is nearly stationary.
                    if (_target != null)
                        RotateTowardTarget(turnSpeed);
                    else
                        _timer = reorientDuration; // no target — skip straight to boost

                    if (_timer >= reorientDuration)
                    {
                        _timer    = 0f;
                        _phase    = Phase.Boost;
                        _boostDir = transform.up; // lock direction once, forever

                        // Snap impulse — goes from still to fast instantly.
                        _rb.linearDamping = 0f;
                        _rb.AddForce(_boostDir * boostImpulse, ForceMode2D.Impulse);
                    }
                    break;

                case Phase.Boost:
                    // Continuous acceleration along the locked direction — no rotation, no drag.
                    _rb.AddForce(_boostDir * boostForce, ForceMode2D.Force);
                    // Use boostMaxSpeed — much higher than the drift cap so the burst isn't killed.
                    if (_rb.linearVelocity.sqrMagnitude > boostMaxSpeed * boostMaxSpeed)
                        _rb.linearVelocity = _rb.linearVelocity.normalized * boostMaxSpeed;

                    if (_timer >= boostDuration)
                        ExplodeAt(transform.position);
                    break;
            }
        }

        // ── Legacy 2-phase (original behaviour) ──────────────────────────────────
        void TickLegacy()
        {
            if (_timer < targetLockDelay) return;

            if (_target == null) _target = FindNearest();
            if (_target == null) return;

            RotateTowardTarget(turnSpeed);
            _rb.AddForce(transform.up * jerkForce, ForceMode2D.Force);
            ClampSpeed();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// Rotate toward _target using torus-shortest delta at the given deg/s rate.
        void RotateTowardTarget(float degPerSec)
        {
            Vector2 delta     = TorusDelta((Vector2)transform.position, (Vector2)_target.position);
            float   wantAngle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0f, 0f, wantAngle),
                degPerSec * Time.fixedDeltaTime);
        }

        void ClampSpeed()
        {
            if (_rb.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
                _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
        }

        /// Returns the shortest displacement from→to, using TorusWorld if assigned.
        Vector2 TorusDelta(Vector2 from, Vector2 to)
        {
            return world != null
                ? world.ShortestDelta(from, to)
                : (to - from);
        }

        /// Find nearest valid target using torus-aware distance.
        Transform FindNearest()
        {
            Transform best     = null;
            float     bestSq   = float.MaxValue;
            foreach (var h in Health.AllEnemies)
            {
                if (h == null) continue;
                float sq = TorusDelta(transform.position, h.transform.position).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = h.transform; }
            }
            return best;
        }

        // ── Collision / Explosion ─────────────────────────────────────────────────
        void OnTriggerEnter2D(Collider2D col)
        {
            if (_hit) return;
            // Don't arm during Drift — the missile is still leaving the ship.
            if (_phase == Phase.Drift) return;

            var hitEnemy = col.GetComponentInParent<Health>();
            var hitShip  = col.GetComponentInParent<RocketShip.ShipHealth>();

            if ( isPlayerMissile && hitEnemy == null) return;
            if (!isPlayerMissile && hitShip  == null) return;

            _hit = true;

            // Direct impact damage — one target, full damage.
            if (isPlayerMissile) hitEnemy?.TakeDamage(damage);
            else                 hitShip?.TakeDamage(damage);

            ExplodeAt(transform.position);
        }

        IEnumerator LifetimeTimeout()
        {
            yield return new WaitForSeconds(lifetime);
            if (!_hit) ExplodeAt(transform.position);
        }

        void ExplodeAt(Vector2 pos)
        {
            // Spawn the explosion prefab (e.g. ExplosionRing).
            // Any area damage is configured on that prefab — nothing injected here.
            if (explosionPrefab != null)
                Instantiate(explosionPrefab, pos, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}

