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
    
    [Header("Ship Prefabs")]
    [SerializeField] private GameObject smallShipPrefab;
    [SerializeField] private GameObject mediumShipPrefab;
    [SerializeField] private GameObject largeShipPrefab;
    
    private enum StartShip { Small, Medium, Large }
    [SerializeField] private StartShip startShip = StartShip.Small;
    
    // Live instances spawned at runtime
    private GameObject _smallShip;
    private GameObject _mediumShip;
    private GameObject _largeShip;
    
    // Just stuff the script is going to store ref to for ease of access
    private ShipController _currentShipController;
    private TorusCamera    _torusCameraComponent;
    
    void Start()
    {
        _torusCameraComponent = cameraObject.GetComponent<TorusCamera>();
        
        // Instantiate whichever prefabs have been assigned
        _smallShip  = Spawn(smallShipPrefab);
        _mediumShip = Spawn(mediumShipPrefab);
        _largeShip  = Spawn(largeShipPrefab);
        
        // Disable PlayerInput on all ships first so only one is ever active
        SetPlayerInputEnabled(_smallShip,  false);
        SetPlayerInputEnabled(_mediumShip, false);
        SetPlayerInputEnabled(_largeShip,  false);

        var cO = cameraObject.GetComponent<TorusCamera>();
        cO.target = _smallShip.transform;
        cO.ship   = _smallShip.GetComponent<ShipController>();
        
        
        // Activate the starting ship
        GameObject startingShip = startShip switch
        {
            StartShip.Small  => _smallShip,
            StartShip.Medium => _mediumShip,
            StartShip.Large  => _largeShip,
            _                => _smallShip
        };
        
        if (startingShip != null)
            SwitchToShip(startingShip);
        else
            Debug.LogError("GameManager: starting ship prefab is not assigned!");
    }
    
    
    // Doing call backs was a pain in the ass so I'm doing this one the old polling way 
    // Just logic for 1-2-3 ship switching 
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if      (kb.digit1Key.wasPressedThisFrame && _smallShip  != null) SwitchToShip(_smallShip);
        else if (kb.digit2Key.wasPressedThisFrame && _mediumShip != null) SwitchToShip(_mediumShip);
        else if (kb.digit3Key.wasPressedThisFrame && _largeShip  != null) SwitchToShip(_largeShip);
    }

    // Instantiate a prefab at the origin, or return null if unassigned
    private static GameObject Spawn(GameObject prefab)
    {
        if (prefab == null) return null;
        return Instantiate(prefab, Vector3.zero, Quaternion.identity);
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

