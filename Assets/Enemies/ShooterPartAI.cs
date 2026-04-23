// ShooterPartAI.cs
// Activated when boss core dies. Orbits player, fires slow projectiles.
// Keep DISABLED while parented to boss.
using System.Collections;
using UnityEngine;
using World;
namespace Enemies
{
    // Rigidbody2D and Collider2D are required by BossPart on the same GameObject
    public class ShooterPartAI : MonoBehaviour
    {
        [Header("Orbital Movement")]
        [SerializeField] private float orbitRadius   = 10f;
        [SerializeField] private float orbitSpeed    = 1.5f;
        [SerializeField] private float approachSpeed = 2f;
        [SerializeField] private float turnSpeed     = 120f;
        [Header("Shooting")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float      shootInterval = 3f;
        [Header("World")]
        [SerializeField] private TorusWorld world;
        private Transform   _target;
        private Rigidbody2D _rb;
        private float       _orbitAngle;
        void OnEnable()
        {
            _rb = GetComponent<Rigidbody2D>();
            ResolveWorld();
            var sc = FindFirstObjectByType<RocketShip.ShipController>();
            if (sc != null)
            {
                _target = sc.transform;
                Vector2 offset = TorusDelta(_target.position, transform.position);
                _orbitAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            }
            StartCoroutine(ShootLoop());
        }
        void FixedUpdate()
        {
            if (_target == null || _rb == null) return;
            _orbitAngle += orbitSpeed * Time.fixedDeltaTime;
            float rad       = _orbitAngle * Mathf.Deg2Rad;
            Vector2 desired = (Vector2)_target.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
            Vector2 toDesired = TorusDelta(transform.position, desired);
            Vector2 desiredVelocity = toDesired.sqrMagnitude > 0.001f
                ? toDesired.normalized * approachSpeed
                : Vector2.zero;
            _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, desiredVelocity, 0.08f);
            Vector2 toTarget  = TorusDelta(transform.position, _target.position);
            float   faceAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.Euler(0f, 0f, faceAngle), turnSpeed * Time.fixedDeltaTime);
            WrapPosition();
        }
        IEnumerator ShootLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(shootInterval);
                if (_target == null || projectilePrefab == null) continue;
                Vector2 dir   = TorusDelta(transform.position, _target.position).normalized;
                float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                Instantiate(projectilePrefab, transform.position, Quaternion.Euler(0f, 0f, angle));
            }
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
