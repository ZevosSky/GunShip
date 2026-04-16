// PopupText.cs — animated world-space TMP text popup.
// Punch-in scale → EaseOutCubic rise → fade out.
// Uses unscaled time so it works during bullet time.

using System.Collections;
using UnityEngine;
using TMPro;

namespace GameManager
{
    [RequireComponent(typeof(TextMeshPro))]
    public class PopupText : MonoBehaviour
    {
        [SerializeField] private float riseDuration   = 1.4f;
        [SerializeField] private float riseDistance   = 2.5f;
        [SerializeField] private float fontSize       = 3f;
        [SerializeField] private float scalePopTime   = 0.12f;

        private TextMeshPro _tmp;

        void Awake() => _tmp = GetComponent<TextMeshPro>();

        public void Play(string text, Color color)
        {
            _tmp.text     = text;
            _tmp.color    = color;
            _tmp.fontSize = fontSize;
            StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            Vector3 startPos = transform.position;

            // Punch-in scale
            float t = 0f;
            while (t < scalePopTime)
            {
                t += Time.unscaledDeltaTime;
                transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, t / scalePopTime);
                yield return null;
            }
            transform.localScale = Vector3.one;

            // Rise and fade
            t = 0f;
            Color c = _tmp.color;
            while (t < riseDuration)
            {
                t += Time.unscaledDeltaTime;
                float ratio = t / riseDuration;
                float ease  = 1f - Mathf.Pow(1f - ratio, 3f);        // EaseOutCubic
                transform.position = startPos + Vector3.up * (riseDistance * ease);
                c.a = Mathf.Clamp01(1f - Mathf.Max(0f, ratio - 0.5f) * 2f);
                _tmp.color = c;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

