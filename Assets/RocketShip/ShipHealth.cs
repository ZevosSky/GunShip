// ShipHealth.cs — Player ship health with regen, bullet-time flag,
// damage trail, and maneuverability penalty.

using System;
using UnityEngine;
using GameManager;

namespace RocketShip
{
    public class ShipHealth : MonoBehaviour
    {
        // ── Static accessor (GameManager sets this on ship switch) ─────────
        public static ShipHealth Current { get; set; }

        // ── Inspector ─────────────────────────────────────────────────────
        [Header("Health")]
        [SerializeField] private float maxHealth    = 100f;
        [SerializeField] private float regenRate    = 5f;      // hp/s
        [SerializeField] [Range(0.1f, 10f)] private float regenDelay = 4f;

        [Header("Collision Damage")]
        [SerializeField] private float minCollisionSpeed   = 3f;
        [SerializeField] private float collisionDamageMultiplier = 2f;

        [Header("Death & Respawn")]
        [SerializeField] private GameObject explosionPrefab;

        // ── Events ────────────────────────────────────────────────────────
        public event Action<float> OnHealthChanged;   // ratio [0-1]
        public event Action        OnDeath;

        // ── Public state ──────────────────────────────────────────────────
        public float CurrentHealth  { get; private set; }
        public float HealthRatio    => CurrentHealth / maxHealth;
        public bool  IsDead         { get; private set; }
        public bool  IsLowHealth    => HealthRatio < 0.25f;

        // ── Maneuverability multiplier, read by ShipController ────────────
        public float ManeuverabilityMult { get; private set; } = 1f;

        // ── Private ───────────────────────────────────────────────────────
        private float          _regenTimer;
        private TrailRenderer  _trail;
        private SpriteRenderer _sr;
        private Color          _baseColor;
        private float          _flashTimer;
        private const float    FlashDuration = 0.1f;

        void Awake()
        {
            CurrentHealth = maxHealth;
            _trail = GetComponentInChildren<TrailRenderer>();
            _sr    = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
            if (_trail != null) _trail.emitting = false;
        }

        void Update()
        {
            if (IsDead) return;

            // Regen after delay
            if (_regenTimer > 0f)
                _regenTimer -= Time.deltaTime;
            else if (CurrentHealth < maxHealth)
            {
                CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + regenRate * Time.deltaTime);
                OnHealthChanged?.Invoke(HealthRatio);
                RefreshEffects();
            }

            // Hit flash
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_sr != null)
                    _sr.color = _flashTimer > 0f ? Color.white : _baseColor;
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            CurrentHealth  = Mathf.Max(0f, CurrentHealth - amount);
            _regenTimer    = regenDelay;
            OnHealthChanged?.Invoke(HealthRatio);
            _flashTimer    = FlashDuration;

            PopupTextSpawner.Instance?.Show(
                $"-{Mathf.RoundToInt(amount)}",
                transform.position + Vector3.up * 0.6f,
                new Color(1f, 0.2f, 0.2f));

            RefreshEffects();
            if (CurrentHealth <= 0f) Die();
        }

        void OnCollisionEnter2D(Collision2D col)
        {
            float impact = col.relativeVelocity.magnitude;
            if (impact > minCollisionSpeed)
                TakeDamage(impact * collisionDamageMultiplier);
        }

        void RefreshEffects()
        {
            // Damage trail activates below 60 % health
            if (_trail != null)
                _trail.emitting = HealthRatio < 0.6f;

            // Maneuverability scales from 1 (full health) to 0.4 (near death)
            ManeuverabilityMult = Mathf.Lerp(0.4f, 1f, HealthRatio);
        }

        void Die()
        {
            if (IsDead) return;
            IsDead = true;
            OnDeath?.Invoke();

            if (explosionPrefab != null)
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);

            PopupTextSpawner.Instance?.Show("SHIP DESTROYED",
                transform.position, Color.red);
        }

        // Called by GameManager to fully restore this ship
        public void Respawn()
        {
            IsDead        = false;
            CurrentHealth = maxHealth;
            _regenTimer   = 0f;
            ManeuverabilityMult = 1f;
            if (_trail != null) _trail.emitting = false;
            if (_sr    != null) _sr.color = _baseColor;
            OnHealthChanged?.Invoke(1f);
        }
    }
}
