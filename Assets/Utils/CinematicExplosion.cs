// CinematicExplosion.cs
// Drop anywhere in the scene and call Play() (or enable the component) to trigger
// a scripted multi-stage explosion sequence:
//
//   1. Time slows to slowMoScale for the whole pre-climax phase.
//   2. Several small explosions fire at random positions inside the radius,
//      each with its own screen shake.
//   3. A big climactic explosion spawns at the centre — fills the full radius,
//      restores time, shakes the camera hard, and flashes the screen white.
//   4. The screen flash fades out over flashFadeDuration.
//
// Assign:
//   - smallExplosionPrefab : any prefab with ExplosionRing / ExplosionController / particles
//   - bigExplosionPrefab   : same (will be scaled up to match bigExplosionRadius)
//   - flashImage           : a full-screen UI Image set to stretch + max alpha 0 at start
//                            (create a Canvas → Image, set color White, Raycast off)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using World;

namespace Utils
{
    public class CinematicExplosion : MonoBehaviour
    {
        // ── Small explosions ───────────────────────────────────────────────
        [Header("Small Explosions")]
        [Tooltip("Prefab spawned for each small bang (e.g. your ExplosionRing prefab)")]
        [SerializeField] private GameObject smallExplosionPrefab;

        [Tooltip("How many small explosions to fire before the climax")]
        [SerializeField] [Min(1)] private int smallCount = 8;

        [Tooltip("Radius of the spawn area for small explosions")]
        [SerializeField] private float spawnRadius = 6f;

        [Tooltip("Scale applied to each small explosion prefab")]
        [SerializeField] private float smallScale = 1f;

        [Tooltip("Min/max seconds between successive small explosions")]
        [SerializeField] private Vector2 smallInterval = new Vector2(0.08f, 0.25f);

        [Header("Small Shake")]
        [SerializeField] private float smallShakeDuration  = 0.15f;
        [SerializeField] private float smallShakeMagnitude = 0.2f;

        // ── Big explosion ──────────────────────────────────────────────────
        [Header("Big Explosion")]
        [Tooltip("Prefab spawned for the climactic bang")]
        [SerializeField] private GameObject bigExplosionPrefab;

        [Tooltip("Radius the big explosion prefab should fill " +
                 "(the prefab is scaled so its ExplosionRing targetRadius matches this)")]
        [SerializeField] private float bigExplosionRadius = 10f;

        [Tooltip("Delay after the last small explosion before the big one fires")]
        [SerializeField] private float preCliMaxPause = 0.3f;

        [Header("Big Shake")]
        [SerializeField] private float bigShakeDuration  = 0.6f;
        [SerializeField] private float bigShakeMagnitude = 1.2f;

        // ── Slow motion ────────────────────────────────────────────────────
        [Header("Slow Motion")]
        [Tooltip("Time scale during the pre-climax phase (0.1 = 10% speed)")]
        [SerializeField] [Range(0.01f, 1f)] private float slowMoScale    = 0.25f;
        [Tooltip("How quickly time snaps back to normal after the big explosion (seconds, unscaled)")]
        [SerializeField] private float timeRestoreDuration = 0.5f;

        // ── Screen flash ───────────────────────────────────────────────────
        [Header("Screen Flash")]
        [Tooltip("How bright the flash peaks (0–1 alpha)")]
        [SerializeField] [Range(0f, 1f)] private float flashPeakAlpha = 1f;

        [Tooltip("How long the flash takes to fade back to transparent (unscaled seconds)")]
        [SerializeField] private float flashFadeDuration = 1.2f;

        [Tooltip("Color of the flash (default white)")]
        [SerializeField] private Color flashColor = Color.white;

        // Created at runtime — no scene setup needed
        private Image _flashImage;

        // ── Playback ───────────────────────────────────────────────────────
        [Header("Playback")]
        [Tooltip("If true the sequence starts automatically when the component is enabled")]
        [SerializeField] private bool playOnEnable = true;

        [Tooltip("If true, destroy this GameObject once the sequence finishes")]
        [SerializeField] private bool destroyWhenDone = false;

        /// <summary>True while the sequence is running.</summary>
        public bool IsPlaying { get; private set; }

        void Awake()
        {
            BuildFlashOverlay();
        }

        void BuildFlashOverlay()
        {
            // Reuse any existing overlay canvas tagged for us, otherwise create one
            var canvasGO = new GameObject("CinematicExplosion_FlashCanvas");
            canvasGO.transform.SetParent(transform);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;          // above everything else
            canvasGO.AddComponent<CanvasScaler>();

            var imgGO = new GameObject("FlashImage");
            imgGO.transform.SetParent(canvasGO.transform, false);

            _flashImage       = imgGO.AddComponent<Image>();
            Color c           = flashColor;
            c.a               = 0f;
            _flashImage.color = c;
            _flashImage.raycastTarget = false;

            // Stretch to fill the whole screen
            var rt          = imgGO.GetComponent<RectTransform>();
            rt.anchorMin    = Vector2.zero;
            rt.anchorMax    = Vector2.one;
            rt.offsetMin    = Vector2.zero;
            rt.offsetMax    = Vector2.zero;
        }

        // ── Unity events ───────────────────────────────────────────────────

        void OnEnable()
        {
            if (playOnEnable) Play();
        }

        void OnDisable()
        {
            // Safety: restore time if we're killed mid-sequence
            if (IsPlaying) Time.timeScale = 1f;
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>Kick off the cinematic explosion sequence at this transform's position.</summary>
        public void Play() => StartCoroutine(Sequence());

        /// <summary>Play at an arbitrary world position.</summary>
        public void PlayAt(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            StartCoroutine(Sequence());
        }

        // ── Sequence ───────────────────────────────────────────────────────

        IEnumerator Sequence()
        {
            if (IsPlaying) yield break;
            IsPlaying = true;

            Vector3 centre = transform.position;

            // 1. Enter slow-mo
            Time.timeScale         = slowMoScale;
            Time.fixedDeltaTime    = 0.02f * slowMoScale;

            // 2. Fire small explosions
            for (int i = 0; i < smallCount; i++)
            {
                SpawnSmall(centre);
                ScreenShake.Instance?.Shake(smallShakeDuration, smallShakeMagnitude);

                float interval = Random.Range(smallInterval.x, smallInterval.y);
                // yield in unscaled time so interval feels the same regardless of slow-mo
                yield return new WaitForSecondsRealtime(interval * slowMoScale);
            }

            // Brief pause before climax (unscaled so it's consistent)
            yield return new WaitForSecondsRealtime(preCliMaxPause);

            // 3. Big explosion — restore time first for the hit to feel impactful
            StartCoroutine(RestoreTime());

            SpawnBig(centre);
            ScreenShake.Instance?.Shake(bigShakeDuration, bigShakeMagnitude);

            // 4. Screen flash
            StartCoroutine(FlashScreen());

            // Wait for shake + flash to finish, then we're done
            float waitTime = Mathf.Max(bigShakeDuration, flashFadeDuration, timeRestoreDuration);
            yield return new WaitForSecondsRealtime(waitTime);

            IsPlaying = false;

            if (destroyWhenDone) Destroy(gameObject);
        }

        // ── Spawn helpers ──────────────────────────────────────────────────

        void SpawnSmall(Vector3 centre)
        {
            if (smallExplosionPrefab == null) return;

            Vector2 offset  = Random.insideUnitCircle * spawnRadius;
            Vector3 pos     = centre + new Vector3(offset.x, offset.y, 0f);
            var     go      = Instantiate(smallExplosionPrefab, pos, Quaternion.identity);
            go.transform.localScale = Vector3.one * smallScale;
        }

        void SpawnBig(Vector3 centre)
        {
            if (bigExplosionPrefab == null) return;

            var go = Instantiate(bigExplosionPrefab, centre, Quaternion.identity);

            // Auto-scale so an ExplosionRing's targetRadius fills bigExplosionRadius.
            // If the prefab has an ExplosionRing, its targetRadius is baked into the
            // prefab's default scale (radius=1 → scale=2 diameter). We compute the
            // multiplier needed to hit bigExplosionRadius.
            var ring = go.GetComponent<ExplosionRing>();
            if (ring != null)
            {
                // ExplosionRing sets scale = targetRadius * 2 at full expand.
                // We want final world-units diameter = bigExplosionRadius * 2,
                // so the local scale multiplier = bigExplosionRadius / targetRadius.
                float prefabRadius = GetExplosionRingRadius(ring);
                if (prefabRadius > 0f)
                    go.transform.localScale = Vector3.one * (bigExplosionRadius / prefabRadius);
            }
            else
            {
                // Fallback: just scale the prefab directly
                go.transform.localScale = Vector3.one * bigExplosionRadius;
            }
        }

        // Reads the serialized targetRadius field via reflection so we don't need to
        // make the field public in ExplosionRing.
        static float GetExplosionRingRadius(ExplosionRing ring)
        {
            var field = typeof(ExplosionRing).GetField("targetRadius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (float)field.GetValue(ring) : 1f;
        }

        // ── Time restore ───────────────────────────────────────────────────

        IEnumerator RestoreTime()
        {
            float start   = Time.timeScale;
            float elapsed = 0f;

            while (elapsed < timeRestoreDuration)
            {
                elapsed              += Time.unscaledDeltaTime;
                float t               = Mathf.Clamp01(elapsed / timeRestoreDuration);
                Time.timeScale        = Mathf.Lerp(start, 1f, t);
                Time.fixedDeltaTime   = 0.02f * Time.timeScale;
                yield return null;
            }

            Time.timeScale      = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        // ── Screen flash ───────────────────────────────────────────────────

        IEnumerator FlashScreen()
        {
            if (_flashImage == null) yield break;

            // Instant peak
            SetFlashAlpha(flashPeakAlpha);

            float elapsed = 0f;
            while (elapsed < flashFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = Mathf.Clamp01(elapsed / flashFadeDuration);
                SetFlashAlpha(Mathf.Lerp(flashPeakAlpha, 0f, t));
                yield return null;
            }

            SetFlashAlpha(0f);
        }

        void SetFlashAlpha(float a)
        {
            if (_flashImage == null) return;
            Color c = _flashImage.color;
            c.a = a;
            _flashImage.color = c;
        }

        // ── Gizmos ────────────────────────────────────────────────────────

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // Small spawn zone
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.25f);
            DrawWireCircle(transform.position, spawnRadius, 48);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.6f);
            DrawWireCircle(transform.position, spawnRadius, 48, solid: false);

            // Big explosion radius
            Gizmos.color = new Color(1f, 0.15f, 0.05f, 0.15f);
            DrawWireCircle(transform.position, bigExplosionRadius, 48);
            Gizmos.color = new Color(1f, 0.15f, 0.05f, 0.7f);
            DrawWireCircle(transform.position, bigExplosionRadius, 48, solid: false);

            // Labels via handles
            UnityEditor.Handles.color = new Color(1f, 0.6f, 0.1f);
            UnityEditor.Handles.Label(transform.position + Vector3.right * spawnRadius,
                                      " spawn zone");
            UnityEditor.Handles.color = new Color(1f, 0.2f, 0.05f);
            UnityEditor.Handles.Label(transform.position + Vector3.right * bigExplosionRadius,
                                      " big explosion");
        }

        static void DrawWireCircle(Vector3 centre, float radius, int segs, bool solid = true)
        {
            if (solid)
            {
                Gizmos.DrawSphere(centre, radius);
                return;
            }
            float step = Mathf.PI * 2f / segs;
            for (int i = 0; i < segs; i++)
            {
                float a0 = step * i, a1 = step * (i + 1);
                Gizmos.DrawLine(
                    centre + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * radius,
                    centre + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * radius);
            }
        }
#endif
    }
}





