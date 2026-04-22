// BossPart.cs
// One of the boss's destructible pieces. Has its own Health.
// Exposes typed muzzle arrays so BossController can fire the right attack from the right mount.

using System;
using UnityEngine;

namespace Enemies
{
    [RequireComponent(typeof(Health))]
    public class BossPart : MonoBehaviour
    {
        [SerializeField] public string partName = "Part";

        [Header("Muzzle Mounts")]
        [Tooltip("Transforms where fan/bullet attacks spawn")]
        [SerializeField] public Transform[] gunMuzzles    = new Transform[0];
        [Tooltip("Transforms where homing missile attacks spawn")]
        [SerializeField] public Transform[] missileMuzzles = new Transform[0];

        public Health PartHealth { get; private set; }
        public bool   IsAlive    => PartHealth != null && !PartHealth.IsDead;

        public event Action<BossPart> OnPartDestroyed;

        void Awake()
        {
            PartHealth = GetComponent<Health>();
            PartHealth.OnDeath += () => OnPartDestroyed?.Invoke(this);
        }

        // ── Fire helpers (called by BossController) ───────────────────────

        /// Spawn a bullet prefab from a specific muzzle aimed at a world position.
        public void FireGunAt(int muzzleIndex, GameObject prefab, Vector2 targetPos, float dmg, float kb)
        {
            if (prefab == null || muzzleIndex >= gunMuzzles.Length) return;
            var muzzle = gunMuzzles[muzzleIndex];
            if (muzzle == null) return;

            Vector2 dir    = (targetPos - (Vector2)muzzle.position).normalized;
            float   angle  = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            var     go     = Instantiate(prefab, muzzle.position, Quaternion.Euler(0f, 0f, angle));
            SetupDamageDealer(go, dmg, kb);
        }

        /// Spawn a bullet prefab from a specific muzzle at an explicit angle offset (for fan spread).
        public void FireGunAngle(int muzzleIndex, GameObject prefab, float angleDeg, float dmg, float kb)
        {
            if (prefab == null || muzzleIndex >= gunMuzzles.Length) return;
            var muzzle = gunMuzzles[muzzleIndex];
            if (muzzle == null) return;

            var go = Instantiate(prefab, muzzle.position, Quaternion.Euler(0f, 0f, angleDeg));
            SetupDamageDealer(go, dmg, kb);
        }

        /// Spawn a missile prefab from a specific missile muzzle aimed at a target.
        public void FireMissileAt(int muzzleIndex, GameObject prefab, float dmg)
        {
            if (prefab == null || muzzleIndex >= missileMuzzles.Length) return;
            var muzzle = missileMuzzles[muzzleIndex];
            if (muzzle == null) return;

            var go = Instantiate(prefab, muzzle.position, muzzle.rotation);
            // Configure as enemy missile
            if (go.TryGetComponent(out Weapons.MissileProjectile mp))
            {
                mp.isPlayerMissile = false;
                mp.damage          = dmg;
            }
        }

        static void SetupDamageDealer(GameObject go, float dmg, float kb)
        {
            if (go.TryGetComponent(out Projectiles.DamageDealer dd))
            {
                dd.isPlayerProjectile = false;
                dd.SetDamage(dmg, kb);
            }
        }

        // ── Gizmos ────────────────────────────────────────────────────────
        void OnDrawGizmosSelected()
        {
            DrawMuzzleGizmos(gunMuzzles,     new Color(1f, 0.8f, 0f));
            DrawMuzzleGizmos(missileMuzzles, new Color(1f, 0.3f, 0.1f));
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
    }
}
