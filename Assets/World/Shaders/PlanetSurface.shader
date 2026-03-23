// PlanetSurface.shader
// Renders the background planet sphere.
//
// UV scroll  — driven each frame by PlanetBackground.cs via _CamUVOffset.
//              As the camera moves across the torus world the correct
//              portion of the planet texture sweeps across the sphere.
//
// Atmosphere — simple Fresnel rim at the planet's limb baked right into
//              this pass, so a second sphere isn't strictly required.
//              Use AtmosphereRim.shader on an outer sphere for a fuller glow.
//
// SETUP:
//   • Assign to a Sphere GameObject at roughly Z = 100 (behind all sprites).
//   • Scale the sphere so it fills ~80 % of your camera's viewport height.
//   • Use a tileable / seamless planet-surface texture for _MainTex.
//   • Set Sorting Layer to the same layer as your background sprites
//     (or one behind them) so sprites always draw on top.

Shader "Custom/PlanetSurface"
{
    Properties
    {
        _MainTex            ("Planet Surface Texture",   2D)          = "white" {}
        _CamUVOffset        ("Camera UV Offset (XY)",    Vector)       = (0,0,0,0)
        _AtmosphereColor    ("Atmosphere Rim Color",     Color)        = (0.25, 0.55, 1.0, 1.0)
        _AtmosphereStrength ("Atmosphere Rim Strength",  Range(0, 2))  = 0.8
        _AtmospherePower    ("Atmosphere Rim Falloff",   Range(1, 10)) = 4.0
        _DarkSide           ("Dark-Side Tint Strength",  Range(0, 1))  = 0.35
    }

    SubShader
    {
        // Universal2D is the LightMode the URP 2D Renderer actually executes.
        // Sorting order (set to -100 in PlanetSetupEditor) keeps it behind sprites.
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        Pass
        {
            Name "PlanetSurface"
            Tags { "LightMode" = "Universal2D" }

            ZWrite On
            Cull   Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            //===| vertex input / output |===============================================
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;   // for correct viewDir under ortho
            };

            //===| material properties |================================================
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _CamUVOffset;
                float4 _AtmosphereColor;
                float  _AtmosphereStrength;
                float  _AtmospherePower;
                float  _DarkSide;
            CBUFFER_END

            //===| vertex shader |=================================================
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Scroll UVs by the camera's normalised world-position so
                // the visible patch of texture tracks the ship's position.
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex) + _CamUVOffset.xy;

                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            //===| fragment shader |============================================================
            half4 frag(Varyings IN) : SV_Target
            {
                half4 surface = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // View direction from surface point to camera
                // (correct under both orthographic and perspective projection).
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float3 normal  = normalize(IN.normalWS);

                float NdotV    = saturate(dot(normal, viewDir));

                // --- Atmosphere rim -----------------------------------------
                // Fresnel: 0 at sphere centre, 1 at the limb.
                float rim      = pow(1.0 - NdotV, _AtmospherePower);
                half3 color    = lerp(surface.rgb,
                                      _AtmosphereColor.rgb,
                                      saturate(rim * _AtmosphereStrength));

                // --- Subtle dark-side tint ----------------------------------
                // Normals on the sphere's "back" relative to the camera are
                // slightly darkened to imply a shadow side.
                color *= lerp(_DarkSide, 1.0, NdotV);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}


