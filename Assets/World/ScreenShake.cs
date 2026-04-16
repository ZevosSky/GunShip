// ScreenShake.cs — singleton camera shake.
// TorusCamera reads CurrentOffset in LateUpdate (after this Update runs).

using UnityEngine;

namespace World
{
    public class ScreenShake : MonoBehaviour
    {
        public static ScreenShake Instance { get; private set; }

        public Vector2 CurrentOffset { get; private set; }

        private float _mag;
        private float _timer;
        private float _duration;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>Start a screen shake. magnitude is in world units.</summary>
        public void Shake(float duration, float magnitude)
        {
            _duration = duration;
            _timer    = duration;
            _mag      = magnitude;
        }

        void Update()
        {
            if (_timer > 0f)
            {
                _timer -= Time.unscaledDeltaTime;
                float t   = _timer / Mathf.Max(_duration, 0.001f);
                float mag = _mag * t;
                CurrentOffset = new Vector2(Random.Range(-mag, mag), Random.Range(-mag, mag));
            }
            else
            {
                CurrentOffset = Vector2.zero;
            }
        }
    }
}

