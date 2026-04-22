// Author: Gary Yang
// 3/11/2026
// Weapon Data Scriptable Object



using UnityEngine;

namespace Weapons
{
    public enum WeaponType
    {
        Gun,
        Missile, 
        Laser
    }

    [CreateAssetMenu(menuName = "GunShip/Weapons/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public WeaponType Type; 
    
        [Header("Core")] 
        public float damage = 10f;
    
        [Header("Rate of Fire")]  // for guns 
        public float maxShotsPerSecond = 10f;
    
        [Header("Machine Gun Spin-up")] 
        public bool hasSpinUp;
        public float spinUpTime = 1.5f; 
    
        [Header("Laser Charge")] 
        public bool hasLaserCharge;
        public float chargeTime = 0.75f;
    
        [Header("Projectile Physics")]
        public float projectileSpeed   = 30f;
        public float projectileLifetime = 3f;

        [Header("Kick / Spread")]
        public float maxSpreadDegrees = 5f;
        public float knockbackForce   = 3f;

        [Header("Missile")]
        public int   maxSalvoSize          = 5;    // max missiles per escalating volley
        public float missileSpreadAngle    = 30f;  // full cone width at max salvo
        public float missileSalvoResetTime = 3f;   // seconds before salvo level resets
        public float missileTurnSpeed      = 150f; // deg/s for homing turn
        public float missileJerkForce      = 20f;  // acceleration toward target

        [Header("Homing Missile Phases")]
        [Tooltip("Enable 3-phase drift→reorient→boost homing behaviour")]
        public bool  isHoming                  = false;
        [Tooltip("Seconds the missile coasts before drag kicks in")]
        public float missileHomingDriftTime    = 0.6f;
        [Tooltip("Linear drag applied during Drift to bleed the missile to nearly a stop")]
        public float missileDriftDrag          = 4f;
        [Tooltip("Seconds the missile spends rotating to face the target before boosting")]
        public float missileHomingReorientTime = 0.4f;
        [Tooltip("Instant impulse applied the moment Boost begins — gives the snap-forward feel")]
        public float missileBoostImpulse       = 25f;
        [Tooltip("Continuous force applied each FixedUpdate during Boost")]
        public float missileBoostForce         = 30f;
        [Tooltip("Seconds of hard acceleration before auto-detonation")]
        public float missileBoostDuration      = 1.5f;

        [Header("VFX/SFX")]
        public GameObject projectilePrefab;
        public GameObject muzzleVFXPrefab;
        public GameObject explosionPrefab;
        public AudioClip fireSfx;
    
    }

}
