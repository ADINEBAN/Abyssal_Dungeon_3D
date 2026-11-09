Shader "AbyssalDungeon/TorchFlame"
{
    Properties
    {
        [HDR] _CoreColor("Inner Core Color", Color) = (2.8, 2.1, 0.95, 1)
        [HDR] _OuterColor("Outer Flame Color", Color) = (1.9, 0.45, 0.08, 1)
        _SwaySpeed("Sway Speed", Range(0.5, 12.0)) = 5.4
        _SwayAmount("Sway Amount", Range(0.0, 0.4)) = 0.09
        _ScrollSpeed("Flame Scroll Speed", Range(0.5, 8.0)) = 3.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 150

        Pass
        {
            Name "TorchFlameForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _OuterColor;
                half _SwaySpeed;
                half _SwayAmount;
                half _ScrollSpeed;
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
                half phase        : TEXCOORD1;
                half fogFactor    : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return (half)frac(p.x * p.y);
            }

            half ValueNoise(float2 p)
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

                float3 originWS = TransformObjectToWorld(float3(0, 0, 0));
                float worldSeed = originWS.x * 1.73 + originWS.z * 2.19;
                float hMask = saturate(IN.uv.y);

                float3 posOS = IN.positionOS.xyz;
                float swayX = sin(_Time.y * _SwaySpeed + posOS.y * 5.0 + worldSeed) * _SwayAmount * hMask;
                float swayZ = cos(_Time.y * (_SwaySpeed * 0.83) + posOS.y * 4.3 + worldSeed * 1.3) * _SwayAmount * hMask;
                posOS.x += swayX;
                posOS.z += swayZ;

                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                OUT.positionCS = posInputs.positionCS;
                OUT.uv = IN.uv;
                OUT.phase = (half)frac(worldSeed * 0.137);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float2 uv = IN.uv;
                float t = _Time.y * _ScrollSpeed + IN.phase * 6.28;

                float2 nUv1 = float2(uv.x * 3.5, uv.y * 3.0 - t);
                float2 nUv2 = float2(uv.x * 6.5 + 1.7, uv.y * 5.5 - t * 1.45);
                half n = ValueNoise(nUv1) * 0.65h + ValueNoise(nUv2) * 0.35h;

                // Teardrop silhouette centered around uv.x = 0.5, wider near bottom, tapering at top
                half dx = abs((half)uv.x - 0.5h) * 2.0h;
                half widthProfile = lerp(0.88h, 0.12h, saturate((half)uv.y));
                half radial = saturate(1.0h - dx / max(0.08h, widthProfile + (n - 0.5h) * 0.42h));
                half vertFade = smoothstep(0.0h, 0.16h, (half)uv.y) * smoothstep(1.0h, 0.22h, (half)uv.y);

                half shape = saturate(radial * vertFade * (0.65h + n * 0.75h));
                half coreMask = smoothstep(0.48h, 0.88h, shape);

                half3 col = lerp(_OuterColor.rgb, _CoreColor.rgb, coreMask) * shape;
                col = MixFogColor(col, half3(0.0h, 0.0h, 0.0h), IN.fogFactor);
                return half4(col, shape);
            }
            ENDHLSL
        }
    }
}

