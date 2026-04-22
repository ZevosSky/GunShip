// ShipAutoPilot.cs — Toggleable AI pilot for the player ship.
// Press T to toggle on/off.
//
// Behaviour:
//   • Scans all active enemies every FixedUpdate.
//   • Each enemy type has its own flee radius:
//       - BossController  → bossFleeRadius   (default 16)
//       - EnemyBase       → basicFleeRadius  (default 6)
//   • If ANY enemy is inside its flee radius the ship steers away from the
//     blended repulsion vector and thrusts at full power.
//   • Otherwise the ship steers toward the nearest enemy and thrusts.
//   • Gun  (Primary)  fires continuously while the ship is roughly facing its target.
//   • Missiles (Secondary) fire on a cooldown while any enemy is in missile range.

using UnityEngine;
using Weapons;
using World;

namespace RocketShip
{
    [RequireComponent(typeof(ShipController))]
    public class ShipAutoPilot : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────
        [Header("World")]
        [SerializeField] public TorusWorld world;

        [Header("Avoidance Radii")]
        [Tooltip("Flee radius for basic enemies (EnemyBase).")]
        [SerializeField] private float basicFleeRadius = 6f;
        [Tooltip("Flee radius for the boss (BossController).")]
        [SerializeField] private float bossFleeRadius  = 16f;

        [Header("Attack")]
        [Tooltip("Dot-product threshold for the gun to fire (1 = dead-on, 0 = 90°).")]
        [SerializeField] [Range(0f, 1f)] private float gunAimThreshold   = 0.85f;
        [Tooltip("Enemies within this distance trigger a missile launch.")]
        [SerializeField] private float missileRange    = 22f;
        [Tooltip("Minimum seconds between missile salvos.")]
        [SerializeField] private float missileInterval = 2f;

        // ── Public state ──────────────────────────────────────────────────
        public bool IsActive
        {
            get => _active;
            set
            {
                if (_active == value) return;
                _active = value;
                if (!_active) Deactivate();
            }
        }

        // ── Private ───────────────────────────────────────────────────────
        private ShipController _ship;
        private bool           _active;
        private float          _missileTimer;
        private bool           _gunDown;

        void Awake()
        {
            _ship = GetComponent<ShipController>();
        }

        // ── Toggle (called by ShipController polling T key) ───────────────
        public void Toggle()
        {
            IsActive = !_active;
            string msg   = _active ? "AUTOPILOT ON"  : "AUTOPILOT OFF";
            Color  color = _active ? new Color(0.3f, 1f, 0.4f) : new Color(0.8f, 0.8f, 0.8f);
            GameManager.PopupTextSpawner.Instance?.Show(msg, transform.position + Vector3.up * 1.5f, color);
        }

        void FixedUpdate()
        {
            if (!_active) return;

            float dt = Time.fixedDeltaTime;

            // ── Gather enemies ────────────────────────────────────────────
            var basicEnemies = FindObjectsByType<Enemies.EnemyBase>  (FindObjectsSortMode.None);
            var bosses       = FindObjectsByType<Enemies.BossController>(FindObjectsSortMode.None);

            Vector2 myPos = transform.position;

            // ── Compute blended repulsion ─────────────────────────────────
            Vector2 repulsion   = Vector2.zero;
            int     repelCount  = 0;

            foreach (var e in basicEnemies)
            {
                if (e == null) continue;
                Vector2 delta = ToTarget(myPos, e.transform.position);
                if (delta.magnitude < basicFleeRadius)
                {
                    // Push away — weight by how close (closer = stronger push)
                    float w = 1f - delta.magnitude / basicFleeRadius;
                    repulsion  -= delta.normalized * w;
                    repelCount++;
                }
            }
            foreach (var b in bosses)
            {
                if (b == null) continue;
                Vector2 delta = ToTarget(myPos, b.transform.position);
                if (delta.magnitude < bossFleeRadius)
                {
                    float w = 1f - delta.magnitude / bossFleeRadius;
                    repulsion  -= delta.normalized * w;
                    repelCount++;
                }
            }

            // ── Decide steering goal ──────────────────────────────────────
            Vector2 steerDir;
            bool    fleeing = repelCount > 0 && repulsion.sqrMagnitude > 0.001f;

            if (fleeing)
            {
                steerDir = repulsion.normalized;
            }
            else
            {
                // Find nearest enemy across both lists
                steerDir = NearestEnemyDir(myPos, basicEnemies, bosses);
            }

            // ── Apply movement inputs ─────────────────────────────────────
            if (steerDir.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(steerDir.y, steerDir.x) * Mathf.Rad2Deg - 90f;
                float currentAngle = transform.eulerAngles.z;
                float angleDiff   = Mathf.DeltaAngle(currentAngle, targetAngle);

                // Rotate: +1 = counter-clockwise, -1 = clockwise (matches ShipController convention)
                float rotateInput = Mathf.Clamp(angleDiff / 45f, -1f, 1f);
                _ship.SetAutoInput(1f, rotateInput);   // always thrust forward
            }
            else
            {
                _ship.SetAutoInput(0f, 0f);
            }

            // ── Missile timer ─────────────────────────────────────────────
            if (_missileTimer > 0f) _missileTimer -= dt;

            // ── Weapon logic (Update-rate calls via helper) ───────────────
            // (weapon targeting runs in Update for responsiveness)
        }

        void Update()
        {
            if (!_active) return;

            // ── Gun ───────────────────────────────────────────────────────
            Vector2 myPos   = transform.position;
            Vector2 forward = transform.up;   // ship faces up in local space

            var basicEnemies = FindObjectsByType<Enemies.EnemyBase>    (FindObjectsSortMode.None);
            var bosses       = FindObjectsByType<Enemies.BossController>(FindObjectsSortMode.None);

            Vector2 nearestDir = NearestEnemyDir(myPos, basicEnemies, bosses);

            bool shouldShoot = nearestDir.sqrMagnitude > 0.001f &&
                               Vector2.Dot(forward, nearestDir.normalized) >= gunAimThreshold;

            if (shouldShoot && !_gunDown)
            {
                _ship.TriggerDownAll(WeaponRole.Primary);
                _gunDown = true;
            }
            else if (!shouldShoot && _gunDown)
            {
                _ship.TriggerUpAll(WeaponRole.Primary);
                _gunDown = false;
            }

            // ── Missiles ──────────────────────────────────────────────────
            if (_missileTimer <= 0f)
            {
                bool enemyInRange = false;
                foreach (var e in basicEnemies)
                    if (e != null && ToTarget(myPos, e.transform.position).magnitude <= missileRange)
                    { enemyInRange = true; break; }

                if (!enemyInRange)
                    foreach (var b in bosses)
                        if (b != null && ToTarget(myPos, b.transform.position).magnitude <= missileRange)
                        { enemyInRange = true; break; }

                if (enemyInRange)
                {
                    _ship.FireOnceAll(WeaponRole.Secondary);
                    _missileTimer = missileInterval;
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        // Torus-correct vector from myPos to targetPos
        private Vector2 ToTarget(Vector2 from, Vector2 to)
        {
            return world != null ? world.ShortestDelta(from, to) : (to - from);
        }

        private Vector2 NearestEnemyDir(Vector2 myPos,
                                         Enemies.EnemyBase[]       basicEnemies,
                                         Enemies.BossController[]  bosses)
        {
            float   bestSqr = float.MaxValue;
            Vector2 bestDir = Vector2.zero;

            foreach (var e in basicEnemies)
            {
                if (e == null) continue;
                Vector2 d = ToTarget(myPos, e.transform.position);
                if (d.sqrMagnitude < bestSqr) { bestSqr = d.sqrMagnitude; bestDir = d; }
            }
            foreach (var b in bosses)
            {
                if (b == null) continue;
                Vector2 d = ToTarget(myPos, b.transform.position);
                if (d.sqrMagnitude < bestSqr) { bestSqr = d.sqrMagnitude; bestDir = d; }
            }

            return bestDir;
        }

        private void Deactivate()
        {
            if (_gunDown)
            {
                _ship.TriggerUpAll(WeaponRole.Primary);
                _gunDown = false;
            }
            _ship.SetAutoInput(0f, 0f);
        }

        // ── Gizmos ────────────────────────────────────────────────────────
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // Basic enemy flee radius — yellow
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.35f);
            DrawCircle(transform.position, basicFleeRadius);

            // Boss flee radius — red
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
            DrawCircle(transform.position, bossFleeRadius);

            // Missile range — cyan
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.20f);
            DrawCircle(transform.position, missileRange);
        }

        private static void DrawCircle(Vector3 center, float radius, int steps = 36)
        {
            float step = 360f / steps;
            for (int i = 0; i < steps; i++)
            {
                float a0 = i * step * Mathf.Deg2Rad;
                float a1 = (i + 1) * step * Mathf.Deg2Rad;
                Gizmos.DrawLine(
                    center + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * radius,
                    center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * radius);
            }
        }
#endif
    }
}

