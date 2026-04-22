// ShooterPartAI.cs
// Activated when boss core dies. Orbits player, fires slow projectiles.
// Keep DISABLED while parented to boss.
using System.Collections;
using UnityEngine;
namespace Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
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
        [SerializeField] private float      projectileDmg = 10f;
        [SerializeField] private float      projectileKB  = 1f;
        private Transform   _target;
        private Rigidbody2D _rb;
        private float       _orbitAngle;
        void OnEnable()
        {
            _rb = GetComponent<Rigidbody2D>();
            var sc = FindFirstObjectByType<RocketShip.ShipController>();
            if (sc != null)
            {
                _target = sc.transform;
                Vector2 offset = (Vector2)(transform.position - _target.position);
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
            _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity,
                (desired - (Vector2)transform.position).normalized * approachSpeed, 0.08f);
            Vector2 toTarget  = (Vector2)(_target.position - transform.position);
            float   faceAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.Euler(0f, 0f, faceAngle), turnSpeed * Time.fixedDeltaTime);
        }
        IEnumerator ShootLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(shootInterval);
                if (_target == null || projectilePrefab == null) continue;
                Vector2 dir   = ((Vector2)_target.position - (Vector2)transform.position).normalized;
                float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                var     go    = Instantiate(projectilePrefab, transform.position, Quaternion.Euler(0f, 0f, angle));
                if (go.TryGetComponent(out Projectiles.DamageDealer dd))
                {
                    dd.isPlayerProjectile = false;
                    dd.SetDamage(projectileDmg, projectileKB);
                }
            }
        }
    }
}
