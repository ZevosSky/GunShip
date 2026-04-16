namespace RocketShip{


using UnityEngine;
using UnityEngine.InputSystem;
using Weapons;
using World;
    

[RequireComponent(typeof(Rigidbody2D))]
public class ShipController : MonoBehaviour
{
    [Header("World")]
    [SerializeField] public TorusWorld world;

    [Header("Weapon Mounts")]
    [SerializeField] private WeaponMount[] weaponMounts = new WeaponMount[0];

    [Header("Jerk Chain")]
    public float jerkForce       = 120f;
    public float maxAcceleration = 25f;
    public float accelDecay      = 5f;

    [Header("Velocity")]
    public float maxSpeed   = 18f;
    public float linearDrag = 0.8f;

    [Header("Rotation")]
    public float rotationSpeed = 180f;

    public Vector2 Velocity => _velocity;

    private float   _angle;
    private Vector2 _acceleration;
    private Vector2 _velocity;
    private Vector2 _position;
    private float   _thrustInput;
    private float   _rotateInput;

    private Vector2 _prevPosition;
    private float   _prevAngle;
    private Vector2 _stepDelta;

    // ── Weapon hold state ─────────────────────────────────────────────
    private bool _primaryDown;

    // ── Weapon helpers ────────────────────────────────────────────────
    private void TriggerDownAll(WeaponRole role)
    {
        foreach (var m in weaponMounts)
            if (m != null && m.Role == role) m.TriggerDown();
    }
    private void TriggerUpAll(WeaponRole role)
    {
        foreach (var m in weaponMounts)
            if (m != null && m.Role == role) m.TriggerUp();
    }
    private void FireOnceAll(WeaponRole role)
    {
        foreach (var m in weaponMounts)
            if (m != null && m.Role == role) m.FireOnce();
    }

    private void Awake()
    {
        _position     = transform.position;
        _prevPosition = _position;
        _angle        = transform.eulerAngles.z;
        _prevAngle    = _angle;
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;   // allows OnCollisionEnter2D to fire on this body
    }

    private void OnThrust(InputValue v) => _thrustInput = v.Get<float>();
    private void OnRotate(InputValue v) => _rotateInput = v.Get<float>();

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Apply maneuverability penalty if ShipHealth is present
        float mult = 1f;
        if (TryGetComponent(out ShipHealth sh)) mult = sh.ManeuverabilityMult;

        _prevPosition = _position;
        _prevAngle    = _angle;

        _angle += _rotateInput * rotationSpeed * mult * dt;

        Vector2 forward = new Vector2(
            -Mathf.Sin(_angle * Mathf.Deg2Rad),
             Mathf.Cos(_angle * Mathf.Deg2Rad));

        _acceleration += forward * (_thrustInput * jerkForce * mult * dt);
        _acceleration  = Vector2.MoveTowards(_acceleration, Vector2.zero, accelDecay * dt);
        _acceleration  = Vector2.ClampMagnitude(_acceleration, maxAcceleration);

        _velocity += _acceleration * dt;
        _velocity *= Mathf.Clamp01(1f - linearDrag * dt);
        _velocity  = Vector2.ClampMagnitude(_velocity, maxSpeed);

        _position += _velocity * dt;
        _position  = world.Wrap(_position);

        _stepDelta = world.ShortestDelta(_prevPosition, _position);
    }

    void Update()
    {
        float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
        Vector2 renderPos   = _position - _stepDelta * (1f - alpha);
        float   renderAngle = Mathf.LerpUnclamped(_prevAngle, _angle, alpha);

        transform.position = new Vector3(renderPos.x, renderPos.y, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, renderAngle);

        //===| Weapon input (keyboard polling — reliable for hold detection) |===========================  
        var kb = Keyboard.current;
        if (kb != null)
        {
            // Primary hold (SPACE) — chaingun or any Primary-role mount
            bool primaryNow = kb.spaceKey.isPressed;
            if (primaryNow  && !_primaryDown) TriggerDownAll(WeaponRole.Primary);
            if (!primaryNow &&  _primaryDown) TriggerUpAll(WeaponRole.Primary);
            _primaryDown = primaryNow;

            // Secondary single press (ENTER) — missiles or any Secondary-role mount
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                FireOnceAll(WeaponRole.Secondary);

            // Ship switching (1 / 2 / 3) — kept here from original GameManager
        }
    }

    // Injects an impulse into the custom physics — called by EnemyBase on contact
    public void ApplyKnockback(Vector2 impulse)
    {
        _velocity = Vector2.ClampMagnitude(_velocity + impulse, maxSpeed);
    }
}  // class ShipController
}  // namespace RocketShip
