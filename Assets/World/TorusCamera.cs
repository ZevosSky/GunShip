// Gary 
// This is contains the logic for the follow camera with additional logic for traversing the world seams


using Unity.VisualScripting;

namespace World
{
    
using UnityEngine;
using RocketShip;

[RequireComponent(typeof(Camera))]
public class TorusCamera : MonoBehaviour
{
    [Header("World")]
    [SerializeField] public TorusWorld world;

    [Header("Target")]
    [DoNotSerialize] public Transform target;

    [Header("Easing")]
    [Range(1f, 25f)]
    public float smoothing = 6f;         // Higher = snappier follow

    [Header("Look-Ahead")]
    public float lookAheadStrength  = 2.5f;
    public float lookAheadSmoothing = 3f;

    [Header("Z")]
    public float cameraZ = -10f;

    private Vector2 _camPos;
    private Vector2 _smoothedLookAhead;
    private Transform _lastTarget;          // Detects when target is swapped
    [DoNotSerialize] public ShipController ship;

    /// Raw torus-space camera position (unwrapped). Read by PlanetBackground
    /// to drive the planet sphere's UV scroll without relying on LateUpdate order.
    public Vector2 CamWorldPos => _camPos;

    private void Awake()
    {
        _camPos     = target != null ? (Vector2)target.position : Vector2.zero;
        _lastTarget = target;
        ship        = target != null ? target.GetComponent<ShipController>() : null;
    }
    
    private void LateUpdate()
    {
        if (target == null) return;

        // Refresh ship reference whenever GameManager swaps the target
        if (target != _lastTarget)
        {
            ship        = target.GetComponent<ShipController>();
            _lastTarget = target;
        }

        float dt = Time.deltaTime;

        if (ship != null)
        {
            Vector2 desiredLookAhead = ship.Velocity.normalized * lookAheadStrength;
            _smoothedLookAhead = Vector2.Lerp(
                _smoothedLookAhead, desiredLookAhead,
                1f - Mathf.Exp(-lookAheadSmoothing * dt));
        }

        Vector2 targetPos = (Vector2)target.position + _smoothedLookAhead;

        Vector2 shortDelta = world.ShortestDelta(_camPos, targetPos);
        Vector2 rawDelta   = targetPos - _camPos;

        if ((rawDelta - shortDelta).sqrMagnitude > 0.001f)
            _camPos = targetPos - shortDelta;

        float t = 1f - Mathf.Exp(-smoothing * dt);
        _camPos += shortDelta * t;

        transform.position = new Vector3(_camPos.x, _camPos.y, cameraZ);

        // Keep _camPos near the ship's wrapped coordinate space to prevent
        // float drift over very long sessions. 
        if (Mathf.Abs(_camPos.x - world.Wrap(_camPos).x) > world.width  * 0.9f ||
            Mathf.Abs(_camPos.y - world.Wrap(_camPos).y) > world.height * 0.9f)
        {
            _camPos = world.Wrap(_camPos);
        }
    }
}
}
