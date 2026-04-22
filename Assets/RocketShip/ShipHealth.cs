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

        [Header("Hit Flash")]
        [Tooltip("Sprites to flash on hit. Leave empty to auto-detect the SpriteRenderer on this GameObject.")]
        [SerializeField] private SpriteRenderer[] flashRenderers;
        [SerializeField] private Color flashColor    = Color.white;
        [SerializeField] private float flashDuration = 0.1f;

        [Header("Invincibility Frames")]
        [Tooltip("Seconds of damage immunity after being hit. 0 = no iframes.")]
        [SerializeField] private float iframeDuration;

        [Header("Effects")]
        [SerializeField] private ParticleSystem damageTrail;
        [SerializeField] [Range(0f, 1f)] private float damageTrailHealthThreshold = 0.6f;
        [SerializeField] [Range(0f, 1f)] private float handlingPenaltyStartsBelowHealthRatio = 0.55f;
        [SerializeField] [Range(0f, 1f)] private float minimumThrustMultiplier = 0.75f;
        [SerializeField] [Range(0f, 1f)] private float minimumTurnMultiplier = 0.85f;

        // ── Events ────────────────────────────────────────────────────────
        public event Action<float> OnHealthChanged;   // ratio [0-1]
        public event Action        OnDeath;

        // ── Public state ──────────────────────────────────────────────────
        public float CurrentHealth  { get; private set; }
        public float HealthRatio    => CurrentHealth / maxHealth;
        public bool  IsDead         { get; private set; }
        public bool  IsLowHealth    => HealthRatio < 0.25f;

        // ── Maneuverability multiplier, read by ShipController ────────────
        public float ThrustMultiplier { get; private set; } = 1f;
        public float TurnMultiplier   { get; private set; } = 1f;
        public float ManeuverabilityMult => ThrustMultiplier;

        // ── Private ───────────────────────────────────────────────────────
        private float          _regenTimer;
        private Color[]        _baseColors;
        private float          _flashTimer;
        private float          _iframeTimer;

        void Awake()
        {
            CurrentHealth = maxHealth;

            // Auto-detect if nothing assigned
            if (flashRenderers == null || flashRenderers.Length == 0)
            {
                var sr = GetComponent<SpriteRenderer>();
                flashRenderers = sr != null ? new[] { sr } : new SpriteRenderer[0];
            }

            _baseColors = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++)
                if (flashRenderers[i] != null) _baseColors[i] = flashRenderers[i].color;

            if (damageTrail != null)
            {
                var e = damageTrail.emission;
                e.enabled = false;
                damageTrail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        void Update()
        {
            if (IsDead) return;

            // Iframe timer
            if (_iframeTimer > 0f) _iframeTimer -= Time.deltaTime;

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
                bool flashing = _flashTimer > 0f;
                for (int i = 0; i < flashRenderers.Length; i++)
                    if (flashRenderers[i] != null)
                        flashRenderers[i].color = flashing ? flashColor : _baseColors[i];
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            if (_iframeTimer > 0f) return;

            CurrentHealth  = Mathf.Max(0f, CurrentHealth - amount);
            _regenTimer    = regenDelay;
            _flashTimer    = flashDuration;
            _iframeTimer   = iframeDuration;
            OnHealthChanged?.Invoke(HealthRatio);

            PopupTextSpawner.Instance?.Show(
                $"-{Mathf.RoundToInt(amount)}",
                transform.position + Vector3.up * 0.6f,
                new Color(1f, 0.2f, 0.2f));

            RefreshEffects();
            if (CurrentHealth <= 0f) Die();
        }

        void OnCollisionEnter2D(Collision2D col)
        {
            // Enemies handle their own contact damage — ignore them here
            if (col.gameObject.GetComponentInParent<Enemies.EnemyBase>() != null) return;

            float impact = col.relativeVelocity.magnitude;
            if (impact > minCollisionSpeed)
                TakeDamage(impact * collisionDamageMultiplier);
        }

        void RefreshEffects()
        {
            // Damage trail activates below threshold health
            if (damageTrail != null)
            {
                bool shouldPlay = HealthRatio < damageTrailHealthThreshold;
                var emission = damageTrail.emission;
                if (emission.enabled != shouldPlay)
                {
                    emission.enabled = shouldPlay;
                    if (shouldPlay) damageTrail.Play();
                    else            damageTrail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            float handlingRatio = Mathf.InverseLerp(0f, handlingPenaltyStartsBelowHealthRatio, HealthRatio);
            ThrustMultiplier = Mathf.Lerp(minimumThrustMultiplier, 1f, handlingRatio);
            TurnMultiplier   = Mathf.Lerp(minimumTurnMultiplier, 1f, handlingRatio);
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
            ThrustMultiplier = 1f;
            TurnMultiplier   = 1f;
            if (damageTrail != null) damageTrail.Stop();
            for (int i = 0; i < flashRenderers.Length; i++)
                if (flashRenderers[i] != null) flashRenderers[i].color = _baseColors[i];
            OnHealthChanged?.Invoke(1f);
        }
    }
}
