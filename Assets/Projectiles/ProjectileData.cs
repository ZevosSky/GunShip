


namespace Projectiles
{
    

using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "ProjectileData", menuName = "Scriptable Objects/ProjectileData")]
public class ProjectileData : ScriptableObject
{
    private enum SequenceType
    {
        Homing,     // Moving towards a target with some turn speed 
        Projectile, // Free flying projectile 
        Targeting,  // No thrust, just reorienting towards target
        Propelled   // No Targeting, accelerating in current direction
    }
    [System.Serializable]
    private struct Sequence
    {
        [SerializeField] public float duration;
        [SerializeField] public SequenceType type;
        [SerializeField] public float thrust;
    }

    [SerializeField] private List<Sequence> sequences;
    
    [Tooltip("Degrees Per second of turn")]
    [SerializeField] [Range(0.01f, 420)]
    public float turnSpeed = 10f;
    
    
    [SerializeField] public bool isExplosive = false;
    [SerializeField] public GameObject endExplosion;
    
    
    // Death sound, vfx, and sound effect 
    
    
    
    
    public float TotalLifeTime()
    {
        float total = 0;
        for (int i = 0; i < sequences.Count; ++i)
        {
            total +=  sequences[i].duration;
        }
        return total;
    }
    
}
}
