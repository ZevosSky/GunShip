// BoundaryRenderer.cs
// Draws a visible rectangle around the torus world boundary using a LineRenderer.

using UnityEngine;

namespace World
{
    [RequireComponent(typeof(LineRenderer))]
    public class BoundaryRenderer : MonoBehaviour
    {
        [SerializeField] private TorusWorld world;
        [SerializeField] private Color  lineColor     = new Color(0.3f, 0.6f, 1f, 0.7f);
        [SerializeField] private float  lineWidth     = 0.4f;
        [SerializeField] private float  pulseSpeed    = 1.5f;
        [SerializeField] private float  pulseAmplitude = 0.3f;

        private LineRenderer _lr;

        void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.useWorldSpace  = true;
            _lr.loop           = true;
            _lr.positionCount  = 4;
            _lr.startWidth     = lineWidth;
            _lr.endWidth       = lineWidth;
            _lr.startColor     = lineColor;
            _lr.endColor       = lineColor;
            _lr.sortingOrder   = 10;

            if (world != null) DrawBoundary();
        }

        void DrawBoundary()
        {
            float w = world.width;
            float h = world.height;
            _lr.SetPosition(0, new Vector3(0f, 0f, 0f));
            _lr.SetPosition(1, new Vector3(w,  0f, 0f));
            _lr.SetPosition(2, new Vector3(w,  h,  0f));
            _lr.SetPosition(3, new Vector3(0f, h,  0f));
        }

        void Update()
        {
            // Pulse the alpha to make the boundary noticeable
            float a = lineColor.a + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            Color c = lineColor;
            c.a = Mathf.Clamp01(a);
            _lr.startColor = c;
            _lr.endColor   = c;
        }
    }
}

