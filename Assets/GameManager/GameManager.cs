

// This class should be responsible for....
// - Swaping between the 3 ships 
//     - re-wiring the UI & Camera to the new ship 
//     - cleaning up anything the ship did


using RocketShip;
using UnityEngine.InputSystem;
using World;

namespace GameManager
{

using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Scene References")]
    [SerializeField] private GameObject cameraObject;
    
    [Header("Ship References")]
    [SerializeField] private GameObject smallShip;
    [SerializeField] private GameObject mediumShip;
    [SerializeField] private GameObject largeShip;
    
    private enum StartShip { Small, Medium, Large }
    [SerializeField] private StartShip startShip = StartShip.Small;
    
    // Just stuff the script is going to store ref to for ease of access
    private ShipController _currentShipController;
    private TorusCamera    _torusCameraComponent;
    
    void Start()
    {
        _torusCameraComponent = cameraObject.GetComponent<TorusCamera>();
        
        // Disable PlayerInput on all ships first so only one is ever active
        SetPlayerInputEnabled(smallShip,  false);
        SetPlayerInputEnabled(mediumShip, false);
        SetPlayerInputEnabled(largeShip,  false);
        
        // Activate the starting ship
        GameObject startingShip = startShip switch
        {
            StartShip.Small  => smallShip,
            StartShip.Medium => mediumShip,
            StartShip.Large  => largeShip,
            _                => smallShip
        };
        SwitchToShip(startingShip);
    }

    // Call this whenever you want to hand control to a different ship
    public void SwitchToShip(GameObject newShip)
    {
        // Revoke input from current ship
        if (_currentShipController != null)
            SetPlayerInputEnabled(_currentShipController.gameObject, false);
        
        // Grant input to new ship
        _currentShipController = newShip.GetComponent<ShipController>();
        SetPlayerInputEnabled(newShip, true);
        
        // Re-point the camera
        if (_torusCameraComponent != null)
            _torusCameraComponent.target = newShip.transform;
    }

    private static void SetPlayerInputEnabled(GameObject ship, bool enabled)
    {
        if (ship == null) return;
        var pi = ship.GetComponent<PlayerInput>();
        if (pi != null) pi.enabled = enabled;
    }
}
}

