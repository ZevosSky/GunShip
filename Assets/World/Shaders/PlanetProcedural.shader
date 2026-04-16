// PlanetProcedural.shader
// Generates a planet surface entirely in the fragment shader —
// no textures required.
//
// Pipeline
// ---------------------------------------------------------------------
//  1. FBM (Fractal Brownian Motion) built from 3 octaves of 2D Perlin
//     gradient noise gives a continuous height field in [0, 1].
//     (shamelessly airlifted from reading various shaders online) 
//  2. A 7-stop colour gradient maps height to biome colours.
//     _WaterLevel controls the ocean / land ratio.
//     ( Substance Designer Levels Node essentially ) 
//  3. Atmosphere rim and dark-side tint identical to PlanetSurface.shader.
//  4. _CamUVOffset (driven by PlanetBackground.cs) scrolls the UV each
//     frame so the planet surface tracks the ship's position.
//
// Tuning quick-reference
// --------------------------------------------------------------------
//  _Seed        — change to get a completely different planet layout
//  _NoiseScale  — lower = larger continents, higher = more fragmented
//  _WaterLevel  — 0.3 = mostly land, 0.5 = Earth-like, 0.7 = ocean world

Shader "Custom/PlanetProcedural"
{
    Properties
    {
        // ── Generation ──────────────────────────────────────────────────────
        [Header(Generation)]
        _Seed        ("Planet Seed",  Range(0, 999)) = 0
        _NoiseScale  ("Noise Scale",  Range(0.5, 8)) = 2.5
        _WaterLevel  ("Water Level",  Range(0, 1))   = 0.50

        // ── Biome colours ────────────────────────────────────────────────────
        [Header(Biome Colours)]
        _DeepOceanColor    ("Deep Ocean",    Color) = (0.04, 0.10, 0.30, 1)
        _ShallowOceanColor ("Shallow Ocean", Color) = (0.08, 0.28, 0.55, 1)
        _CoastColor        ("Coast / Beach", Color) = (0.76, 0.70, 0.50, 1)
        _LowlandColor      ("Lowland",       Color) = (0.22, 0.50, 0.16, 1)
        _HighlandColor     ("Highland",      Color) = (0.33, 0.28, 0.16, 1)
        _MountainColor     ("Mountain",      Color) = (0.50, 0.46, 0.42, 1)
        _SnowColor         ("Snow / Ice",    Color) = (0.92, 0.95, 1.00, 1)

        // ── Atmosphere (same as PlanetSurface.shader) ───────────────────────
        [Header(Atmosphere)]
        _AtmosphereColor    ("Rim Color",    Color)        = (0.25, 0.55, 1.0, 1)
        _AtmosphereStrength ("Rim Strength", Range(0, 2))  = 0.8
        _AtmospherePower    ("Rim Falloff",  Range(1, 10)) = 4.0

        // ── Lighting ─────────────────────────────────────────────────────────
        // _SunDir is a world-space direction FROM the sun TOWARD the planet.
        // Default (1, 0.5, -2) = sun slightly right + above, mostly toward camera.
        // Change X/Y to rotate the lit/shadow hemisphere.
        [Header(Lighting)]
        _SunDir      ("Sun Direction (XYZ)", Vector)    = (1, 0.5, -2, 0)
        _AmbientLight ("Dark Side Brightness", Range(0, 0.5)) = 0.12

        // ── Scroll (set by PlanetBackground.cs every frame) ─────────────────
        _CamUVOffset ("Camera UV Offset (XY)", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        Pass
        {
            Name "PlanetProcedural"
            Tags { "LightMode" = "Universal2D" }

            ZWrite On
            Cull   Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── structs ───────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                 float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
            };

            // ── cbuffer ───────────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _CamUVOffset;

                float  _Seed;
                float  _NoiseScale;
                float  _WaterLevel;

                float4 _DeepOceanColor;
                float4 _ShallowOceanColor;
                float4 _CoastColor;
                float4 _LowlandColor;
                float4 _HighlandColor;
                float4 _MountainColor;
                float4 _SnowColor;

                float4 _AtmosphereColor;
                float  _AtmosphereStrength;
                float  _AtmospherePower;

                float4 _SunDir;
                float  _AmbientLight;
            CBUFFER_END

            // ════════════════════════════════════════════════════════════════
            //  NOISE
            // ════════════════════════════════════════════════════════════════

            // Pseudo-random gradient direction for a lattice point.
            // The seed shifts the hash input so every seed value gives a
            // completely different set of gradients.
            float2 GradientDir(float2 p, float seed)
            {
                float h = frac(sin(dot(p + seed * 0.01,
                                       float2(127.1, 311.7))) * 43758.5453);
                float a = h * 6.28318530; // 2π
                return float2(cos(a), sin(a));
            }

            // 2D Perlin gradient noise.
            // Uses Ken Perlin's quintic fade curve for smooth derivatives.
            // Output remapped to [0, 1].
            float Perlin(float2 uv, float seed)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                // Quintic smoothstep: 6t⁵ − 15t⁴ + 10t³
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float a = dot(GradientDir(i + float2(0,0), seed), f - float2(0,0));
                float b = dot(GradientDir(i + float2(1,0), seed), f - float2(1,0));
                float c = dot(GradientDir(i + float2(0,1), seed), f - float2(0,1));
                float d = dot(GradientDir(i + float2(1,1), seed), f - float2(1,1));

                float n = lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
                return n * 0.5 + 0.5;   // [-1..1] → [0..1]
            }

            // Fractal Brownian Motion — 3 octaves.
            //
            //  Octave   Freq    Weight   Contribution
            //  ──────   ──────  ──────   ────────────
            //  1 (coarse)  1.0×  57.1%  large continents / ocean basins
            //  2 (medium)  2.1×  28.6%  coastline shape, island chains
            //  3 (fine)    4.3×  14.3%  small detail, river deltas
            //
            // Non-integer frequency ratios (2.1, 4.3) and different UV
            // offsets per octave break up grid-aligned repetition.
            // Weights sum to 1.0 so output stays in [0, 1].
            float FBM(float2 uv, float seed)
            {
                float v = 0.0;
                v += 0.571 * Perlin(uv * 1.00,                         seed);
                v += 0.286 * Perlin(uv * 2.10 + float2(1.70,  9.20),  seed + 31.41);
                v += 0.143 * Perlin(uv * 4.30 + float2(8.30,  2.80),  seed + 71.92);
                return saturate(v);
            }

            // ════════════════════════════════════════════════════════════════
            //  COLOUR GRADIENT
            // ════════════════════════════════════════════════════════════════

            // Maps a height value to a biome colour.
            // _WaterLevel is sea level: below = ocean, above = land.
            // Both ocean and land have their own internal gradients so the
            // full colour range is used regardless of the water-level setting.
            half3 PlanetColor(float h)
            {
                // ── Ocean (h < waterLevel) ──────────────────────────────────
                // seaT = 0 at deepest point, 1 at the shoreline.
                float seaT    = saturate(h / max(_WaterLevel, 0.001));
                half3 oceanCol = lerp(_DeepOceanColor.rgb,
                                      _ShallowOceanColor.rgb,
                                      smoothstep(0.0, 1.0, seaT));

                // ── Land (h > waterLevel) ───────────────────────────────────
                // landT = 0 at the shoreline, 1 at the highest peak.
                float landT   = saturate((h - _WaterLevel)
                                          / max(1.0 - _WaterLevel, 0.001));
                half3 landCol = _CoastColor.rgb;
                landCol = lerp(landCol, _LowlandColor.rgb,  smoothstep(0.05, 0.20, landT));
                landCol = lerp(landCol, _HighlandColor.rgb, smoothstep(0.45, 0.60, landT));
                landCol = lerp(landCol, _MountainColor.rgb, smoothstep(0.70, 0.82, landT));
                landCol = lerp(landCol, _SnowColor.rgb,     smoothstep(0.88, 0.95, landT));

                // ── Shoreline blend ─────────────────────────────────────────
                // Narrow smoothstep around sea level for a natural coast.
                float shore = smoothstep(_WaterLevel - 0.015,
                                         _WaterLevel + 0.015, h);
                return lerp(oceanCol, landCol, shore);
            }

            // ── vertex shader ─────────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Use local-space XY as the noise projection, NOT the sphere mesh UV.
                // Unity's sphere has its pole facing the camera, so mesh UVs converge
                // to a single point at the top — every fragment samples the same noise
                // position and the output looks uniform (default texture appearance).
                //
                // Local XY ranges from -0.5..0.5 across the sphere diameter.
                // +0.5 remaps that to 0..1, giving a clean overhead projection
                // with no pole distortion at all.
                OUT.uv        = IN.positionOS.xy + 0.5 + _CamUVOffset.xy;

                OUT.normalWS  = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            // ── fragment shader ───────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal  = normalize(IN.normalWS);

                // Evaluate FBM at the scrolled, scaled, seeded UV.
                // The float2 seed offset ensures different seeds sample
                // different regions of the infinite noise field.
                float2 noiseUV = IN.uv * _NoiseScale
                               + float2(_Seed * 0.1000, _Seed * 0.1370);
                float  height  = FBM(noiseUV, _Seed);

                half3 color = PlanetColor(height);

                // ── Lighting ──────────────────────────────────────────────────
                // Lambertian diffuse from a directional sun.
                // NdotL > 0  =  lit side,  NdotL = 0  =  terminator,
                // dark side floors at _AmbientLight so it never goes pure black.
                // This is what makes the sphere actually read as a 3D object —
                // the old NdotV approach only darkened the limb, leaving the
                // whole visible face flat.
                float3 sunDir = normalize(_SunDir.xyz);
                float  NdotL  = saturate(dot(normal, sunDir));
                color        *= _AmbientLight + (1.0 - _AmbientLight) * NdotL;

                // ── Atmosphere rim ────────────────────────────────────────────
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float  NdotV   = saturate(dot(normal, viewDir));
                float  rim     = pow(1.0 - NdotV, _AtmospherePower);
                color          = lerp(color, _AtmosphereColor.rgb,
                                      saturate(rim * _AtmosphereStrength));


                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

