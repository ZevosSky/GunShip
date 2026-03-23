namespace RocketShip{


using UnityEngine;
using UnityEngine.InputSystem;
using World;
    

[RequireComponent(typeof(Rigidbody2D))]
public class ShipController : MonoBehaviour
{
    [Header("World")]
    [SerializeField] public TorusWorld world;

    [Header("Jerk Chain")]
    public float jerkForce       = 120f;  // Rate of change of acceleration
    public float maxAcceleration = 25f;
    public float accelDecay      = 5f;    // Accel bleeds off when no input

    [Header("Velocity")]
    public float maxSpeed   = 18f;
    public float linearDrag = 0.8f;       // Velocity damping coefficient

    [Header("Rotation")]
    public float rotationSpeed = 180f;    // Degrees per second

    // Exposed for camera look-ahead
    public Vector2 Velocity => _velocity;

    private float   _angle;
    private Vector2 _acceleration;
    private Vector2 _velocity;
    private Vector2 _position;
    private float   _thrustInput;
    private float   _rotateInput;

    // Sub-step interpolation — keeps rendered position smooth between physics ticks
    private Vector2 _prevPosition;
    private float   _prevAngle;
    private Vector2 _stepDelta;   // Wrap-safe displacement for the last physics step

    private void Awake()
    {
        _position     = transform.position;
        _prevPosition = _position;
        _angle        = transform.eulerAngles.z;
        _prevAngle    = _angle;
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
    }

    // Called by Unity's Input System PlayerInput component
    private void OnThrust(InputValue v) => _thrustInput = v.Get<float>();
    private void OnRotate(InputValue v) => _rotateInput = v.Get<float>();
    
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Snapshot state at the START of the step for interpolation smoothing ( for the camera ) 
        _prevPosition = _position;
        _prevAngle    = _angle;

        // Rotation
        _angle += _rotateInput * rotationSpeed * dt;

        // Forward direction
        Vector2 forward = new Vector2(
            -Mathf.Sin(_angle * Mathf.Deg2Rad),
             Mathf.Cos(_angle * Mathf.Deg2Rad));

        // Jerk to Acceleration
        _acceleration += forward * (_thrustInput * jerkForce * dt);
        _acceleration  = Vector2.MoveTowards(_acceleration, Vector2.zero, accelDecay * dt);
        _acceleration  = Vector2.ClampMagnitude(_acceleration, maxAcceleration);

        // Acceleration to Velocity
        _velocity += _acceleration * dt;
        _velocity *= Mathf.Clamp01(1f - linearDrag * dt);
        _velocity  = Vector2.ClampMagnitude(_velocity, maxSpeed);

        // Velocity to Position (w/ torus wrap)
        _position += _velocity * dt;
        _position  = world.Wrap(_position);

        // Record the wrap-safe displacement so Update() can interpolate correctly
        // across seams (e.g. 199 → 1 has ShortestDelta = 2, not -198)
        _stepDelta = world.ShortestDelta(_prevPosition, _position);

        // Do NOT write transform here — Update() handles the visual position.
    }

    // Runs every rendered frame. Blends between the previous and current physics
    // step positions so the ship never appears to freeze between ticks.
    void Update()
    {
        // How far we are through the current physics step (0 = just ticked, 1 = about to tick)
        float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);

        // Interpolate using the wrap-safe delta:
        //   alpha=0  ->  _position - _stepDelta  =  _prevPosition  (start of step)
        //   alpha=1  ->  _position               =  current pos    (end of step)
        Vector2 renderPos   = _position - _stepDelta * (1f - alpha);
        float   renderAngle = Mathf.LerpUnclamped(_prevAngle, _angle, alpha);

        transform.position = new Vector3(renderPos.x, renderPos.y, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, renderAngle);
    }
}
}
