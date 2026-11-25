Shader "AbyssalDungeon/GnGEffect"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Effect Texture", 2D) = "white" {}
        _MaskMap ("Mask Texture", 2D) = "white" {}
        [HDR] [MainColor] _BaseColor ("Tint Color", Color) = (1, 1, 1, 1)
        _EmissionBoost ("HDR Boost", Range(0.2, 5.0)) = 1.65
        _UseMask ("Use Mask Texture", Float) = 0
        _LumAsAlpha ("Luminance Soft Alpha", Range(0.0, 1.0)) = 0.55
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+28"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }
        LOD 100

        Pass
        {
            Name "GnGEffectForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaskMap);
            SAMPLER(sampler_MaskMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _MaskMap_ST;
                half4 _BaseColor;
                half _EmissionBoost;
                half _UseMask;
                half _LumAsAlpha;
                half _SrcBlend;
                half _DstBlend;
                half _Cull;
            CBUFFER_END

            // Compatible with both standard Shuriken particles and GnG CustomVertexStreams (Position, Normal, Color, UV, Custom1XYZW)
            struct Attributes
            {
                float4 positionOS  : POSITION;
                float3 normalOS    : NORMAL;
                half4 color        : COLOR;
                float4 uvAndCustom : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvBase      : TEXCOORD0;
                float2 uvMask      : TEXCOORD1;
                half4 color        : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                float2 rawUv = IN.uvAndCustom.xy;
                OUT.uvBase = TRANSFORM_TEX(rawUv, _BaseMap);
                OUT.uvMask = TRANSFORM_TEX(rawUv, _MaskMap);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uvBase);
                half lum = max(baseSample.r, max(baseSample.g, baseSample.b));

                // Multi-channel mask read so R, G, B, or A mask textures in Studio Dois assets never zero out visibility
                half maskVal = 1.0h;
                if (_UseMask > 0.5h)
                {
                    half4 m = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, IN.uvMask);
                    half peak = max(max(m.r, m.g), max(m.b, m.a));
                    maskVal = lerp(0.38h, 1.0h, saturate(peak * 1.25h));
                }

                half softLumAlpha = saturate(lum * 1.45h);
                half texAlpha = max(baseSample.a * lerp(1.0h, softLumAlpha, _LumAsAlpha), softLumAlpha * 0.65h);
                half alpha = saturate(texAlpha * maskVal * _BaseColor.a * max(IN.color.a, 0.25h));
                half boost = max(_EmissionBoost, 1.45h);
                half3 rgb = baseSample.rgb * _BaseColor.rgb * IN.color.rgb * boost * maskVal;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
