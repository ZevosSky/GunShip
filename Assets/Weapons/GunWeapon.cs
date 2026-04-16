// Gary Yang 
// 3/12/2026
// (updated 4/7/2026 — fixed bugs, added SpinDown, kick/spread)
// (updated 4/14/2026 — spin-down no longer fires; accumulator reset on re-engage)

using UnityEngine;
using Utils;
using Projectiles;

namespace Weapons
{
    public class GunWeapon : WeaponBase
    {
        #region Serialized Fields
        [Header("Spin-up/Spin-down Easing")]
        [SerializeField] protected EasingData spinUpEasingType;
        [SerializeField] protected EasingData spinDownEasingType;
        #endregion

        //===| State machine |===============================================
        private enum State { Idle, SpinningUp, FullSpin, SpinningDown }
        private State _firingState = State.Idle;

        //===| Fire timing |==================================================
        private float _fireCooldown;        // seconds between shots (shrinks while spinning up)
        private float _spinUpTimer;         // accumulated spin-up time
        private float _spinDownTimer;       // accumulated spin-down time
        private float _fireAccumulator;     // time since last shot

        //===| Spin ratio [0,1]: 0=idle  1=full speed |=======================
        // Used to scale bullet spread (kick)
        private float _spinRatio;

        // Full-speed fire interval = 1 / (maxShotsPerSecond * 3)
        private float FullSpeedCooldown => 1f / Mathf.Max(data.maxShotsPerSecond * 3f, 0.01f);
        private float IdleCooldown      => 1f / Mathf.Max(data.maxShotsPerSecond,       0.01f);

        //===================================================================
        public override void Tick(float dt)
        {
            if (data == null) return;

            switch (_firingState)
            {
                //===| IDLE |=================================================
                case State.Idle:
                    if (triggerHeld)
                    {
                        _fireCooldown = IdleCooldown;
                        _firingState  = State.SpinningUp;
                    }
                    break;

                //===| SPINNING UP |==========================================
                case State.SpinningUp:
                    if (!triggerHeld) { _firingState = State.SpinningDown; break; }

                    _spinUpTimer     += dt;
                    _fireAccumulator += dt;

                    float tu = Mathf.Clamp01(_spinUpTimer / data.spinUpTime);
                    tu = spinUpEasingType != null ? spinUpEasingType.Evaluate(tu) : tu;
                    _spinRatio    = tu;
                    _fireCooldown = Mathf.Lerp(IdleCooldown, FullSpeedCooldown, tu);

                    if (_fireAccumulator >= _fireCooldown)
                    {
                        Fire();
                        _fireAccumulator = 0f;
                    }

                    if (_spinRatio >= 1f)
                    {
                        _spinRatio    = 1f;
                        _fireCooldown = FullSpeedCooldown;
                        _firingState  = State.FullSpin;
                    }
                    break;

                //===| FULL SPIN |============================================
                case State.FullSpin:
                    if (!triggerHeld) { _firingState = State.SpinningDown; break; }

                    _fireAccumulator += dt;
                    if (_fireAccumulator >= _fireCooldown)
                    {
                        Fire();
                        _fireAccumulator = 0f;
                    }
                    break;

                //===| SPINNING DOWN |========================================
                case State.SpinningDown:
                    if (triggerHeld)
                    {
                        // Re-engage from current ratio; reset accumulator so no burst on re-engage
                        _spinUpTimer     = _spinRatio * data.spinUpTime;
                        _spinDownTimer   = 0f;
                        _fireAccumulator = 0f;
                        _firingState     = State.SpinningUp;
                        break;
                    }

                    _spinDownTimer += dt;
                    // No firing during spin-down — spin ratio decays, no bullets

                    float td = Mathf.Clamp01(_spinDownTimer / data.spinUpTime);
                    td = spinDownEasingType != null ? spinDownEasingType.Evaluate(td) : td;
                    _spinRatio    = 1f - td;
                    _fireCooldown = Mathf.Lerp(FullSpeedCooldown, IdleCooldown, td);


                    if (_spinRatio <= 0f)
                    {
                        _spinRatio = _spinUpTimer = _spinDownTimer = _fireAccumulator = 0f;
                        _firingState = State.Idle;
                    }
                    break;
            }
        }

        void Fire()
        {
            if (data.projectilePrefab == null)
            {
    #if UNITY_EDITOR
                Debug.LogWarning($"[GunWeapon] No projectile prefab on {data.name}!");
    #endif
                return;
            }

            // == Kick / spread: scales with spin ratio ==================
            float spread = data.maxSpreadDegrees * _spinRatio;
            float angle  = Random.Range(-spread, spread);
            Quaternion spreadRot = muzzle.rotation * Quaternion.Euler(0f, 0f, angle);

            // == Spawn bullet ==========================================
            GameObject bullet = Object.Instantiate(
                data.projectilePrefab, muzzle.position, spreadRot);

            // Set speed
            if (bullet.TryGetComponent(out BulletProjectile bp))
                bp.speed = data.projectileSpeed;

            // Set damage / knockback
            if (bullet.TryGetComponent(out DamageDealer dd))
                dd.SetDamage(data.damage, data.knockbackForce);

            // == Muzzle VFX ============================================
            if (data.muzzleVFXPrefab != null)
                Object.Instantiate(data.muzzleVFXPrefab, muzzle.position, muzzle.rotation);

            // == SFX ===================================================
            if (data.fireSfx != null)
                AudioSource.PlayClipAtPoint(data.fireSfx, muzzle.position);
        }

    }
}