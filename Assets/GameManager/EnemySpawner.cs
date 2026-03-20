
// @author: Gary Yang
// @description: Systematic Scalable Enemy Spawner will take prefabs and updatable 
//               Configs for logic around spawning. Enemies 

using System;
using Unity.VisualScripting;

namespace GameManager
{
    

using UnityEngine;
using System.Collections.Generic;


public class EnemySpawner : MonoBehaviour
{
    private struct EnemySpawnInfo 
    {
        [Header("Spawn Mechanics")]
        [SerializeField] [Range(0, 100)] private float _spawnWeight;
        [SerializeField] [Range(1, 100)] private int _spawnLimit; 
        
        [Header("Prefab")] 
        [SerializeField] private GameObject _prefab;
    }

    [SerializeField] private List<EnemySpawnInfo> enemySpawnInfo;
    
    
    [DoNotSerialize] private float[] _spawnCounts;
    [DoNotSerialize] public Transform target;
    [DoNotSerialize] public Vector2 spawnRadiusBand;
    
    
    private void Start()
    {
        _spawnCounts = new float[enemySpawnInfo.Count];
    }

    
    private void Update()
    {
        
    }
}
}
