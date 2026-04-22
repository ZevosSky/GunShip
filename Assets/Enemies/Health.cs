// Health.cs — generic enemy health.
// Registers itself in a static list so missiles can find all targets.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
    public class Health : MonoBehaviour
    {
        // Static target registry — missiles iterate this to find enemies
        public static readonly List<Health> AllEnemies = new List<Health>();

        [SerializeField] public float maxHealth = 50f;

        [Header("Damage VFX")] [SerializeField]
        private ParticleSystem hitEffect;
        
        [SerializeField] private ParticleSystem lowHealthEffect;
        [SerializeField] [Range(0f, 1f)] private float healthRatio;

        [Header("Hit Flash")]
        [Tooltip("Collect all SpriteRenderers from this GameObject and its children. Overrides manual flashRenderers list when set.")]
        [SerializeField] private GameObject flashRoot;
        [Tooltip("Sprites to flash on hit. Leave empty to auto-detect the SpriteRenderer on this GameObject.")]
        [SerializeField] private SpriteRenderer[] flashRenderers;
        [SerializeField] private Color  flashColor    = Color.white;
        [SerializeField] private float  flashDuration = 0.08f;

        [Header("Invincibility Frames")]
        [Tooltip("Seconds of damage immunity after being hit. 0 = no iframes.")]
        [SerializeField] private float iframeDuration = 0f;

        [Header("Death VFX")]
        [SerializeField] private GameObject explosionPrefab;

        public float CurrentHealth { get; private set; }
        public float HealthRatio   => CurrentHealth / maxHealth;
        public bool  IsDead        { get; private set; }

        public event Action<float> OnHealthChanged;
        public event Action        OnDeath;

        private Color[] _baseColors;
        private float   _flashTimer;
        private float   _iframeTimer;

        void Awake()
        {
            CurrentHealth = maxHealth;

            // flashRoot overrides manual list — collect all SpriteRenderers in that hierarchy
            if (flashRoot != null)
                flashRenderers = flashRoot.GetComponentsInChildren<SpriteRenderer>(true);

            // Auto-detect if nothing assigned
            if (flashRenderers == null || flashRenderers.Length == 0)
            {
                var sr = GetComponent<SpriteRenderer>();
                flashRenderers = sr != null ? new[] { sr } : new SpriteRenderer[0];
            }

            _baseColors = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++)
                if (flashRenderers[i] != null) _baseColors[i] = flashRenderers[i].color;

            if (lowHealthEffect != null)
            {
                var e = lowHealthEffect.emission;
                e.enabled = false;
                lowHealthEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void OnEnable()  => AllEnemies.Add(this);
        private void OnDisable() => AllEnemies.Remove(this);

        private void Update()
        {
            if (_iframeTimer > 0f) _iframeTimer -= Time.deltaTime;

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                bool flashing = _flashTimer > 0f;
                for (int i = 0; i < flashRenderers.Length; i++)
                    if (flashRenderers[i] != null)
                        flashRenderers[i].color = flashing ? flashColor : _baseColors[i];
            }
            
            if (lowHealthEffect != null) {
                if (CurrentHealth / maxHealth <= healthRatio)
                    if (!lowHealthEffect.isPlaying) lowHealthEffect.Play();
                else
                    if (lowHealthEffect.isPlaying) lowHealthEffect.Stop();
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            if (_iframeTimer > 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            _flashTimer   = flashDuration;
            _iframeTimer  = iframeDuration;
            OnHealthChanged?.Invoke(HealthRatio);

            GameManager.PopupTextSpawner.Instance?.Show(
                $"-{Mathf.RoundToInt(amount)}",
                transform.position + Vector3.up * 0.5f,
                new Color(1f, 0.4f, 0.1f));
            
            if (hitEffect != null) hitEffect.Play();

            if (CurrentHealth <= 0f) Die();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(HealthRatio);
        }

        void Die()
        {
            if (IsDead) return;
            IsDead = true;
            OnDeath?.Invoke();

            if (explosionPrefab != null)
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);

            GameManager.PopupTextSpawner.Instance?.Show(
                "Destroyed!", transform.position + Vector3.up, Color.red);

            Destroy(gameObject);
        }
    }
}

