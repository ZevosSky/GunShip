// BulletProjectile.cs
// Simple straight-moving bullet fired by GunWeapon.
// Attach alongside DamageDealer on the bullet prefab.

using UnityEngine;

namespace Projectiles
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BulletProjectile : MonoBehaviour
    {
        [Tooltip("Set at runtime by GunWeapon.Fire()")]
        public float speed    = 30f;
        public float lifetime = 3f;

        private Rigidbody2D _rb;

        void Awake() => _rb = GetComponent<Rigidbody2D>();

        void OnEnable()
        {
            // Fire forward along transform.up (muzzle rotation)
            _rb.linearVelocity = transform.up * speed;
            Invoke(nameof(Die), lifetime);
        }

        void OnDisable() => CancelInvoke();

        void Die() => Destroy(gameObject);
    }
}

