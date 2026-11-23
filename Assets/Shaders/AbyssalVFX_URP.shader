Shader "AbyssalDungeon/AbyssalVFX_URP"
{
    Properties
    {
        [MainTexture] _MainTex ("Energy / Rune Texture", 2D) = "white" {}
        [HDR] _MainColor ("Main HDR Color", Color) = (1.0, 0.88, 0.48, 1.0)
        _EmissionIntensity ("Emission Intensity", Range(0.1, 10.0)) = 2.4
        _ScrollSpeedX ("UV Scroll Speed X", Float) = 0.0
        _ScrollSpeedY ("UV Scroll Speed Y", Float) = -1.2
        [HDR] _FresnelColor ("Fresnel Rim Color", Color) = (1.0, 0.96, 0.82, 1.0)
        _FresnelPower ("Fresnel Power", Range(0.5, 8.0)) = 2.5
        _FresnelStrength ("Fresnel Strength", Range(0.0, 2.0)) = 0.9
        _EdgeSoftness ("UV 2-End Soft Fade", Range(0.01, 0.49)) = 0.16
        _RadialMask ("Use Radial Ground Mask", Range(0.0, 1.0)) = 0.0
        _Opacity ("Master Opacity", Range(0.0, 2.0)) = 1.0

        [Header(Blend and Render State)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+20"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "AbyssalVFXForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _MainColor;
                half4  _FresnelColor;
                half   _EmissionIntensity;
                float  _ScrollSpeedX;
                float  _ScrollSpeedY;
                half   _FresnelPower;
                half   _FresnelStrength;
                half   _EdgeSoftness;
                half   _RadialMask;
                half   _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 scrolledUV : TEXCOORD0;
                float2 rawUV      : TEXCOORD1;
                float3 posWS      : TEXCOORD2;
                half3  normalWS   : TEXCOORD3;
                half4  color      : COLOR;
                half   fogFactor  : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.posWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.rawUV = IN.uv;

                float2 baseUV = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.scrolledUV = baseUV + float2(_ScrollSpeedX, _ScrollSpeedY) * _Time.y;
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // Sample scrolling texture + secondary harmonic flow wave for rich 3D energy streaks
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.scrolledUV);
                half texLum = max(tex.r, max(tex.g, tex.b));

                float waveA = sin(IN.scrolledUV.y * 12.566 + IN.scrolledUV.x * 6.283) * 0.5 + 0.5;
                float waveB = sin(IN.scrolledUV.y * 25.132 - _Time.y * 3.2 + IN.rawUV.x * 12.566) * 0.5 + 0.5;
                half flowEnergy = saturate(lerp(0.62h, 1.28h, (half)(waveA * 0.65 + waveB * 0.35)));

                // View-angle Fresnel Rim (two-sided safe via abs(dot(N,V)))
                half3 N = SafeNormalize(IN.normalWS);
                half3 V = SafeNormalize(GetCameraPositionWS() - IN.posWS);
                half ndotv = abs(dot(N, V));
                half fresnel = pow(1.0h - saturate(ndotv), _FresnelPower) * _FresnelStrength;

                // Soft 2-end fade along UV.y (or radial fade when _RadialMask > 0 for ground rune/crack decals)
                half soft = max((half)0.01, _EdgeSoftness);
                half endFadeY = smoothstep(0.0h, soft, (half)IN.rawUV.y) * smoothstep(1.0h, 1.0h - soft, (half)IN.rawUV.y);

                float2 centered = IN.rawUV * 2.0 - 1.0;
                half r = (half)length(centered);
                half radialFade = 1.0h - smoothstep(1.0h - soft * 1.8h, 1.0h, r);
                half edgeMask = lerp(endFadeY, radialFade, _RadialMask);

                // Combine HDR core color, scrolling energy, and Fresnel rim glow for URP Bloom
                half3 coreHdr = _MainColor.rgb * (_EmissionIntensity * tex.rgb * flowEnergy);
                half3 rimHdr  = _FresnelColor.rgb * (fresnel * _EmissionIntensity * 1.15h);
                half3 finalRGB = (coreHdr + rimHdr) * IN.color.rgb;
                finalRGB = MixFog(finalRGB, IN.fogFactor);

                half baseAlpha = saturate(tex.a * texLum * 0.85h + fresnel * 0.65h);
                half finalAlpha = saturate(baseAlpha * edgeMask * _Opacity * _MainColor.a * IN.color.a);

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Particles/Unlit"
}

