// PlayerHUD.cs
// Draws a health bar + numeric readout in the bottom-left of the screen.
// Creates its own Canvas at runtime — just add this component to any scene GO.
// Automatically tracks ShipHealth.Current so it updates on ship switch.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RocketShip;

namespace GameManager
{
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Vector2 barSize        = new Vector2(220f, 22f);
        [SerializeField] private Vector2 barAnchor      = new Vector2(20f,  20f);  // px from bottom-left

        [Header("Colours")]
        [SerializeField] private Color fullColor  = new Color(0.15f, 0.85f, 0.25f);
        [SerializeField] private Color lowColor   = new Color(0.95f, 0.20f, 0.20f);
        [SerializeField] private Color bgColor    = new Color(0.05f, 0.05f, 0.05f, 0.75f);

        // ── Runtime refs ─────────────────────────────────────────────────
        private ShipHealth  _tracked;
        private Image       _fillImage;
        private Image       _bgImage;
        private TextMeshProUGUI _label;

        // ── Build UI on Awake ─────────────────────────────────────────────
        void Awake()
        {
            BuildCanvas();
        }

        void BuildCanvas()
        {
            // Root canvas
            var canvasGO = new GameObject("PlayerHUD_Canvas");
            canvasGO.transform.SetParent(transform);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Background bar
            var bgGO  = new GameObject("HP_BG");
            bgGO.transform.SetParent(canvasGO.transform, false);
            _bgImage  = bgGO.AddComponent<Image>();
            _bgImage.color = bgColor;

            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = bgRect.anchorMax = bgRect.pivot = Vector2.zero;
            bgRect.anchoredPosition = barAnchor - new Vector2(2f, 2f);
            bgRect.sizeDelta        = barSize   + new Vector2(4f, 4f);

            // Fill bar
            var fillGO  = new GameObject("HP_Fill");
            fillGO.transform.SetParent(canvasGO.transform, false);
            _fillImage  = fillGO.AddComponent<Image>();
            _fillImage.color = fullColor;

            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = fillRect.anchorMax = fillRect.pivot = Vector2.zero;
            fillRect.anchoredPosition = barAnchor;
            fillRect.sizeDelta        = barSize;

            // Label — "HP  85 / 100"
            var labelGO = new GameObject("HP_Label");
            labelGO.transform.SetParent(canvasGO.transform, false);
            _label = labelGO.AddComponent<TextMeshProUGUI>();
            _label.fontSize  = 13f;
            _label.color     = Color.white;
            _label.alignment = TextAlignmentOptions.MidlineLeft;

            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = Vector2.zero;
            labelRect.anchoredPosition = barAnchor + new Vector2(6f, 0f);
            labelRect.sizeDelta        = barSize;
        }

        // ── Poll ShipHealth.Current each frame (changes on ship switch) ───
        void Update()
        {
            var current = ShipHealth.Current;

            // Subscribe to the new ship if it changed
            if (current != _tracked)
            {
                if (_tracked != null)
                    _tracked.OnHealthChanged -= OnHealthChanged;

                _tracked = current;

                if (_tracked != null)
                    _tracked.OnHealthChanged += OnHealthChanged;

                Refresh(_tracked != null ? _tracked.HealthRatio : 1f,
                        _tracked);
            }
        }

        void OnHealthChanged(float ratio) => Refresh(ratio, _tracked);

        void Refresh(float ratio, ShipHealth sh)
        {
            if (_fillImage == null) return;

            // Scale fill bar width
            var fillRect = _fillImage.GetComponent<RectTransform>();
            fillRect.sizeDelta = new Vector2(barSize.x * Mathf.Clamp01(ratio), barSize.y);

            // Colour: lerp green → red
            _fillImage.color = Color.Lerp(lowColor, fullColor, ratio);

            // Label
            if (_label != null && sh != null)
                _label.text = $"HP  {Mathf.CeilToInt(sh.CurrentHealth)} / {Mathf.RoundToInt(sh.CurrentHealth / Mathf.Max(ratio, 0.001f))}";
            else if (_label != null)
                _label.text = "HP  --";
        }

        void OnDestroy()
        {
            if (_tracked != null)
                _tracked.OnHealthChanged -= OnHealthChanged;
        }
    }
}

