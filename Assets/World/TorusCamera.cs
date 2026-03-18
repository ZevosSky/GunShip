
using Unity.VisualScripting;

namespace World
{
    
using UnityEngine;
using RocketShip;

[RequireComponent(typeof(Camera))]
public class TorusCamera : MonoBehaviour
{
    [Header("World")]
    [SerializeField] private TorusWorld world;

    [Header("Target")]
    [SerializeField] public Transform target;

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
    [DoNotSerialize] public ShipController ship;

    private void Awake()
    {
        _camPos = target != null ? (Vector2)target.position : Vector2.zero;
        ship   = target != null ? target.GetComponent<ShipController>() : null;
    }
    
    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        if (ship != null)
        {
            Vector2 desiredLookAhead = ship.Velocity.normalized * lookAheadStrength;
            _smoothedLookAhead = Vector2.Lerp(
                _smoothedLookAhead, desiredLookAhead,
                1f - Mathf.Exp(-lookAheadSmoothing * dt));
        }

        Vector2 targetPos = (Vector2)target.position + _smoothedLookAhead;

        // Shortest path from camera to target on the torus
        Vector2 shortDelta = world.ShortestDelta(_camPos, targetPos);
        Vector2 rawDelta   = targetPos - _camPos;

        // If raw and short differ, the ship crossed a seam.
        // Reposition _camPos into the ship's coordinate neighborhood,
        // keeping the exact same lag offset so nothing jumps visually.
        //
        // e.g. ship was at 199.9, cam lagging at 197 (lag = 2.9)
        //      ship wraps to 0.1, shortDelta = 3.1
        //      _camPos = 0.1 - 3.1 = -3.0  (still 3.1 behind the ship, correct side)
        if ((rawDelta - shortDelta).sqrMagnitude > 0.001f)
            _camPos = targetPos - shortDelta;

        // Normal easing — shortDelta is still valid after the reposition
        float t = 1f - Mathf.Exp(-smoothing * dt);
        _camPos += shortDelta * t;

        // Do NOT wrap _camPos. Keeping it unwrapped means the Unity transform
        // stays in the ship's actual coordinate space. Your background just
        // needs to tile so it looks correct at any x/y value.
        transform.position = new Vector3(_camPos.x, _camPos.y, cameraZ);
        
        // normalize 
        float edgeDist = Mathf.Min(
            _camPos.x % world.width,
            _camPos.y % world.height
        );
        if (edgeDist > world.width * 0.4f)
            _camPos = world.Wrap(_camPos);
    }
}
}
