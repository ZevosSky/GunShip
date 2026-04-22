using System;
using System.Collections.Generic;
using UnityEngine;

namespace World
{
    [DefaultExecutionOrder(100)]
    public class PlanetBackground : MonoBehaviour
    {
        private enum BodyKind
        {
            Stars,
            Asteroids,
            Planets
        }

        [Serializable]
        private sealed class LayerSettings
        {
            [Tooltip("Display name for this procedural background layer.")]
            public string name = "Layer";

            [Tooltip("What this layer spawns: stars, asteroid sprites, or additional planet bodies.")]
            public BodyKind kind = BodyKind.Stars;

            [Tooltip("How many bodies this layer keeps alive around the camera.")]
            [Min(0)] public int count = 16;

            [Tooltip("How strongly this layer follows camera movement. Lower values feel farther away.")]
            [Range(0.02f, 1f)] public float parallax = 0.1f;

            [Tooltip("Minimum and maximum spawned size for bodies in this layer.")]
            public Vector2 scaleRange = new(0.25f, 1f);

            [Tooltip("Minimum and maximum drift speed. Useful for slow asteroid drift; stars usually stay at zero.")]
            public Vector2 driftSpeedRange = Vector2.zero;

            [Tooltip("Minimum and maximum spin speed in degrees per second. Mostly useful for asteroid sprites.")]
            public Vector2 rotationSpeedRange = Vector2.zero;

            [Tooltip("Start color for random tinting within this layer.")]
            public Color colorA = Color.white;

            [Tooltip("End color for random tinting within this layer.")]
            public Color colorB = Color.white;

            [Tooltip("For planet layers only: multiplier range for the atmosphere shell relative to the planet scale.")]
            public Vector2 atmosphereScaleRange = new(1.08f, 1.16f);

            [Tooltip("Sorting order range used when assigning render priority within this layer.")]
            public Vector2 sortOrderRange = new(-100, -90);

            [Tooltip("World-space Z used for this layer. Larger values push the layer farther behind gameplay sprites.")]
            public float zPosition = 120f;
        }

        private sealed class BackgroundBody
        {
            public Transform Transform;
            public Transform AtmosphereTransform;
            public Vector2 Anchor;
            public Vector2 Drift;
            public float RotationSpeed;
            public float Parallax;
            public float ZPosition;
        }

        [Header("References")]
        [Tooltip("Torus world definition. Used so the component stays aligned with the same world space as the camera.")]
        [SerializeField] private TorusWorld world;

        [Tooltip("Camera controller that provides the unwrapped camera position used to drive parallax.")]
        [SerializeField] private TorusCamera torusCamera;

        [Header("Assigned Hero Planet")]
        [Tooltip("Optional main planet body already placed in the scene. This becomes the single large hero body.")]
        [SerializeField] private MeshRenderer planetRenderer;

        [Tooltip("Optional atmosphere shell paired with the hero planet renderer.")]
        [SerializeField] private MeshRenderer atmosphereRenderer;

        [Tooltip("How much the hero planet lags behind camera movement. Lower values make it feel more distant.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float heroPlanetParallax = 0.12f;

        [Header("Parallax")]
        [Tooltip("Global multiplier across all parallax layers. Increase for stronger movement, decrease for a flatter backdrop.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float parallaxScale = 1f;

        [Header("Generation")]
        [Tooltip("Seed used for deterministic procedural placement and planet material variation.")]
        [SerializeField] private int seed = 438;

        [Tooltip("Extra spawn area around the visible camera extents so bodies do not pop at the screen edge.")]
        [SerializeField] private Vector2 viewPadding = new(12f, 8f);

        [Tooltip("How far beyond the visible extents a body can travel before it gets recycled to the other side.")]
        [Range(1.1f, 3f)]
        [SerializeField] private float recycleMultiplier = 1.45f;

        [Tooltip("Configure the procedural background mix here. This shows on the PlanetBackground component in the inspector.")]
        [SerializeField] private LayerSettings[] layers;

        private static readonly int PropCamUVOffset = Shader.PropertyToID("_CamUVOffset");
        private static readonly int PropSeed = Shader.PropertyToID("_Seed");
        private static readonly int PropNoiseScale = Shader.PropertyToID("_NoiseScale");
        private static readonly int PropWaterLevel = Shader.PropertyToID("_WaterLevel");
        private static readonly int PropSunDir = Shader.PropertyToID("_SunDir");

        private readonly List<BackgroundBody> _bodies = new();
        private readonly List<UnityEngine.Object> _ownedObjects = new();

        private Camera _camera;
        private Transform _runtimeRoot;
        private Sprite _starSprite;
        private Sprite _asteroidSprite;
        private Texture2D _starTexture;
        private Texture2D _asteroidTexture;
        private Material _heroPlanetMaterial;
        private Material _heroAtmosphereMaterial;
        private bool _initialized;

        private void Reset()
        {
            torusCamera = GetComponent<TorusCamera>();
            if (torusCamera != null)
            {
                world = torusCamera.world;
            }
        }

        private void Awake()
        {
            EnsureReferences();
            EnsureLayerDefaults();
            BuildBackground();
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                BuildBackground();
            }

            if (!_initialized || torusCamera == null || _camera == null)
            {
                return;
            }

            Vector2 cam = torusCamera.CamWorldPos;
            float dt = Time.deltaTime;
            Vector2 viewExtents = GetViewExtents() + viewPadding;

            foreach (BackgroundBody body in _bodies)
            {
                body.Anchor += body.Drift * dt;
                RecycleBody(body, cam, viewExtents);

                Vector2 displayOffset = (body.Anchor - cam) * body.Parallax;
                Vector3 position = new(cam.x + displayOffset.x, cam.y + displayOffset.y, body.ZPosition);

                body.Transform.position = position;
                if (body.AtmosphereTransform != null)
                {
                    float atmosphereZ = body.AtmosphereTransform.parent == _runtimeRoot
                        ? body.ZPosition - 1f
                        : body.AtmosphereTransform.position.z;
                    body.AtmosphereTransform.position = new Vector3(position.x, position.y, atmosphereZ);
                }

                if (!Mathf.Approximately(body.RotationSpeed, 0f))
                {
                    body.Transform.Rotate(0f, 0f, body.RotationSpeed * dt, Space.Self);
                }
            }
        }

        private void OnDestroy()
        {
            if (_runtimeRoot != null)
            {
                Destroy(_runtimeRoot.gameObject);
            }

            foreach (UnityEngine.Object ownedObject in _ownedObjects)
            {
                if (ownedObject != null)
                {
                    Destroy(ownedObject);
                }
            }

            _ownedObjects.Clear();
            _bodies.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            EnsureReferences();
            EnsureLayerDefaults();

            Vector2 cam = GetEditorCameraPosition();
            Vector2 viewExtents = GetViewExtents();
            Vector2 paddedExtents = viewExtents + viewPadding;

            DrawRect(cam, viewExtents, new Color(0.22f, 0.75f, 1f, 0.95f));
            DrawRect(cam, paddedExtents, new Color(0.18f, 0.95f, 0.55f, 0.70f));

            if (planetRenderer != null)
            {
                Gizmos.color = new Color(0.30f, 0.65f, 1f, 0.35f);
                Gizmos.DrawWireSphere(planetRenderer.transform.position,
                    planetRenderer.transform.lossyScale.x * 0.5f);
            }

            if (atmosphereRenderer != null)
            {
                Gizmos.color = new Color(0.35f, 0.9f, 1f, 0.20f);
                Gizmos.DrawWireSphere(atmosphereRenderer.transform.position,
                    atmosphereRenderer.transform.lossyScale.x * 0.5f);
            }

            if (layers == null)
            {
                return;
            }

            foreach (LayerSettings layer in layers)
            {
                float parallax = Mathf.Max(layer.parallax * parallaxScale, 0.02f);
                Vector2 rawRecycleExtents = paddedExtents * recycleMultiplier / parallax;
                DrawRect(cam, rawRecycleExtents, GetLayerDebugColor(layer.kind));
            }
        }
#endif

        private void EnsureReferences()
        {
            if (torusCamera == null)
            {
                torusCamera = GetComponent<TorusCamera>();
            }

            if (world == null && torusCamera != null)
            {
                world = torusCamera.world;
            }

            if (_camera == null)
            {
                _camera = torusCamera != null ? torusCamera.GetComponent<Camera>() : Camera.main;
            }
        }

        private void EnsureLayerDefaults()
        {
            if (layers != null && layers.Length > 0)
            {
                foreach (LayerSettings layer in layers)
                {
                    layer.count = Mathf.Max(0, layer.count);
                    layer.parallax = Mathf.Clamp(layer.parallax, 0.02f, 1f);
                    layer.scaleRange = SortRange(layer.scaleRange, 0.01f);
                    layer.driftSpeedRange = SortRange(layer.driftSpeedRange, 0f);
                    layer.rotationSpeedRange = SortRange(layer.rotationSpeedRange);
                    layer.atmosphereScaleRange = SortRange(layer.atmosphereScaleRange, 1f);
                    layer.sortOrderRange = SortRange(layer.sortOrderRange);
                }

                return;
            }

            layers = new[]
            {
                new LayerSettings
                {
                    name = "Far Stars",
                    kind = BodyKind.Stars,
                    count = 96,
                    parallax = 0.04f,
                    scaleRange = new Vector2(0.06f, 0.20f),
                    colorA = new Color(0.55f, 0.70f, 1f, 0.38f),
                    colorB = new Color(1f, 0.96f, 0.78f, 0.95f),
                    sortOrderRange = new Vector2(-260f, -220f),
                    zPosition = 180f
                },
                new LayerSettings
                {
                    name = "Mid Stars",
                    kind = BodyKind.Stars,
                    count = 42,
                    parallax = 0.09f,
                    scaleRange = new Vector2(0.14f, 0.34f),
                    colorA = new Color(0.44f, 0.76f, 1f, 0.20f),
                    colorB = new Color(1f, 0.96f, 0.88f, 0.60f),
                    sortOrderRange = new Vector2(-219f, -200f),
                    zPosition = 175f
                },
                new LayerSettings
                {
                    name = "Planets",
                    kind = BodyKind.Planets,
                    count = 0,
                    parallax = 0.18f,
                    scaleRange = new Vector2(0.9f, 1.8f),
                    driftSpeedRange = new Vector2(0f, 0.02f),
                    colorA = new Color(0.32f, 0.58f, 1f, 1f),
                    colorB = new Color(1f, 0.72f, 0.35f, 1f),
                    atmosphereScaleRange = new Vector2(1.10f, 1.22f),
                    sortOrderRange = new Vector2(-150f, -130f),
                    zPosition = 140f
                },
                new LayerSettings
                {
                    name = "Asteroids",
                    kind = BodyKind.Asteroids,
                    count = 30,
                    parallax = 0.28f,
                    scaleRange = new Vector2(0.35f, 1.20f),
                    driftSpeedRange = new Vector2(0f, 0.08f),
                    rotationSpeedRange = new Vector2(-22f, 22f),
                    colorA = new Color(0.30f, 0.34f, 0.38f, 0.55f),
                    colorB = new Color(0.58f, 0.52f, 0.42f, 0.95f),
                    sortOrderRange = new Vector2(-124f, -105f),
                    zPosition = 110f
                }
            };
        }

        private void BuildBackground()
        {
            EnsureReferences();
            if (torusCamera == null || _camera == null)
            {
                return;
            }

            Vector2 cam = torusCamera.CamWorldPos;
            CreateSprites();
            CreateRuntimeRoot();

            if (planetRenderer != null)
            {
                RegisterHeroPlanet(cam);
            }

            System.Random rng = new(seed);
            foreach (LayerSettings layer in layers)
            {
                for (int i = 0; i < layer.count; i++)
                {
                    CreateBody(layer, cam, rng, i);
                }
            }

            _initialized = true;
        }

        private void CreateRuntimeRoot()
        {
            if (_runtimeRoot != null)
            {
                return;
            }

            GameObject root = new("ProceduralSpaceBackgroundRuntime");
            _runtimeRoot = root.transform;
        }

        private void CreateSprites()
        {
            if (_starSprite != null && _asteroidSprite != null)
            {
                return;
            }

            _starTexture = CreateStarTexture(64);
            _asteroidTexture = CreateAsteroidTexture(96);
            _starSprite = Sprite.Create(_starTexture, new Rect(0f, 0f, _starTexture.width, _starTexture.height),
                new Vector2(0.5f, 0.5f), _starTexture.width);
            _asteroidSprite = Sprite.Create(_asteroidTexture,
                new Rect(0f, 0f, _asteroidTexture.width, _asteroidTexture.height),
                new Vector2(0.5f, 0.5f), _asteroidTexture.width);

            _ownedObjects.Add(_starTexture);
            _ownedObjects.Add(_asteroidTexture);
            _ownedObjects.Add(_starSprite);
            _ownedObjects.Add(_asteroidSprite);
        }

        private void RegisterHeroPlanet(Vector2 cam)
        {
            _heroPlanetMaterial = planetRenderer.material;
            _ownedObjects.Add(_heroPlanetMaterial);

            if (_heroPlanetMaterial.HasProperty(PropCamUVOffset))
            {
                _heroPlanetMaterial.SetVector(PropCamUVOffset, Vector4.zero);
            }

            RandomizePlanetMaterial(_heroPlanetMaterial, new System.Random(seed));

            if (atmosphereRenderer != null)
            {
                _heroAtmosphereMaterial = atmosphereRenderer.material;
                _ownedObjects.Add(_heroAtmosphereMaterial);
                if (_heroAtmosphereMaterial.HasProperty(PropCamUVOffset))
                {
                    _heroAtmosphereMaterial.SetVector(PropCamUVOffset, Vector4.zero);
                }
            }

            Vector2 offset = (Vector2)planetRenderer.transform.position - cam;
            BackgroundBody body = new()
            {
                Transform = planetRenderer.transform,
                AtmosphereTransform = atmosphereRenderer != null ? atmosphereRenderer.transform : null,
                Anchor = cam + offset / Mathf.Max(heroPlanetParallax * parallaxScale, 0.02f),
                Drift = Vector2.zero,
                RotationSpeed = 0f,
                Parallax = Mathf.Max(heroPlanetParallax * parallaxScale, 0.02f),
                ZPosition = planetRenderer.transform.position.z
            };

            _bodies.Add(body);
        }

        private void CreateBody(LayerSettings layer, Vector2 cam, System.Random rng, int index)
        {
            switch (layer.kind)
            {
                case BodyKind.Stars:
                    CreateSpriteBody(layer, cam, rng, index, _starSprite);
                    break;
                case BodyKind.Asteroids:
                    CreateSpriteBody(layer, cam, rng, index, _asteroidSprite);
                    break;
                case BodyKind.Planets:
                    CreatePlanetBody(layer, cam, rng, index);
                    break;
            }
        }

        private void CreateSpriteBody(LayerSettings layer, Vector2 cam, System.Random rng, int index, Sprite sprite)
        {
            GameObject go = new($"{layer.name}_{index:00}");
            go.transform.SetParent(_runtimeRoot, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.Lerp(layer.colorA, layer.colorB, NextFloat(rng));
            sr.sortingOrder = Mathf.RoundToInt(Mathf.Lerp(layer.sortOrderRange.x, layer.sortOrderRange.y, NextFloat(rng)));

            float scale = Mathf.Lerp(layer.scaleRange.x, layer.scaleRange.y, NextFloat(rng));
            go.transform.localScale = Vector3.one * scale;
            go.transform.rotation = Quaternion.Euler(0f, 0f, NextFloat(rng) * 360f);

            BackgroundBody body = new()
            {
                Transform = go.transform,
                Anchor = CreateAnchor(layer, cam, rng),
                Drift = RandomDirection(rng) * Mathf.Lerp(layer.driftSpeedRange.x, layer.driftSpeedRange.y, NextFloat(rng)),
                RotationSpeed = Mathf.Lerp(layer.rotationSpeedRange.x, layer.rotationSpeedRange.y, NextFloat(rng)),
                Parallax = Mathf.Max(layer.parallax * parallaxScale, 0.02f),
                ZPosition = layer.zPosition
            };

            _bodies.Add(body);
        }

        private void CreatePlanetBody(LayerSettings layer, Vector2 cam, System.Random rng, int index)
        {
            GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = $"{layer.name}_{index:00}";
            planet.transform.SetParent(_runtimeRoot, false);
            Destroy(planet.GetComponent<Collider>());

            MeshRenderer renderer = planet.GetComponent<MeshRenderer>();
            Material surfaceMaterial = CreatePlanetMaterialCopy();
            renderer.sharedMaterial = surfaceMaterial;
            renderer.sortingOrder = Mathf.RoundToInt(Mathf.Lerp(layer.sortOrderRange.x, layer.sortOrderRange.y, NextFloat(rng)));
            RandomizePlanetMaterial(surfaceMaterial, rng);

            float scale = Mathf.Lerp(layer.scaleRange.x, layer.scaleRange.y, NextFloat(rng));
            planet.transform.localScale = Vector3.one * scale;

            Transform atmosphereTransform = null;
            Material atmosphereMaterial = null;
            if (TryCreateAtmosphereShell(layer, scale, rng, renderer.sortingOrder + 1, planet.transform, out atmosphereTransform, out atmosphereMaterial))
            {
                atmosphereTransform.SetParent(_runtimeRoot, false);
            }

            BackgroundBody body = new()
            {
                Transform = planet.transform,
                AtmosphereTransform = atmosphereTransform,
                Anchor = CreateAnchor(layer, cam, rng),
                Drift = RandomDirection(rng) * Mathf.Lerp(layer.driftSpeedRange.x, layer.driftSpeedRange.y, NextFloat(rng)),
                RotationSpeed = 0f,
                Parallax = Mathf.Max(layer.parallax * parallaxScale, 0.02f),
                ZPosition = layer.zPosition
            };

            _bodies.Add(body);
        }

        private bool TryCreateAtmosphereShell(LayerSettings layer, float planetScale, System.Random rng, int sortingOrder,
            Transform planetTransform, out Transform atmosphereTransform, out Material atmosphereMaterial)
        {
            atmosphereTransform = null;
            atmosphereMaterial = null;

            Material template = atmosphereRenderer != null ? atmosphereRenderer.sharedMaterial : null;
            if (template == null)
            {
                Shader shader = Shader.Find("Custom/AtmosphereRim");
                if (shader == null)
                {
                    return false;
                }

                template = new Material(shader);
                _ownedObjects.Add(template);
            }

            atmosphereMaterial = new Material(template);
            _ownedObjects.Add(atmosphereMaterial);

            GameObject atmosphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            atmosphere.name = $"{planetTransform.name}_Atmosphere";
            Destroy(atmosphere.GetComponent<Collider>());

            MeshRenderer renderer = atmosphere.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = atmosphereMaterial;
            renderer.sortingOrder = sortingOrder;

            float scale = planetScale * Mathf.Lerp(layer.atmosphereScaleRange.x, layer.atmosphereScaleRange.y, NextFloat(rng));
            atmosphere.transform.localScale = Vector3.one * scale;
            atmosphere.transform.rotation = planetTransform.rotation;
            atmosphereTransform = atmosphere.transform;
            return true;
        }

        private Material CreatePlanetMaterialCopy()
        {
            Material template = planetRenderer != null ? planetRenderer.sharedMaterial : null;
            if (template == null)
            {
                Shader shader = Shader.Find("Custom/PlanetProcedural");
                if (shader == null)
                {
                    throw new InvalidOperationException("Custom/PlanetProcedural shader not found.");
                }

                template = new Material(shader);
                _ownedObjects.Add(template);
            }

            Material material = new(template);
            if (material.HasProperty(PropCamUVOffset))
            {
                material.SetVector(PropCamUVOffset, Vector4.zero);
            }

            _ownedObjects.Add(material);
            return material;
        }

        private void RandomizePlanetMaterial(Material material, System.Random rng)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(PropSeed))
            {
                material.SetFloat(PropSeed, Mathf.Round(NextFloat(rng) * 999f));
            }

            if (material.HasProperty(PropNoiseScale))
            {
                material.SetFloat(PropNoiseScale, Mathf.Lerp(1.6f, 4.8f, NextFloat(rng)));
            }

            if (material.HasProperty(PropWaterLevel))
            {
                material.SetFloat(PropWaterLevel, Mathf.Lerp(0.34f, 0.68f, NextFloat(rng)));
            }

            if (material.HasProperty(PropSunDir))
            {
                float sunX = Mathf.Lerp(-1.4f, 1.4f, NextFloat(rng));
                float sunY = Mathf.Lerp(-0.3f, 1.1f, NextFloat(rng));
                material.SetVector(PropSunDir, new Vector4(sunX, sunY, -2f, 0f));
            }
        }

        private Vector2 CreateAnchor(LayerSettings layer, Vector2 cam, System.Random rng)
        {
            Vector2 extents = GetViewExtents() + viewPadding;
            Vector2 displayOffset = new(
                Mathf.Lerp(-extents.x, extents.x, NextFloat(rng)),
                Mathf.Lerp(-extents.y, extents.y, NextFloat(rng)));

            float scaledParallax = Mathf.Max(layer.parallax * parallaxScale, 0.02f);
            return cam + displayOffset / scaledParallax;
        }

        private void RecycleBody(BackgroundBody body, Vector2 cam, Vector2 displayExtents)
        {
            float parallax = Mathf.Max(body.Parallax, 0.02f);
            Vector2 rawLimits = displayExtents * recycleMultiplier / parallax;
            Vector2 rawDelta = body.Anchor - cam;

            if (Mathf.Abs(rawDelta.x) > rawLimits.x)
            {
                body.Anchor.x = cam.x - Mathf.Sign(rawDelta.x) * rawLimits.x;
                body.Anchor.y = cam.y + UnityEngine.Random.Range(-rawLimits.y, rawLimits.y);
            }

            if (Mathf.Abs(rawDelta.y) > rawLimits.y)
            {
                body.Anchor.y = cam.y - Mathf.Sign(rawDelta.y) * rawLimits.y;
                body.Anchor.x = cam.x + UnityEngine.Random.Range(-rawLimits.x, rawLimits.x);
            }
        }

        private Vector2 GetViewExtents()
        {
            if (_camera == null)
            {
                return new Vector2(16f, 9f);
            }

            float height = _camera.orthographic ? _camera.orthographicSize : 10f;
            return new Vector2(height * _camera.aspect, height);
        }

        private Vector2 GetEditorCameraPosition()
        {
            if (Application.isPlaying && torusCamera != null)
            {
                return torusCamera.CamWorldPos;
            }

            return torusCamera != null
                ? (Vector2)torusCamera.transform.position
                : (Vector2)transform.position;
        }

        private static Vector2 SortRange(Vector2 value, float min = float.NegativeInfinity)
        {
            if (value.x > value.y)
            {
                (value.x, value.y) = (value.y, value.x);
            }

            value.x = Mathf.Max(min, value.x);
            value.y = Mathf.Max(value.x, value.y);
            return value;
        }

        private static float NextFloat(System.Random rng)
        {
            return (float)rng.NextDouble();
        }

        private static Vector2 RandomDirection(System.Random rng)
        {
            float angle = NextFloat(rng) * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

#if UNITY_EDITOR
        private static void DrawRect(Vector2 center, Vector2 extents, Color color)
        {
            Gizmos.color = color;

            Vector3 topLeft = new(center.x - extents.x, center.y + extents.y, 0f);
            Vector3 topRight = new(center.x + extents.x, center.y + extents.y, 0f);
            Vector3 bottomRight = new(center.x + extents.x, center.y - extents.y, 0f);
            Vector3 bottomLeft = new(center.x - extents.x, center.y - extents.y, 0f);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }

        private static Color GetLayerDebugColor(BodyKind kind)
        {
            return kind switch
            {
                BodyKind.Stars => new Color(0.55f, 0.65f, 1f, 0.28f),
                BodyKind.Asteroids => new Color(0.82f, 0.66f, 0.38f, 0.32f),
                BodyKind.Planets => new Color(0.38f, 1f, 0.78f, 0.30f),
                _ => Color.white
            };
        }
#endif

        private static Texture2D CreateStarTexture(int size)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = center.x;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new(x, y);
                    float distance = Vector2.Distance(point, center) / radius;
                    float glow = Mathf.Exp(-distance * distance * 8f);
                    float core = Mathf.Pow(Mathf.Clamp01(1f - distance), 5f);
                    float cross = Mathf.Clamp01(1f - Mathf.Min(Mathf.Abs(point.x - center.x), Mathf.Abs(point.y - center.y)) / radius);
                    float alpha = Mathf.Clamp01(glow * 0.75f + core + cross * 0.15f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateAsteroidTexture(int size)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = center.x;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 uv = (new Vector2(x, y) - center) / radius;
                    float angle = Mathf.Atan2(uv.y, uv.x);
                    float edge = 0.75f
                                 + Mathf.Sin(angle * 3f) * 0.09f
                                 + Mathf.Sin(angle * 5f + 1.7f) * 0.07f
                                 + Mathf.Sin(angle * 7f - 0.45f) * 0.04f;
                    float dist = uv.magnitude;
                    float alpha = Mathf.SmoothStep(edge + 0.06f, edge - 0.02f, dist);
                    float light = Mathf.Clamp01(0.52f + uv.x * 0.22f - uv.y * 0.14f + (1f - dist) * 0.25f);
                    pixels[y * size + x] = new Color(light, light, light, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
