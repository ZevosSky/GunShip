// BossController.cs
// Main boss body. Has its own Health (core). Two attached BossParts are damageable
// independently; when the core dies the parts detach and their independent AI activates.
//
// Attacks (randomly selected each cycle):
//   Fan Volley    — each gun muzzle fires a sequential fan of bullets with per-shot delay
//   Missile Salvo — each missile muzzle fires one slow homing missile

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameManager;

namespace Enemies
{
    public class BossController : MonoBehaviour
    {
        // ── Parts ──────────────────────────────────────────────────────────
        [Header("Boss Parts (damageable, detach on core death)")]
        [SerializeField] private BossPart partA;   // melee / orbital
        [SerializeField] private BossPart partB;   // ranged / shooter

        // The main body's muzzles live directly on this object
        [Header("Main Body Muzzle Mounts")]
        [Tooltip("Gun muzzle Transforms on the main body (for Fan Volley)")]
        [SerializeField] private Transform[] bodyGunMuzzles    = new Transform[0];
        [Tooltip("Missile muzzle Transforms on the main body (for Missile Salvo)")]
        [SerializeField] private Transform[] bodyMissileMuzzles = new Transform[0];

        // ── Projectile prefabs ─────────────────────────────────────────────
        [Header("Projectile Prefabs")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject missilePrefab;

        // ── Fan Volley ─────────────────────────────────────────────────────
        [Header("Fan Volley")]
        [Tooltip("Number of projectiles fired per gun muzzle")]
        [SerializeField] private int   fanShotCount   = 5;
        [Tooltip("Total spread angle of the fan in degrees")]
        [SerializeField] private float fanAngle       = 60f;
        [Tooltip("Delay between each successive shot within one gun's fan")]
        [SerializeField] private float fanShotDelay   = 0.1f;
        [SerializeField] private float fanDamage      = 8f;
        [SerializeField] private float fanKnockback   = 2f;

        // ── Missile Salvo ──────────────────────────────────────────────────
        [Header("Missile Salvo")]
        [Tooltip("Damage per missile")]
        [SerializeField] private float missileDamage   = 25f;
        [Tooltip("Override missile lifetime (homing duration). 0 = use prefab default")]
        [SerializeField] private float missileLifetime = 8f;
        [Tooltip("Override missile turn speed. 0 = use prefab default")]
        [SerializeField] private float missileTurnSpeed = 120f;
        [Tooltip("Override missile initial speed. 0 = use prefab default")]
        [SerializeField] private float missileInitialSpeed = 4f;

        // ── Movement ───────────────────────────────────────────────────────
        [Header("Movement")]
        [SerializeField] private float moveSpeed     = 2f;
        [SerializeField] private float preferredDist = 18f;

        // ── Attack cycle ───────────────────────────────────────────────────
        [Header("Attack Cycle")]
        [SerializeField] private float pauseBetweenAttacks = 2.5f;

        // ── Runtime ────────────────────────────────────────────────────────
        private Transform   _target;
        private Rigidbody2D _rb;
        private Health      _coreHealth;
        private bool        _dead;

        // ══════════════════════════════════════════════════════════════════
        void Start()
        {
            _rb         = GetComponent<Rigidbody2D>();
            _coreHealth = GetComponent<Health>();

            var sc = FindFirstObjectByType<RocketShip.ShipController>();
            if (sc != null) _target = sc.transform;

            if (_coreHealth != null)
                _coreHealth.OnDeath += OnCoreDestroyed;

            PopupTextSpawner.Instance?.Show("BOSS APPEARED!", transform.position + Vector3.up * 3f,
                                            new Color(1f, 0.2f, 0.2f));
            StartCoroutine(AttackLoop());
        }

        void FixedUpdate()
        {
            if (_dead || _target == null || _rb == null) return;

            Vector2 toTarget   = (Vector2)(_target.position - transform.position);
            float   dist       = toTarget.magnitude;
            Vector2 desiredVel = dist > preferredDist
                ? toTarget.normalized * moveSpeed
                : -toTarget.normalized * (moveSpeed * 0.5f);

            _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, desiredVel, 0.1f);

            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.Euler(0f, 0f, angle), 60f * Time.fixedDeltaTime);
        }

        // ── Attack loop ────────────────────────────────────────────────────
        IEnumerator AttackLoop()
        {
            while (!_dead)
            {
                yield return new WaitForSeconds(pauseBetweenAttacks);
                if (_dead) yield break;

                // Build available attacks based on whether muzzles exist
                var available = new List<IEnumerator>();
                if (bodyGunMuzzles.Length    > 0 && bulletPrefab  != null) available.Add(FanVolley());
                if (bodyMissileMuzzles.Length > 0 && missilePrefab != null) available.Add(MissileSalvo());

                if (available.Count == 0) yield break;
                yield return StartCoroutine(available[Random.Range(0, available.Count)]);
            }
        }

        // ── Attack 1: Fan Volley ───────────────────────────────────────────
        // Each gun muzzle fires fanShotCount shots spread across fanAngle degrees,
        // relative to the muzzle's own facing direction (rotates with the boss).
        IEnumerator FanVolley()
        {
            foreach (var muzzle in bodyGunMuzzles)
            {
                if (muzzle == null) continue;

                // Base angle from the muzzle's own up-axis (rotates with the boss)
                float baseAngle = Mathf.Atan2(muzzle.up.y, muzzle.up.x) * Mathf.Rad2Deg - 90f;

                for (int i = 0; i < fanShotCount; i++)
                {
                    float t      = fanShotCount == 1 ? 0f : (float)i / (fanShotCount - 1);
                    float spread = Mathf.Lerp(-fanAngle * 0.5f, fanAngle * 0.5f, t);
                    float angle  = baseAngle + spread;

                    var go = Object.Instantiate(bulletPrefab, muzzle.position, Quaternion.Euler(0f, 0f, angle));
                    if (go.TryGetComponent(out Projectiles.DamageDealer dd))
                    {
                        dd.isPlayerProjectile = false;
                        dd.SetDamage(fanDamage, fanKnockback);
                    }

                    yield return new WaitForSeconds(fanShotDelay);
                }
            }
        }

        // ── Attack 2: Missile Salvo ────────────────────────────────────────
        // Each missile muzzle fires one slow homing missile.
        IEnumerator MissileSalvo()
        {
            foreach (var muzzle in bodyMissileMuzzles)
            {
                if (muzzle == null) continue;

                var go = Object.Instantiate(missilePrefab, muzzle.position, muzzle.rotation);
                if (go.TryGetComponent(out Weapons.MissileProjectile mp))
                {
                    mp.isPlayerMissile = false;
                    mp.damage          = missileDamage;
                    if (missileLifetime    > 0f) mp.lifetime      = missileLifetime;
                    if (missileTurnSpeed   > 0f) mp.turnSpeed     = missileTurnSpeed;
                    if (missileInitialSpeed > 0f) mp.initialSpeed = missileInitialSpeed;
                    mp.isHoming = true;
                }

                yield return new WaitForSeconds(0.25f); // stagger per launcher
            }
        }

        // ── Core death → detach parts ──────────────────────────────────────
        void OnCoreDestroyed()
        {
            _dead = true;
            StopAllCoroutines();

            // Detach and activate each surviving part's independent AI
            DetachPart(partA);
            DetachPart(partB);

            PopupTextSpawner.Instance?.Show("BOSS CORE DESTROYED!", transform.position + Vector3.up * 2f,
                                            new Color(1f, 0.9f, 0f));

            // Destroy only the main body — parts are now independent
            Destroy(gameObject);
        }

        // ── Gizmos ────────────────────────────────────────────────────────
        void OnDrawGizmosSelected()
        {
            DrawMuzzleGizmos(bodyGunMuzzles,     new Color(1f, 0.8f, 0f));
            DrawMuzzleGizmos(bodyMissileMuzzles, new Color(1f, 0.3f, 0.1f));
        }

        static void DrawMuzzleGizmos(Transform[] muzzles, Color col)
        {
            if (muzzles == null) return;
            Gizmos.color = col;
            foreach (var m in muzzles)
            {
                if (m == null) continue;
                Gizmos.DrawSphere(m.position, 0.15f);
                Gizmos.DrawLine(m.position, m.position + m.up * 1.2f);
            }
        }

        static void DetachPart(BossPart part)
        {
            if (part == null || !part.IsAlive) return;
            part.transform.SetParent(null);
            var melee   = part.GetComponent<MeleePartAI>();
            var shooter = part.GetComponent<ShooterPartAI>();
            if (melee   != null) melee.enabled   = true;
            if (shooter != null) shooter.enabled = true;
        }
    }
}
