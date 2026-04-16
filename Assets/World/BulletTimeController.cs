// BulletTimeController.cs
// Polls ShipHealth.Current each frame. When HP < 25% it slows time.

using UnityEngine;
using UnityEngine.Rendering;
using RocketShip;

namespace World
{
    public class BulletTimeController : MonoBehaviour
    {
        public static BulletTimeController Instance { get; private set; }

        [Header("Time Scale")]
        [SerializeField] [Range(0.1f, 1f)] private float slowTimeScale  = 0.35f;
        [SerializeField]                   private float transitionSpeed = 3f;

        [Header("Audio Pitch (optional — assign your music AudioSource)")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] [Range(0.3f, 1f)] private float slowPitch = 0.6f;

        [Header("Post-Processing (optional)")]
        [SerializeField] private Volume bulletTimeVolume;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Update()
        {
            bool bt = ShipHealth.Current != null
                      && ShipHealth.Current.IsLowHealth
                      && !ShipHealth.Current.IsDead;

            float targetScale = bt ? slowTimeScale : 1f;
            float targetPitch = bt ? slowPitch     : 1f;

            Time.timeScale      = Mathf.MoveTowards(Time.timeScale,
                                      targetScale, transitionSpeed * Time.unscaledDeltaTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // Optional: pitch-shift a looping music/ambient source
            if (musicSource != null)
                musicSource.pitch = Mathf.MoveTowards(musicSource.pitch,
                                        targetPitch, transitionSpeed * Time.unscaledDeltaTime);

            if (bulletTimeVolume != null)
                bulletTimeVolume.weight = Mathf.MoveTowards(bulletTimeVolume.weight,
                                              bt ? 1f : 0f, transitionSpeed * Time.unscaledDeltaTime);
        }

        void OnDestroy()
        {
            Time.timeScale      = 1f;
            Time.fixedDeltaTime = 0.02f;
            if (musicSource != null) musicSource.pitch = 1f;
        }
    }
}
