using UnityEngine;


namespace Weapons {
    
public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected WeaponData data;
    protected Transform muzzle; 
    protected bool triggerHeld; 
    
    // TODO: Make a projectile pool, so we aren't just entering construct & destruct hell. 
    
    public virtual void Equip(WeaponData newWeaponDat,Transform muzzleLocation) 
    {
        data = newWeaponDat;
        muzzle = muzzleLocation;
        triggerHeld = false; 
    }

    public virtual void TriggerDown(Transform muzzleTransform)
    {
        muzzle = muzzleTransform;
        triggerHeld = true;
        OnTriggerDown();
    }

    public virtual void TriggerUp()
    {
        triggerHeld = false;
        OnTriggerUp();
    }

    public virtual void Tick(float dt) { }

    protected virtual void OnEquipped() { }
    protected virtual void OnTriggerDown() { }
    protected virtual void OnTriggerUp() { }
    
    #region Unity Functions
    /* nothing cuz virtual */ 
    #endregion
}

}
