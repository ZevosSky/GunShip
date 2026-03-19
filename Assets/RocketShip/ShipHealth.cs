

using Unity.VisualScripting;

namespace RocketShip
{
    

using UnityEngine;



public class ShipHealth : MonoBehaviour
{
    
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int baseRegenRate = 5;
    [SerializeField][Range(0.1f, 6f)] private float regenDelay = 4f;
    [DoNotSerialize] public int currentHealth;
    
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    
}
}
