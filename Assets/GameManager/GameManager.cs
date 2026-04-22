// GameManager.cs
// Handles ship switching, camera wiring, text effects, and ship respawn.

using System.Collections;
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
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private World.TorusWorld world;   // used to find center spawn point

    [Header("Ship Prefabs")]
    [SerializeField] private GameObject smallShipPrefab;
    [SerializeField] private GameObject mediumShipPrefab;
    [SerializeField] private GameObject largeShipPrefab;

    private enum StartShip { Small, Medium, Large }
    [SerializeField] private StartShip startShip = StartShip.Small;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 5f;

    // Live instances
    private GameObject _smallShip;
    private GameObject _mediumShip;
    private GameObject _largeShip;

    private ShipController _currentShipController;
    private ShipHealth     _currentShipHealth;
    private TorusCamera    _torusCameraComponent;
    private bool           _respawning;

    private static readonly Vector3 _defaultCenter = new Vector3(100f, 60f, 0f);

    private Vector3 WorldCenter =>
        world != null ? new Vector3(world.width * 0.5f, world.height * 0.5f, 0f) : _defaultCenter;

    void Start()
    {
        _torusCameraComponent = cameraObject.GetComponent<TorusCamera>();

        _smallShip  = Spawn(smallShipPrefab);
        _mediumShip = Spawn(mediumShipPrefab);
        _largeShip  = Spawn(largeShipPrefab);

        // Deactivate all ships; SwitchToShip will activate the chosen one
        if (_smallShip  != null) _smallShip.SetActive(false);
        if (_mediumShip != null) _mediumShip.SetActive(false);
        if (_largeShip  != null) _largeShip.SetActive(false);

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

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if      (kb.digit1Key.wasPressedThisFrame && _smallShip  != null) SwitchToShip(_smallShip);
        else if (kb.digit2Key.wasPressedThisFrame && _mediumShip != null) SwitchToShip(_mediumShip);
        else if (kb.digit3Key.wasPressedThisFrame && _largeShip  != null) SwitchToShip(_largeShip);
    }

    private GameObject Spawn(GameObject prefab)
    {
        if (prefab == null) return null;
        return Instantiate(prefab, WorldCenter, Quaternion.identity);
    }

    public void SwitchToShip(GameObject newShip)
    {
        // Deactivate old ship entirely so its physics don't interfere
        if (_currentShipController != null)
        {
            _currentShipHealth.OnDeath -= OnShipDied;
            _currentShipController.gameObject.SetActive(false);
        }

        // Activate the new ship before getting components
        newShip.SetActive(true);

        _currentShipController = newShip.GetComponent<ShipController>();
        _currentShipHealth     = newShip.GetComponent<ShipHealth>();

        SetPlayerInputEnabled(newShip, true);

        // Point camera
        if (_torusCameraComponent != null)
        {
            _torusCameraComponent.target = newShip.transform;
            _torusCameraComponent.ship   = _currentShipController;
        }

        // Subscribe to new ship's death
        if (_currentShipHealth != null)
            _currentShipHealth.OnDeath += OnShipDied;

        // BulletTimeController watches ShipHealth.Current
        ShipHealth.Current = _currentShipHealth;

        // Wire enemy spawner target
        if (enemySpawner != null)
            enemySpawner.target = newShip.transform;

        // Popup text: ship type name
        string shipName = newShip == _smallShip  ? "SMALL SHIP"
                        : newShip == _mediumShip ? "MEDIUM SHIP"
                        : "LARGE SHIP";
        PopupTextSpawner.Instance?.Show(shipName,
            newShip.transform.position + Vector3.up * 2f,
            new Color(0.4f, 0.9f, 1f));
    }

    void OnShipDied()
    {
        if (_respawning) return;
        StartCoroutine(RespawnAfterDelay(_currentShipController?.gameObject));
    }

    IEnumerator RespawnAfterDelay(GameObject ship)
    {
        if (ship == null) yield break;
        _respawning = true;

        // Deactivate ship during death sequence
        ship.SetActive(false);

        yield return new WaitForSecondsRealtime(respawnDelay);

        // Move to safe spawn point before reactivating
        ship.transform.position = new Vector3(100f, 60f, 0f);
        ship.SetActive(true);

        // Respawn — restore health, re-enable
        var sh = ship.GetComponent<ShipHealth>();
        sh?.Respawn();


        SetPlayerInputEnabled(ship, true);
        ShipHealth.Current = sh;

        PopupTextSpawner.Instance?.Show("SHIP RESPAWNED",
            ship.transform.position + Vector3.up * 2f,
            new Color(0.4f, 1f, 0.5f));

        _respawning = false;
    }

    private static void SetPlayerInputEnabled(GameObject ship, bool enabled)
    {
        if (ship == null) return;
        var pi = ship.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi != null) pi.enabled = enabled;
    }
}
}

