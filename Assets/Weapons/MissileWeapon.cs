// MissileWeapon.cs
// ENTER fires a full spread burst, then enters a cooldown before you can fire again.
// Spread size scales with ship size via WeaponData.maxSalvoSize.

using System.Collections;
using UnityEngine;
using Projectiles;

namespace Weapons
{
    public class MissileWeapon : WeaponBase
    {
        // Inspector — override defaults from WeaponData if needed
        [Header("Burst / Cooldown")]
        [SerializeField] private float cooldown         = 4f;   // seconds between bursts
        [SerializeField] private float burstFireDelay   = 0.08f; // seconds between each missile in burst

        private bool  _onCooldown;
        private float _cooldownTimer;

        // ── Tick: count down cooldown ──────────────────────────────────────
        public override void Tick(float dt)
        {
            if (_onCooldown)
            {
                _cooldownTimer -= dt;
                if (_cooldownTimer <= 0f)
                    _onCooldown = false;
            }
        }

        // ── OnTriggerDown: called once per ENTER press via WeaponMount.FireOnce ──
        protected override void OnTriggerDown()
        {
            if (data == null || data.projectilePrefab == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[MissileWeapon] No projectile prefab assigned!");
#endif
                return;
            }

            if (_onCooldown) return;

            // Start the burst coroutine
            StartCoroutine(FireBurst());
        }

        IEnumerator FireBurst()
        {
            _onCooldown    = true;
            _cooldownTimer = cooldown;

            int   count      = Mathf.Max(1, data.maxSalvoSize);           // e.g. 3 small, 5 med, 9 large
            float halfSpread = data.missileSpreadAngle * 0.5f;            // e.g. 30°

            if (data.fireSfx != null)
                AudioSource.PlayClipAtPoint(data.fireSfx, muzzle.position);

            for (int i = 0; i < count; i++)
            {
                // Evenly space missiles across the spread cone
                float angle = count == 1 ? 0f
                    : Mathf.Lerp(-halfSpread, halfSpread, (float)i / (count - 1));

                Quaternion rot = muzzle.rotation * Quaternion.Euler(0f, 0f, angle);
                GameObject go  = Object.Instantiate(data.projectilePrefab, muzzle.position, rot);

                if (go.TryGetComponent(out MissileProjectile mp))
                {
                    mp.damage          = data.damage;
                    mp.turnSpeed       = data.missileTurnSpeed;
                    mp.jerkForce       = data.missileJerkForce;
                    mp.isPlayerMissile = true;
                }

                if (go.TryGetComponent(out DamageDealer dd))
                    dd.SetDamage(data.damage, data.knockbackForce);

                if (data.muzzleVFXPrefab != null)
                    Object.Instantiate(data.muzzleVFXPrefab, muzzle.position, muzzle.rotation);

                // Stagger each missile slightly for a satisfying volley feel
                yield return new WaitForSeconds(burstFireDelay);
            }
        }
    }
}

