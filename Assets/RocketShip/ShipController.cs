



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

    private float   _angle;              // Ship facing angle in degrees (Unity Z)
    private Vector2 _acceleration;
    private Vector2 _velocity;
    private Vector2 _position;
    private float   _thrustInput;        // W = +1 (forward),  S = -1 (brake)
    private float   _rotateInput;        // A = +1 (CCW/left), D = -1 (CW/right)

    private void Awake()
    {
        _position = transform.position;
        _angle    = transform.eulerAngles.z;
        // Kinematic: we own position
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
    }

    // Called by Unity's Input System PlayerInput component
    private void OnThrust(InputValue v) => _thrustInput = v.Get<float>();
    private void OnRotate(InputValue v) => _rotateInput = v.Get<float>();
    
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Rotation: A/D spin the ship directly
        _angle += _rotateInput * rotationSpeed * dt;
        transform.rotation = Quaternion.Euler(0f, 0f, _angle);

        // Forward direction from current facing angle
        Vector2 forward = transform.up;

        // Jerk to Acceleration
        // W/S input is jerk: it changes acceleration along the forward axis.
        _acceleration += forward * (_thrustInput * jerkForce * dt);

        // Decay acceleration toward zero when no input, and clamp to maxAcceleration
        _acceleration = Vector2.MoveTowards(
            _acceleration, Vector2.zero, accelDecay * dt);
        _acceleration = Vector2.ClampMagnitude(_acceleration, maxAcceleration);

        // Acceleration to Velocity
        _velocity += _acceleration * dt;
        _velocity *= Mathf.Clamp01(1f - linearDrag * dt); // frame-rate safe drag
        _velocity  = Vector2.ClampMagnitude(_velocity, maxSpeed);

        // Velocity to Position (w/ torus wrap)
        _position += _velocity * dt;
        _position  = world.Wrap(_position);

        transform.position = new Vector3(_position.x, _position.y, 0f);
    }
    
    
}
}
