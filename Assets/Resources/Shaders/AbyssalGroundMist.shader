Shader "AbyssalDungeon/GroundMist"
{
    Properties
    {
        [MainColor] _MistColor("Mist Color", Color) = (0.36, 0.42, 0.58, 0.28)
        _ScrollSpeed("Mist Drift Speed", Range(0.02, 1.5)) = 0.16
        _NoiseScale("Mist Scale", Range(0.5, 10.0)) = 2.8
        _EdgeSoftness("Border Softness", Range(0.05, 0.5)) = 0.28
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-10"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 150

        Pass
        {
            Name "GroundMistForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _MistColor;
                half _ScrollSpeed;
                half _NoiseScale;
                half _EdgeSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 posWS      : TEXCOORD1;
                half fogFactor    : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            half Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 567.21));
                p += dot(p, p + 34.23);
                return (half)frac(p.x * p.y);
            }

            half MistNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                half a = Hash21(i);
                half b = Hash21(i + float2(1.0, 0.0));
                half c = Hash21(i + float2(0.0, 1.0));
                half d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, (half)f.x), lerp(c, d, (half)f.x), (half)f.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.posWS = posInputs.positionWS;
                OUT.uv = IN.uv;
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float2 wuv = IN.posWS.xz * (0.14 * _NoiseScale);
                float t = _Time.y * _ScrollSpeed;

                half n1 = MistNoise(wuv + float2(t, t * 0.63));
                half n2 = MistNoise(wuv * 2.15 - float2(t * 0.72, -t * 0.48));
                half cloud = smoothstep(0.28h, 0.85h, n1 * 0.62h + n2 * 0.38h);

                half2 borderDist = min((half2)IN.uv, 1.0h - (half2)IN.uv);
                half borderFade = smoothstep(0.0h, _EdgeSoftness, min(borderDist.x, borderDist.y));

                half alpha = saturate(cloud * borderFade * _MistColor.a);
                half3 col = MixFog(_MistColor.rgb, IN.fogFactor);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}

