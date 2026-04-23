// MeleePartAI.cs
// Activated when the boss core dies and this part detaches.
// Behaviour: Approaches the player and periodically dashes at them.
//            Keep this component DISABLED while parented to the boss — BossController enables it on detach.

using System.Collections;
using UnityEngine;
using World;

namespace Enemies
{
    // Rigidbody2D and Collider2D are required by BossPart on the same GameObject
    public class MeleePartAI : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed  = 4f;
        [SerializeField] private float turnSpeed  = 180f;

        [Header("Dash")]
        [Tooltip("Impulse force applied during a dash")]
        [SerializeField] private float dashForce    = 18f;
        [Tooltip("Seconds between dashes")]
        [SerializeField] private float dashCooldown = 2.5f;
        [Tooltip("Distance to player at which a dash is triggered (0 = always dash when off cooldown)")]
        [SerializeField] private float dashRange    = 12f;
        [Tooltip("How long the part is locked into the dash direction before normal movement resumes")]
        [SerializeField] private float dashDuration = 0.35f;

        [Header("Contact Damage")]
        [SerializeField] private float contactDamage = 12f;
        [SerializeField] private float contactCooldown = 0.75f;
        [SerializeField] private float knockbackStrength = 8f;

        [Header("World")]
        [SerializeField] private TorusWorld world;

        private Transform   _target;
        private Rigidbody2D _rb;
        private float       _dashCooldownTimer;
        private float       _contactTimer;
        private bool        _dashing;

        void OnEnable()
        {
            _rb = GetComponent<Rigidbody2D>();
            var sc = FindFirstObjectByType<RocketShip.ShipController>();
            if (sc != null) _target = sc.transform;
            ResolveWorld();

            _dashCooldownTimer = dashCooldown; // start with one full cooldown before first dash
            _dashing           = false;
        }

        void FixedUpdate()
        {
            if (_target == null || _rb == null) return;

            Vector2 toTarget = TorusDelta(transform.position, _target.position);
            if (toTarget.sqrMagnitude < 0.01f) return;

            // ── Rotate to face target ──────────────────────────────────────
            float desiredAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0f, 0f, desiredAngle),
                turnSpeed * Time.fixedDeltaTime);

            // ── Cooldown tick ──────────────────────────────────────────────
            if (_dashCooldownTimer > 0f)
                _dashCooldownTimer -= Time.fixedDeltaTime;
            if (_contactTimer > 0f)
                _contactTimer -= Time.fixedDeltaTime;

            // ── Normal movement (suppressed while dashing) ─────────────────
            if (!_dashing)
            {
                _rb.linearVelocity = transform.up * moveSpeed;

                // ── Try to dash ────────────────────────────────────────────
                bool inRange = dashRange <= 0f || toTarget.magnitude <= dashRange;
                if (_dashCooldownTimer <= 0f && inRange)
                    StartCoroutine(Dash(toTarget.normalized));
            }

            WrapPosition();
        }

        IEnumerator Dash(Vector2 direction)
        {
            _dashing           = true;
            _dashCooldownTimer = dashCooldown;

            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(direction * dashForce, ForceMode2D.Impulse);

            yield return new WaitForSeconds(dashDuration);

            _dashing = false;
        }

        void OnCollisionEnter2D(Collision2D col)
            => TryContactDamage(col.gameObject);

        void OnCollisionStay2D(Collision2D col)
            => TryContactDamage(col.gameObject);

        void OnTriggerEnter2D(Collider2D col)
        {
            if (col.isTrigger) return;
            TryContactDamage(col.gameObject);
        }

        void OnTriggerStay2D(Collider2D col)
        {
            if (col.isTrigger) return;
            TryContactDamage(col.gameObject);
        }

        void TryContactDamage(GameObject other)
        {
            if (_contactTimer > 0f) return;

            var shipHealth = other.GetComponentInParent<RocketShip.ShipHealth>();
            if (shipHealth == null) return;

            shipHealth.TakeDamage(contactDamage);
            _contactTimer = contactCooldown;

            var shipController = other.GetComponentInParent<RocketShip.ShipController>();
            if (shipController != null)
            {
                Vector2 dir = TorusDelta(transform.position, shipController.transform.position).normalized;
                if (dir == Vector2.zero) dir = Vector2.up;
                shipController.ApplyKnockback(dir * knockbackStrength);
            }

            if (_rb != null)
                _rb.linearVelocity = -_rb.linearVelocity * 0.5f;
        }

        void ResolveWorld()
        {
            if (world != null) return;

            var part = GetComponent<BossPart>();
            if (part != null && part.world != null)
            {
                world = part.world;
                return;
            }

            var wrap = GetComponent<TorusWrap>();
            if (wrap == null) wrap = GetComponentInParent<TorusWrap>();
            if (wrap == null) wrap = FindFirstObjectByType<TorusWrap>();
            if (wrap != null) world = wrap.world;
        }

        Vector2 TorusDelta(Vector2 from, Vector2 to)
        {
            return world != null
                ? world.ShortestDelta(from, to)
                : to - from;
        }

        void WrapPosition()
        {
            if (world == null || _rb == null) return;

            Vector2 wrapped = world.Wrap(transform.position);
            if ((wrapped - (Vector2)transform.position).sqrMagnitude > 0.001f)
                _rb.MovePosition(wrapped);
        }
    }
}
