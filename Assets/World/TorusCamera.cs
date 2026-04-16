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
    public float smoothing = 6f;

    [Header("Look-Ahead")]
    public float lookAheadStrength  = 2.5f;
    public float lookAheadSmoothing = 3f;

    [Header("Speed Zoom")]
    [SerializeField] private float baseCameraSize  = 10f;
    [SerializeField] private float zoomAtMaxSpeed  = 7f;   // extra units added at max speed
    [SerializeField] private float zoomSmoothing   = 3f;

    [Header("Z")]
    public float cameraZ = -10f;

    private Vector2    _camPos;
    private Vector2    _smoothedLookAhead;
    private Transform  _lastTarget;
    private Camera     _cam;
    [DoNotSerialize] public ShipController ship;

    public Vector2 CamWorldPos => _camPos;

    private void Awake()
    {
        _cam        = GetComponent<Camera>();
        _camPos     = target != null ? (Vector2)target.position : Vector2.zero;
        _lastTarget = target;
        ship        = target != null ? target.GetComponent<ShipController>() : null;
    }
    
    private void LateUpdate()
    {
        if (target == null) return;

        if (target != _lastTarget)
        {
            ship        = target.GetComponent<ShipController>();
            _lastTarget = target;
        }

        float dt = Time.deltaTime;

        // ── Look-ahead ────────────────────────────────────────────────────
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

        // ── Screen shake offset (computed in ScreenShake.Update, before LateUpdate) ──
        Vector2 shake = ScreenShake.Instance != null
            ? ScreenShake.Instance.CurrentOffset
            : Vector2.zero;

        transform.position = new Vector3(_camPos.x + shake.x, _camPos.y + shake.y, cameraZ);

        // ── Speed zoom ────────────────────────────────────────────────────
        if (_cam != null && ship != null)
        {
            float speedRatio  = ship.Velocity.magnitude / Mathf.Max(ship.maxSpeed, 0.01f);
            float targetSize  = baseCameraSize + zoomAtMaxSpeed * speedRatio;
            _cam.orthographicSize = Mathf.Lerp(
                _cam.orthographicSize, targetSize,
                1f - Mathf.Exp(-zoomSmoothing * dt));
        }

        // ── Drift guard ───────────────────────────────────────────────────
        if (Mathf.Abs(_camPos.x - world.Wrap(_camPos).x) > world.width  * 0.9f ||
            Mathf.Abs(_camPos.y - world.Wrap(_camPos).y) > world.height * 0.9f)
        {
            _camPos = world.Wrap(_camPos);
        }
    }
}
}
