// BossController.cs
// Multi-part boss with 3 unique attacks firing from different parts.
// Attack 1 (FrontGun):    Burst Barrage  — rapid aimed shots
// Attack 2 (CoreBody):    Spread Ring    — bullets in all directions
// Attack 3 (Launchers):   Missile Volley — homing missiles from sides
//
// Setup in Inspector:
//   frontGun    → child BossPart with a muzzle
//   coreBody    → child BossPart (destroying it = instant boss defeat)
//   launchers[] → 1-2 side BossParts

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameManager;

namespace Enemies
{
    public class BossController : MonoBehaviour
    {
        // ── Parts ─────────────────────────────────────────────────────────
        [Header("Boss Parts")]
        [SerializeField] private BossPart   frontGun;
        [SerializeField] private BossPart   coreBody;     // destroying this = boss dead
        [SerializeField] private BossPart[] launchers;

        // ── Projectile prefabs ────────────────────────────────────────────
        [Header("Projectile Prefabs")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject missilePrefab;

        // ── Attack tuning ─────────────────────────────────────────────────
        [Header("Burst Barrage (FrontGun)")]
        [SerializeField] private int   burstCount      = 6;
        [SerializeField] private float burstInterval   = 0.1f;
        [SerializeField] private float bulletDamage    = 8f;
        [SerializeField] private float bulletKnockback = 2f;

        [Header("Spread Ring (CoreBody)")]
        [SerializeField] private int   ringCount       = 12;
        [SerializeField] private float ringDamage      = 6f;

        [Header("Missile Volley (Launchers)")]
        [SerializeField] private int   salvoSize       = 3;
        [SerializeField] private float missileDamage   = 20f;
        [SerializeField] private float missileKB       = 5f;
        [SerializeField] private float missileSpread   = 30f;  // half-angle

        // ── Movement ──────────────────────────────────────────────────────
        [Header("Movement")]
        [SerializeField] private float moveSpeed    = 2f;
        [SerializeField] private float preferredDist = 18f;    // desired range from player

        // ── Cycle ─────────────────────────────────────────────────────────
        [Header("Attack Cycle")]
        [SerializeField] private float pauseBetweenAttacks = 2f;

        // ── Runtime ───────────────────────────────────────────────────────
        private Transform   _target;
        private Rigidbody2D _rb;
        private bool        _dead;
        private int         _phase;   // 0=all alive, 1=gun lost, 2=launcher lost

        void Start()
        {
            _rb = GetComponent<Rigidbody2D>();

            // Find player
            var sc = FindFirstObjectByType<RocketShip.ShipController>();
            if (sc != null) _target = sc.transform;

            // Subscribe to part deaths
            if (coreBody != null)
                coreBody.OnPartDestroyed += _ => OnCoreDestroyed();
            if (frontGun != null)
                frontGun.OnPartDestroyed += _ => { _phase = Mathf.Max(_phase, 1); };
            foreach (var l in launchers)
                if (l != null) l.OnPartDestroyed += _ => { _phase = Mathf.Max(_phase, 2); };

            PopupTextSpawner.Instance?.Show("BOSS APPEARED!", transform.position + Vector3.up * 3f,
                                            new Color(1f, 0.2f, 0.2f));

            StartCoroutine(AttackLoop());
        }

        void FixedUpdate()
        {
            if (_dead || _target == null || _rb == null) return;

            Vector2 toTarget = (Vector2)(_target.position - transform.position);
            float dist = toTarget.magnitude;

            // Orbit / approach at preferred distance
            Vector2 desiredVel = dist > preferredDist
                ? toTarget.normalized * moveSpeed
                : -toTarget.normalized * (moveSpeed * 0.5f);

            _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, desiredVel, 0.1f);

            // Always face the player
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.Euler(0f, 0f, angle), 60f * Time.fixedDeltaTime);
        }

        // ── Attack loop ───────────────────────────────────────────────────
        IEnumerator AttackLoop()
        {
            while (!_dead)
            {
                yield return new WaitForSeconds(pauseBetweenAttacks);

                // Build list of available attacks
                var available = new List<IEnumerator>();
                if (frontGun  != null && frontGun.IsAlive)  available.Add(BurstBarrage());
                if (coreBody  != null && coreBody.IsAlive)  available.Add(SpreadRing());
                bool hasLauncher = false;
                foreach (var l in launchers) if (l != null && l.IsAlive) { hasLauncher = true; break; }
                if (hasLauncher) available.Add(MissileVolley());

                // Phase 2+ → attacks are faster
                if (_phase >= 2) pauseBetweenAttacks = Mathf.Max(0.8f, pauseBetweenAttacks - 0.1f);

                if (available.Count == 0) yield break;
                int pick = Random.Range(0, available.Count);
                yield return StartCoroutine(available[pick]);
            }
        }

        // ── Attack 1: Burst Barrage ───────────────────────────────────────
        IEnumerator BurstBarrage()
        {
            if (frontGun == null || !frontGun.IsAlive || _target == null) yield break;
            for (int i = 0; i < burstCount; i++)
            {
                frontGun.FireAt(bulletPrefab, _target.position, bulletDamage, bulletKnockback);
                yield return new WaitForSeconds(burstInterval);
            }
        }

        // ── Attack 2: Spread Ring ─────────────────────────────────────────
        IEnumerator SpreadRing()
        {
            if (coreBody == null || !coreBody.IsAlive) yield break;
            float step = 360f / ringCount;
            for (int i = 0; i < ringCount; i++)
                coreBody.FireDir(bulletPrefab, i * step, ringDamage, bulletKnockback);
            yield return null;
        }

        // ── Attack 3: Missile Volley ──────────────────────────────────────
        IEnumerator MissileVolley()
        {
            if (_target == null) yield break;
            foreach (var launcher in launchers)
            {
                if (launcher == null || !launcher.IsAlive) continue;
                float halfSpread = missileSpread;
                for (int i = 0; i < salvoSize; i++)
                {
                    float ang = salvoSize == 1 ? 0f
                        : Mathf.Lerp(-halfSpread, halfSpread, (float)i / (salvoSize - 1));
                    Vector2 dir = (Vector2)(_target.position - launcher.muzzle.position);
                    float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                    launcher.FireDir(missilePrefab, baseAngle + ang, missileDamage, missileKB);
                }
                yield return new WaitForSeconds(0.3f);
            }
        }

        void OnCoreDestroyed()
        {
            _dead = true;
            StopAllCoroutines();

            // Destroy all remaining parts
            if (frontGun != null && frontGun.IsAlive)
                frontGun.PartHealth.TakeDamage(9999f);
            foreach (var l in launchers)
                if (l != null && l.IsAlive) l.PartHealth.TakeDamage(9999f);

            PopupTextSpawner.Instance?.Show("BOSS DEFEATED!", transform.position + Vector3.up * 2f,
                                            new Color(1f, 0.9f, 0f));
            Destroy(gameObject, 0.5f);
        }
    }
}


