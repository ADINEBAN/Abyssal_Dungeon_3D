Shader "AbyssalDungeon/EnvParticle"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.75, 0.72, 0.70, 0.65)
        _SecondaryColor ("Edge Shadow Color", Color) = (0.08, 0.06, 0.09, 0.85)
        _Intensity ("Intensity", Range(0.1, 3.0)) = 1.0
        _ScrollSpeed ("UV Scroll (XY)", Vector) = (0.0, 0.0, 0.0, 0.0)
        _DistortionAmp ("Subtle Flow Distortion", Range(0.0, 0.25)) = 0.04
        _EdgeSoftness ("Radial Edge Softness", Range(0.05, 0.95)) = 0.45
        _UseRadialFade ("Use Radial Quad Fade", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+6"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 150

        Pass
        {
            Name "EnvParticleForward"
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _TintColor;
                half4 _SecondaryColor;
                half _Intensity;
                float4 _ScrollSpeed;
                half _DistortionAmp;
                half _EdgeSoftness;
                half _UseRadialFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float2 rawUv       : TEXCOORD1;
                half4 color        : COLOR;
                half fogFactor     : TEXCOORD2;
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

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.rawUv = IN.uv;
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float2 uv = IN.uv + _ScrollSpeed.xy * _Time.y;
                if (_DistortionAmp > 0.001h)
                {
                    half n1 = ValueNoise(uv * 3.1 + float2(_Time.y * 0.25, -_Time.y * 0.18)) - 0.5h;
                    half n2 = ValueNoise(uv * 4.7 + float2(-_Time.y * 0.21, _Time.y * 0.31)) - 0.5h;
                    uv += float2(n1, n2) * _DistortionAmp;
                }

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half mask = tex.a * max(tex.r, max(tex.g, tex.b));

                float2 centered = IN.rawUv * 2.0 - 1.0;
                half r = (half)length(centered);
                half radial = 1.0h - smoothstep(saturate(0.95h - _EdgeSoftness), 0.98h, r);
                half geoMask = lerp(1.0h, radial, _UseRadialFade);

                half density = saturate(tex.r * 0.5h + tex.g * 0.5h);
                half3 baseRgb = lerp(_SecondaryColor.rgb, _TintColor.rgb, density) * _Intensity * IN.color.rgb;
                half alpha = saturate(mask * geoMask * _TintColor.a * IN.color.a);

                baseRgb = MixFog(baseRgb, IN.fogFactor);
                return half4(baseRgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}

