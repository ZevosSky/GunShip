//==============================================================================
// @Author:   Gary Yang
// @File:     PlanetBackground.cs
// @brief:    Drives the orbital-view illusion.
//            Each frame the script maps the camera's torus-world position to
//            a UV offset and pushes it to the PlanetSurface material so the
//            correct patch of planet scrolls into view as the ship moves.
//
// ── SCENE SETUP ──────────────────────────────────────────────────────────────
//
//  1.  Create a Sphere GameObject (planet).
//        • Position  : (0, 0,  100)   — any large positive Z, behind sprites
//        • Scale     : fill ~80 % of camera viewport in Y
//                      e.g. if Camera.orthographicSize = 5  ->  scale ≈ (9,9,9)
//        • Material  : new Material using Custom/PlanetSurface
//        • Assign a seamless/tileable planet texture to _MainTex
//
//  2.  Create a second Sphere (atmosphere).
//        • Same position as planet sphere
//        • Scale     : ~1.06× the planet sphere  (e.g. (9.54, 9.54, 9.54))
//        • Material  : new Material using Custom/AtmosphereRim
//        • Tune _GlowColor, _RimPower, _RimStrength in the material inspector
//
//  3.  Add this component (PlanetBackground) to any persistent GameObject
//      (e.g. the Camera, or a dedicated "Background" object).
//        • Assign the TorusWorld SO, the TorusCamera, and both MeshRenderers.
//
//  4.  Script Execution Order (optional but clean):
//        Project Settings -> Script Execution Order
//        Set PlanetBackground to 100  (runs after TorusCamera = 0)
//
// ── TUNING ───────────────────────────────────────────────────────────────────
//
//  parallaxScale  1.0 = planet scrolls at exactly 1:1 with camera movement.
//                 This is correct for a "directly overhead" view.
//                 Reduce slightly (<1) for a subtle deep-space parallax feel.
//
//  Planet sphere scale  ->  larger = lower perceived orbit altitude
//                           smaller = higher orbit altitude / more of the planet
//                           fits in view
//==============================================================================

using UnityEngine;

namespace World
{
    // Runs after TorusCamera (default order = 0) so _camPos is already set.
    [DefaultExecutionOrder(100)]
    public class PlanetBackground : MonoBehaviour
    {
        //===| inspector |===========================================================
        [Header("References")]
        [SerializeField] private TorusWorld  world;
        [SerializeField] private TorusCamera torusCamera;

        [Header("Sphere Renderers")]
        [SerializeField] private MeshRenderer planetRenderer;
        [SerializeField] private MeshRenderer atmosphereRenderer;  // optional

        [Header("Scroll")]
        [Tooltip("1 = planet scrolls 1:1 with camera movement (correct overhead view).\n" +
                 "Lower values add a subtle parallax lag.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float parallaxScale = 1f;

        //===| private |=============================================================
        private Material _planetMatInst;
        private Material _atmosphereMatInst;

        private static readonly int PropCamUVOffset = Shader.PropertyToID("_CamUVOffset");

        #region Unity Life Time Functions 
        private void Awake()
        {
            // .material creates a per-instance copy — we own it and must destroy it.
            if (planetRenderer     != null) _planetMatInst     = planetRenderer.material;
            if (atmosphereRenderer != null) _atmosphereMatInst = atmosphereRenderer.material;
        }

        private void LateUpdate()
        {
            if (world == null || torusCamera == null) return;

            // CamWorldPos is the raw unwrapped torus position, updated by
            // TorusCamera before this script runs (execution order 0 vs 100).
            Vector2 cam = torusCamera.CamWorldPos;

            //---| Lock spheres to camera XY |----------------------------------------|
            // The spheres must follow the camera in world-space XY every frame,
            // otherwise they sit at a fixed world position and scroll off-screen
            // as the ship moves around the torus. Z is preserved from the prefab
            // so they stay behind all sprites.
            if (planetRenderer != null)
            {
                var p = planetRenderer.transform.position;
                planetRenderer.transform.position = new Vector3(cam.x, cam.y, p.z);
            }
            if (atmosphereRenderer != null)
            {
                var p = atmosphereRenderer.transform.position;
                atmosphereRenderer.transform.position = new Vector3(cam.x, cam.y, p.z);
            }

            //===| Sphere rotation (warp around the actual sphere) |===============
            // Physically rotating the sphere is the correct way to move planet
            // terrain — not a flat UV scroll.  Two key benefits:
            // Sign conventions (camera looks in +Z, planet at positive Z):
            //   Camera moves right (+X)  ->  yaw   negative  ->  right face comes
            //                               forward  ->  terrain scrolls left  
            //   Camera moves up   (+Y)  ->  pitch  negative  ->  top  face comes
            //                               forward  ->  terrain scrolls down  
            float yaw   = -(cam.x / world.width) * 360f * parallaxScale;
            float pitch = -(cam.y / world.width) * 360f * parallaxScale;

            // AngleAxis pins each rotation to a fixed world-space axis so the
            // two never interfere.  Quaternion.Euler(pitch, yaw, 0) applies pitch
            // around the already-yawed local X axis, which flips the apparent Y
            // direction on diagonal movement — this avoids that entirely.
            Quaternion sphereRot = Quaternion.AngleAxis(pitch, Vector3.right)
                                 * Quaternion.AngleAxis(yaw,   Vector3.up);

            if (planetRenderer     != null) planetRenderer    .transform.rotation = sphereRot;
            if (atmosphereRenderer != null) atmosphereRenderer.transform.rotation = sphereRot;

            // UV offset is zeroed — sphere rotation is now the sole driver of
            // terrain movement.  Sending both would double-scroll the surface.
            // (The _CamUVOffset property still exists in the shaders should you
            // ever want to add a manual fine-tune offset on top of rotation.)
            var offset = Vector4.zero;

            _planetMatInst?    .SetVector(PropCamUVOffset, offset);
            _atmosphereMatInst?.SetVector(PropCamUVOffset, offset);
        }

        private void OnDestroy()
        {
            if (_planetMatInst     != null) Destroy(_planetMatInst);
            if (_atmosphereMatInst != null) Destroy(_atmosphereMatInst);
        }
        
        #endregion // Unity Functions 

#if UNITY_EDITOR
        // ── editor gizmos ─────────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            if (planetRenderer == null) return;
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.3f);
            Gizmos.DrawWireSphere(planetRenderer.transform.position,
                                  planetRenderer.transform.lossyScale.x * 0.5f);
        }
#endif
    }
}

