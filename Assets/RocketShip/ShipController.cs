



namespace RocketShip{


using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using World;
    


[RequireComponent(typeof(Rigidbody2D))]
public class ShipController : MonoBehaviour
{
    [SerializeField] private GameObject smallShip;
    [SerializeField] private GameObject mediumShip;
    [SerializeField] private GameObject largeShip;
    
    [Header("World")]
    [SerializeField] private TorusWorld world;

    [Header("Jerk Chain")]
    public float jerkForce      = 120f;  // Rate of change of acceleration
    public float maxAcceleration = 25f;
    public float accelDecay      = 5f;   // Accel bleeds off when no input

    [Header("Velocity")]
    public float maxSpeed    = 18f;
    public float linearDrag  = 0.8f;     // Velocity damping coefficient

    [Header("Rotation")]
    public float rotationSpeed = 10f;

    // Exposed for camera look-ahead
    public Vector2 Velocity => _velocity;

    
    private Vector2 _acceleration;
    private Vector2 _velocity;
    private Vector2 _position;
    private Vector2 _inputDir;

    private void Awake()
    {
        _position = transform.position;
        // Kinematic: we own position
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
    }

    // Called by Unity's Input System PlayerInput component
    void OnMove(InputValue v) => _inputDir = v.Get<Vector2>();

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Jerk to Acceleration 
        // Player input is jerk: it changes acceleration, not velocity directly.
        _acceleration += _inputDir * (jerkForce * dt);

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

        // Rotate to face velocity direction
        if (_velocity.sqrMagnitude > 0.05f)
        {
            float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.Euler(0f, 0f, angle),
                rotationSpeed * dt);
        }
    }
    
    
}
}
