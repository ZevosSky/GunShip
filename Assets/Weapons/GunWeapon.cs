// Gary Yang 
// 3/12/2026

using System;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using Utils;


namespace Weapons
{
    public class GunWeapon : WeaponBase
    {
        #region Serialized Fields
        //===| Serialized Fields |====||
        [Header("Spin-up/Spin-down Easing For Guns")]
        [SerializeField] protected EasingData spinUpEasingType;
        [SerializeField] protected EasingData spinDownEasingType;
        #endregion
        
        //===| State data |===========|| 
        private enum State{Idle, SpinningUp, FullSpin, SpinningDown}
        private State _firingState = State.Idle;
        
        //===| Fire Logic values |====||
        
        // This the threshold of time needed to fire which gradually gets shorter
        // as you spin up and gets larger as you spin down 
        private float _fireCooldown;         
        
        private float _spinUpTimer; // time spent spinning up
        
        private float _fireAccumulator; // This is the time elapsed since the last shot 
        
        

        public override void Tick(float dt)
        {
            if (data == null) return;

            switch (_firingState)
            {
                case State.Idle:
                    if (triggerHeld) {
                        _firingState = State.SpinningUp;
                    }
                    break;
                case State.SpinningUp: // gradually fire faster and faster till full speed
                    if (!triggerHeld) {
                        _firingState = State.SpinningDown;
                        break;
                    }
                    // accumulate time and check if we can fire
                    _fireAccumulator += dt;
                    _spinUpTimer += dt;
                    // fire if we can
                    if (_fireAccumulator > _fireCooldown) {
                        Fire();
                        _fireAccumulator = 0f;
                    }
                    // adjust _fireCooldown based on spin up easing function and time spent spinning up
                    
                    // Normalize how far into spinup we are (t)
                    float t = _spinUpTimer / data.spinUpTime; 
                    t = Mathf.Clamp01(t); 
                    t = spinUpEasingType.Evaluate(t); // Evaluate what that would be eased
                    
                    // un-normalize to calculate _fireCooldown
                    _fireCooldown = Mathf.Lerp(1f / data.maxShotsPerSecond, 0.01f, t); // from slow fire rate to very fast fire rate (0.01s between shots)
                    
                    
                    // if we reach full spin, transition to full spin state
                    if (_fireCooldown <= data.maxShotsPerSecond) {
                        _firingState = State.FullSpin;
                        break;
                    }
                    break;
                    
                case State.FullSpin:
                    if (!triggerHeld) {
                        _firingState = State.SpinningDown;
                        break;
                    }
                    // accumulate time and check if we can fire
                    _fireAccumulator += dt;

                    if (_fireAccumulator > _fireCooldown)
                    {
                        
                    }
                    break;
                
            }
        }

        void Fire()
        {
            // for now just instantiate the weapon prefab
            // todo: add a projectile system. 
            
            #if UNITY_EDITOR
            if (data.projectilePrefab == null) {
                Debug.LogWarning("No projectile prefab assigned to weapon data!");
            } else { Instantiate(data.projectilePrefab,  muzzle); }

            if (data.muzzleVFXPrefab == null) {
                Debug.LogWarning("No muzzle VFX prefab assigned to weapon data!");
            } else { Instantiate(data.muzzleVFXPrefab, muzzle); }
            
            if (data.fireSfx == null) {
                Debug.LogWarning("No fire SFX assigned to weapon data!");
            } else { AudioSource.PlayClipAtPoint(data.fireSfx, muzzle.position); }
            
            #else // In build, just fire without warnings 
            
            if (data.projectilePrefab != null) { Instantiate(data.projectilePrefab,  muzzle); }
            if (data.muzzleVFXPrefab != null) { Instantiate(data.muzzleVFXPrefab, muzzle); }
            if (data.fireSfx != null) { AudioSource.PlayClipAtPoint(data.fireSfx, muzzle.position); }
            
            #endif
            
        }
        
        
        #region Unity Functions

        void Start()
        {
            // calculate how long it takes to get from 0 to full fire rate in given spin up time in data
            
            
        }

        void Update()
        {
            
        }
        #endregion


    }
}