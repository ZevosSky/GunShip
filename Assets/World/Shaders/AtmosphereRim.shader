// AtmosphereRim.shader
// Renders an atmospheric halo that glows from the planet surface outward,
// fading to transparent at the outer edge into space.
//
// The band formula  pow(NdotV, SurfaceFade) * pow(1-NdotV, RimPower)
// creates a soft glow that peaks at the planet-atmosphere boundary
// and drops to zero in BOTH directions:
//   -> outer edge (NdotV = 0)  = space, fully transparent
//   -> inner center (NdotV = 1) = planet face, fully transparent
//
// Peak NdotV = SurfaceFade / (SurfaceFade + RimPower)
// Default SurfaceFade=3, RimPower=1  →  peak at NdotV 0.75
// which aligns with the planet disc edge at 1.5× atmosphere scale.

Shader "Custom/AtmosphereRim"
{
    Properties
    {
        _GlowColor   ("Atmosphere Glow Color",   Color)         = (0.3, 0.65, 1.0, 1.0)
        _SurfaceFade ("Surface Glow Falloff",    Range(1, 10))  = 3.0
        _RimPower    ("Outer Space Fade",        Range(0.5, 6)) = 1.0
        _RimStrength ("Glow Intensity",          Range(0,  4))  = 0.85
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "AtmosphereRim"
            Tags { "LightMode" = "Universal2D" }

            Blend  SrcAlpha One
            ZWrite Off
            Cull   Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float  _SurfaceFade;
                float  _RimPower;
                float  _RimStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float3 normal  = normalize(IN.normalWS);
                float  NdotV   = saturate(dot(normal, viewDir));

                //===| Atmosphere band |===========================================================
                // pow(NdotV, SurfaceFade)   -> 0 at outer rim (space), 1 at center
                // pow(1-NdotV, RimPower)    -> 1 at outer rim, 0 at center
                // Product peaks at  NdotV = SurfaceFade / (SurfaceFade + RimPower)
                float band = pow(NdotV, _SurfaceFade) * pow(1.0 - NdotV, _RimPower);

                // Normalise so the peak always equals 1, making _RimStrength
                // directly control intensity regardless of the power values.
                float peakN = _SurfaceFade / (_SurfaceFade + _RimPower);
                float peakV = pow(peakN, _SurfaceFade) * pow(1.0 - peakN, _RimPower);
                band = band / max(peakV, 1e-4);

                float alpha = saturate(band * _RimStrength * _GlowColor.a);
                return half4(_GlowColor.rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
