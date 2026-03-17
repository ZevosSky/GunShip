using Unity.VisualScripting;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private GameObject smallShip;
    [SerializeField] private GameObject mediumShip;
    [SerializeField] private GameObject largeShip;
    
    
    
    [DoNotSerialize] public BoxCollider2D hitbox;
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
