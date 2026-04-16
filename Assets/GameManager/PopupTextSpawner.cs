// PopupTextSpawner.cs
// Singleton manager. Call PopupTextSpawner.Instance.Show(text, pos, color)
// from anywhere to create a floating TMP popup.
//
// Inspector setup:
//   popupPrefab → a prefab with TextMeshPro + PopupText components
//   sortingLayer, sortingOrder → make sure text renders above everything

using UnityEngine;

namespace GameManager
{
    public class PopupTextSpawner : MonoBehaviour
    {
        public static PopupTextSpawner Instance { get; private set; }

        [Header("Popup Prefab")]
        [Tooltip("Prefab must have TextMeshPro + PopupText components.")]
        [SerializeField] private GameObject popupPrefab;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Spawn a floating popup at world position <paramref name="pos"/>.
        /// Safe to call even if no prefab is assigned (degrades to a debug log).
        /// </summary>
        public void Show(string text, Vector3 pos, Color color)
        {
            if (popupPrefab == null)
            {
#if UNITY_EDITOR
                Debug.Log($"[PopupText] {text}  @{pos}");
#endif
                return;
            }

            GameObject go = Instantiate(popupPrefab, pos, Quaternion.identity);
            if (go.TryGetComponent(out PopupText pt))
                pt.Play(text, color);
        }

        // Overload with default white colour
        public void Show(string text, Vector3 pos) => Show(text, pos, Color.white);
    }
}

