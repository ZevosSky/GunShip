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

        [Header("Death VFX")]
        [SerializeField] private GameObject explosionPrefab;

        public float CurrentHealth { get; private set; }
        public float HealthRatio   => CurrentHealth / maxHealth;
        public bool  IsDead        { get; private set; }

        public event Action<float> OnHealthChanged;
        public event Action        OnDeath;

        private SpriteRenderer _sr;
        private Color          _baseColor;
        private float          _flashTimer;
        private const float    FlashDur = 0.08f;

        void Awake()
        {
            CurrentHealth = maxHealth;
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        void OnEnable()  => AllEnemies.Add(this);
        void OnDisable() => AllEnemies.Remove(this);

        void Update()
        {
            if (_sr == null || _flashTimer <= 0f) return;
            _flashTimer -= Time.deltaTime;
            _sr.color = _flashTimer > 0f ? Color.white : _baseColor;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            _flashTimer   = FlashDur;
            OnHealthChanged?.Invoke(HealthRatio);

            GameManager.PopupTextSpawner.Instance?.Show(
                $"-{Mathf.RoundToInt(amount)}",
                transform.position + Vector3.up * 0.5f,
                new Color(1f, 0.4f, 0.1f));

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

