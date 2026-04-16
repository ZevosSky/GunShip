
using System;
using Unity.VisualScripting;

namespace Projectiles
{
    

using UnityEngine;
using Projectiles;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    #region Serialized Fields & Configureables
    
    [Header("Projectile Data")]
    [SerializeField] private ProjectileData data;
    
    #endregion
    
    #region Public Fields 
    
    [HideInInspector] public ProjectilePool projectilePool;
    
    #endregion
    
    #region Private Fields

    private float _stateAccumulator;
    private float _totalSequenceTime;
    private int _currentSequence; 
    #endregion
    
    #region Unity

    private void Start()
    {
        _totalSequenceTime = data.TotalLifeTime();
        _currentSequence = 0; 
    }
    
    
    private void Update()
    {
        float dt = Time.deltaTime; 
        
        _stateAccumulator += dt;
        
        
        

    }
    
    #endregion
    
    #region Helpers
    
    
    
    
    #endregion
    
}
}
