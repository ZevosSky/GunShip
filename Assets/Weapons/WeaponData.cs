// Author: Gary Yang
// 3/11/2026
// Weapon Data Scriptable Object



using UnityEngine;


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
    public float shotsPerSecond = 10f;
    
    [Header("Machine Gun Spin-up")] 
    public bool hasSpinUp = false;
    public float spinUpTime = 0.25f; 
    
    [Header("Laser Charge")] 
    public bool hasLaserCharge = false;
    public float chargeTime = 0.75f;
    
    [Header("VFX/SFX")]
    public GameObject projectilePrefab;
    public GameObject muzzleVFXPrefab;
    public AudioClip fireSfx;
    
}
