//==============================================================================
// @File:   PlanetSetupEditor.cs
// @brief:  One-click scene setup for the orbital planet background.
//
//  Menu:  Tools ▶ Setup Planet Background
//
//  What it does
//  ─────────────
//  1. Finds the TorusCamera in the active scene.
//  2. Creates / reuses PlanetSurface.mat and AtmosphereRim.mat
//     in Assets/World/Materials/.
//  3. Creates PlanetSphere + AtmosphereSphere GameObjects, scaled to fit
//     the camera's orthographic size.
//  4. Adds PlanetBackground to the camera and wires all references.
//  5. Marks the scene dirty so you just hit Ctrl+S to save.
//==============================================================================

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using World;

namespace World.Editor
{
    public static class PlanetSetupEditor
    {
        private const string PlanetShaderName    = "Custom/PlanetSurface";
        private const string AtmosphereShaderName = "Custom/AtmosphereRim";
        private const string MatFolder           = "Assets/World/Materials";

        [MenuItem("Tools/Setup Planet Background")]
        public static void SetupPlanetBackground()
        {
            // ── 1. Find TorusCamera ───────────────────────────────────────────
            var torusCamera = Object.FindFirstObjectByType<TorusCamera>();
            if (torusCamera == null)
            {
                Debug.LogError("[PlanetSetup] No TorusCamera found in the active scene.");
                return;
            }

            var cam      = torusCamera.GetComponent<Camera>();
            float ortho  = cam != null ? cam.orthographicSize : 5f;
            var world    = torusCamera.world;

            // ── 2. Shaders ────────────────────────────────────────────────────
            var planetShader = Shader.Find(PlanetShaderName);
            if (planetShader == null)
            {
                Debug.LogError("[PlanetSetup] Shader 'Custom/PlanetSurface' not found. " +
                               "Make sure Unity has imported the shader (check Assets/World/Shaders/).");
                return;
            }

            var atmosphereShader = Shader.Find(AtmosphereShaderName);
            if (atmosphereShader == null)
            {
                Debug.LogError("[PlanetSetup] Shader 'Custom/AtmosphereRim' not found.");
                return;
            }

            // ── 3. Materials ──────────────────────────────────────────────────
            if (!AssetDatabase.IsValidFolder(MatFolder))
                AssetDatabase.CreateFolder("Assets/World", "Materials");

            var planetMat = GetOrCreateMaterial(MatFolder + "/PlanetProcedural.mat", planetShader, mat =>
            {
                // Generation
                mat.SetFloat("_Seed",       0f);
                mat.SetFloat("_NoiseScale", 2.5f);
                mat.SetFloat("_WaterLevel", 0.50f);
                // Biome colours
                mat.SetColor("_DeepOceanColor",    new Color(0.04f, 0.10f, 0.30f));
                mat.SetColor("_ShallowOceanColor", new Color(0.08f, 0.28f, 0.55f));
                mat.SetColor("_CoastColor",        new Color(0.76f, 0.70f, 0.50f));
                mat.SetColor("_LowlandColor",      new Color(0.22f, 0.50f, 0.16f));
                mat.SetColor("_HighlandColor",     new Color(0.33f, 0.28f, 0.16f));
                mat.SetColor("_MountainColor",     new Color(0.50f, 0.46f, 0.42f));
                mat.SetColor("_SnowColor",         new Color(0.92f, 0.95f, 1.00f));
                // Atmosphere
                mat.SetColor("_AtmosphereColor",    new Color(0.25f, 0.55f, 1.0f));
                mat.SetFloat("_AtmosphereStrength", 1.2f);
                mat.SetFloat("_AtmospherePower",    3.5f);
                // Lighting
                mat.SetVector("_SunDir",      new Vector4(1f, 0.5f, -2f, 0f));
                mat.SetFloat("_AmbientLight", 0.12f);
            });

            var atmosphereMat = GetOrCreateMaterial(MatFolder + "/AtmosphereRim.mat", atmosphereShader, mat =>
            {
                mat.SetColor("_GlowColor",    new Color(0.3f, 0.65f, 1.0f, 1.0f));
                mat.SetFloat("_SurfaceFade",  3.0f);   // peak at planet-atmosphere boundary
                mat.SetFloat("_RimPower",     1.0f);   // soft fade into space
                mat.SetFloat("_RimStrength",  0.85f);  // normalised, so 1.0 = full intensity
            });

            AssetDatabase.SaveAssets();

            // ── 4. Camera background → space black ────────────────────────────
            // The corners outside the planet sphere should look like space, not sky.
            if (cam != null)
            {
                cam.backgroundColor = new Color(0.0f, 0.0f, 0.02f, 1f);
                EditorUtility.SetDirty(cam);
            }

            // ── 5. Sphere scale ───────────────────────────────────────────────
            // Unity's Sphere primitive has local radius 0.5, so scale == diameter.
            //
            // Planet fills ~90 % of screen height → you see it as a large disc
            // with dark space visible in the corners (16:9).
            //
            // Atmosphere is 1.5× the planet so its halo extends well beyond the
            // planet's edge — the previous 1.06× was only 3 % per side, invisible.
            float screenH      = ortho * 2f;                  // e.g. 10 units
            float planetDia    = screenH * 0.9f;              // e.g. 9.0 units
            float atmosphereDia = planetDia * 1.5f;           // e.g. 13.5 units

            // ── 6. Planet sphere ──────────────────────────────────────────────
            var planetObj = CreateOrFindSphere("PlanetSphere",
                                               new Vector3(0f, 0f, 100f),
                                               planetDia, planetMat,
                                               sortingOrder: -100);

            // ── 7. Atmosphere sphere ──────────────────────────────────────────
            // Z = 99 (1 unit closer than planet) so its fragments always pass the
            // depth test on top of the planet surface — avoids z-fighting.
            var atmObj = CreateOrFindSphere("AtmosphereSphere",
                                            new Vector3(0f, 0f, 99f),
                                            atmosphereDia, atmosphereMat,
                                            sortingOrder: -99);

            // ── 8. PlanetBackground component ─────────────────────────────────
            var pb = torusCamera.GetComponent<PlanetBackground>()
                  ?? torusCamera.gameObject.AddComponent<PlanetBackground>();

            // SerializedObject lets us write to private [SerializeField] fields.
            var so = new SerializedObject(pb);
            so.FindProperty("world")               .objectReferenceValue = world;
            so.FindProperty("torusCamera")         .objectReferenceValue = torusCamera;
            so.FindProperty("planetRenderer")      .objectReferenceValue = planetObj.GetComponent<MeshRenderer>();
            so.FindProperty("atmosphereRenderer")  .objectReferenceValue = atmObj.GetComponent<MeshRenderer>();
            so.FindProperty("parallaxScale")       .floatValue           = 1f;
            so.ApplyModifiedProperties();

            // ── 9. Done ───────────────────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log("[PlanetSetup] ✓ Done!\n" +
                      "  • PlanetSphere      (scale " + planetDia.ToString("F2") + ")\n" +
                      "  • AtmosphereSphere  (scale " + atmosphereDia.ToString("F2") + ")\n" +
                      "  • PlanetBackground  added to " + torusCamera.gameObject.name + "\n\n" +
                      "Tune the planet in the Inspector:\n" +
                      "  " + MatFolder + "/PlanetProcedural.mat\n" +
                      "    _Seed        — new value = new planet\n" +
                      "    _WaterLevel  — 0.3 land-heavy, 0.7 ocean world\n" +
                      "    _NoiseScale  — lower = bigger continents\n" +
                      "Then hit Ctrl+S to save the scene.");
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static Material GetOrCreateMaterial(string path, Shader shader,
                                                     System.Action<Material> initialise)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(mat, path);
            }

            // Always re-apply properties so re-running the tool refreshes values.
            // _MainTex and any user-assigned textures are NOT touched here.
            initialise(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject CreateOrFindSphere(string name, Vector3 position,
                                                      float diameter, Material mat,
                                                      int sortingOrder = 0)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = name;
                // No physics needed on a background decoration.
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            go.transform.position   = position;
            go.transform.localScale = Vector3.one * diameter;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            // Negative order → renders behind Default/0 sprites in the 2D sort.
            mr.sortingOrder   = sortingOrder;
            return go;
        }
    }
}


