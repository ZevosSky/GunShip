// BossPart.cs
// Individual boss entity (gun, launcher, core) — each with its own Health.
// BossController reads alive state and fires attacks through these parts.

using System;
using UnityEngine;

namespace Enemies
{
    [RequireComponent(typeof(Health))]
    public class BossPart : MonoBehaviour
    {
        [SerializeField] public string partName = "Part";
        [SerializeField] public Transform muzzle;           // where projectiles spawn

        public Health  PartHealth { get; private set; }
        public bool    IsAlive    => PartHealth != null && !PartHealth.IsDead;

        public event Action<BossPart> OnPartDestroyed;

        void Awake()
        {
            PartHealth = GetComponent<Health>();
            PartHealth.OnDeath += HandleDeath;
        }

        void HandleDeath()
        {
            OnPartDestroyed?.Invoke(this);
        }

        /// Fire a single projectile from this part's muzzle toward a target.
        public void FireAt(GameObject prefab, Vector2 targetPos, float dmg, float kb)
        {
            if (prefab == null || muzzle == null) return;

            Vector2 dir   = (targetPos - (Vector2)muzzle.position).normalized;
            float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);

            GameObject go = Instantiate(prefab, muzzle.position, rot);
            if (go.TryGetComponent(out Projectiles.DamageDealer dd))
            {
                dd.isPlayerProjectile = false;
                dd.SetDamage(dmg, kb);
            }
        }

        /// Fire in a specific direction (for ring/spread attacks).
        public void FireDir(GameObject prefab, float angleDeg, float dmg, float kb)
        {
            if (prefab == null || muzzle == null) return;
            Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg);
            GameObject go  = Instantiate(prefab, muzzle.position, rot);
            if (go.TryGetComponent(out Projectiles.DamageDealer dd))
            {
                dd.isPlayerProjectile = false;
                dd.SetDamage(dmg, kb);
            }
        }
    }
}

