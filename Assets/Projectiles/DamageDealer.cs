// DamageDealer.cs — attach to any projectile prefab.
// isPlayerProjectile true  → damages Enemies.Health
// isPlayerProjectile false → damages RocketShip.ShipHealth

using UnityEngine;
using Enemies;

namespace Projectiles
{
    public class DamageDealer : MonoBehaviour
    {
        [SerializeField] public bool isPlayerProjectile = true;
        private float _damage    = 10f;
        private float _knockback = 3f;
        private bool  _hit;

        public void SetDamage(float dmg, float kb) { _damage = dmg; _knockback = kb; }

        void OnTriggerEnter2D(Collider2D col)
        {
            if (HasMissileProjectile()) return;
            if (ShouldIgnoreTrigger(col)) return;
            HandleHit(col.gameObject, col.ClosestPoint(transform.position));
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (HasMissileProjectile()) return;
            HandleHit(c.gameObject,
                c.contactCount > 0 ? c.contacts[0].point : transform.position);
        }

        void HandleHit(GameObject go, Vector2 pt)
        {
            if (_hit) return;

            if (isPlayerProjectile)
            {
                var h = go.GetComponentInParent<Health>();
                if (h == null) return;
                _hit = true;
                h.TakeDamage(_damage);
                ApplyKnockback(go.GetComponentInParent<Rigidbody2D>(), h.transform.position, pt);
            }
            else
            {
                var sh = go.GetComponentInParent<RocketShip.ShipHealth>();
                if (sh == null) return;
                _hit = true;
                sh.TakeDamage(_damage);
                ApplyKnockback(go.GetComponentInParent<Rigidbody2D>(), sh.transform.position, pt);
            }
            Destroy(gameObject);
        }

        bool ShouldIgnoreTrigger(Collider2D col)
        {
            if (isPlayerProjectile || !col.isTrigger) return false;
            return col.GetComponentInParent<RocketShip.ShipHealth>() != null;
        }

        bool HasMissileProjectile() => TryGetComponent<Weapons.MissileProjectile>(out _);

        void ApplyKnockback(Rigidbody2D rb, Vector2 targetPos, Vector2 hitPt)
        {
            if (rb == null) return;
            Vector2 dir = (targetPos - hitPt).normalized;
            if (dir == Vector2.zero) dir = Vector2.up;
            rb.AddForce(dir * _knockback, ForceMode2D.Impulse);
        }
    }
}
