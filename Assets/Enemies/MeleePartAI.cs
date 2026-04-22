// MeleePartAI.cs
// Activated when the boss core dies and this part detaches.
// Behaviour: 4 orbital balls deal contact damage + knockback.
//            Part itself approaches the player at moderate speed.
//
// TODO: Implement orbital ball spawning, approach movement, and contact damage.
//       Keep this component DISABLED while parented to the boss — BossController enables it on detach.

using UnityEngine;

namespace Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MeleePartAI : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed  = 4f;
        [SerializeField] private float turnSpeed  = 180f;

        // TODO: orbital ball fields go here

        private Transform   _target;
        private Rigidbody2D _rb;

        void OnEnable()
        {
            _rb = GetComponent<Rigidbody2D>();
            var sc = FindFirstObjectByType<RocketShip.ShipController>();
            if (sc != null) _target = sc.transform;
            // TODO: spawn / enable orbital balls
        }

        void FixedUpdate()
        {
            if (_target == null || _rb == null) return;

            Vector2 toTarget = (Vector2)(_target.position - transform.position);
            if (toTarget.sqrMagnitude < 0.01f) return;

            float desiredAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0f, 0f, desiredAngle),
                turnSpeed * Time.fixedDeltaTime);

            _rb.linearVelocity = transform.up * moveSpeed;
        }
    }
}

