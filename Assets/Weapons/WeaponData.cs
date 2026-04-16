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
        public int   maxSalvoSize         = 5;    // max missiles per escalating volley
        public float missileSpreadAngle   = 30f;  // full cone width at max salvo
        public float missileSalvoResetTime = 3f;  // seconds before salvo level resets
        public float missileTurnSpeed     = 150f; // deg/s for homing turn
        public float missileJerkForce     = 20f;  // acceleration toward target

        [Header("VFX/SFX")]
        public GameObject projectilePrefab;
        public GameObject muzzleVFXPrefab;
        public GameObject explosionPrefab;
        public AudioClip fireSfx;
    
    }

}
