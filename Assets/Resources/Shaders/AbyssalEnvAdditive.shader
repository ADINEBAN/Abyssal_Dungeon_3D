Shader "AbyssalDungeon/EnvAdditive"
{
    Properties
    {
        _MainTex ("VFX Texture", 2D) = "white" {}
        [HDR] _CoreColor ("Core HDR Color", Color) = (1.6, 1.35, 0.72, 1.0)
        [HDR] _OuterColor ("Outer Glow Color", Color) = (1.15, 0.42, 0.12, 1.0)
        _Intensity ("Intensity Scalar", Range(0.1, 3.0)) = 1.0
        _ScrollSpeed ("UV Scroll (XY)", Vector) = (0.0, 0.0, 0.0, 0.0)
        _RadialFade ("Radial Quad Fade", Range(0.0, 1.0)) = 1.0
        _FadeEnds ("Cylindrical Vertical Fade", Range(0.0, 1.0)) = 0.0
        _PulseSpeed ("Subtle Pulse Speed", Range(0.0, 12.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+14"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 150

        Pass
        {
            Name "EnvAdditiveForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _CoreColor;
                half4 _OuterColor;
                half _Intensity;
                float4 _ScrollSpeed;
                half _RadialFade;
                half _FadeEnds;
                half _PulseSpeed;
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
                float3 posWS       : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.posWS = posInputs.positionWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.rawUv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float2 uv = IN.uv + _ScrollSpeed.xy * _Time.y;
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half lum = max(tex.r, max(tex.g, tex.b));
                half mask = lum * tex.a;

                float2 centered = IN.rawUv * 2.0 - 1.0;
                half r = (half)length(centered);
                half circleMask = 1.0h - smoothstep(0.70h, 0.98h, r);
                half vFade = smoothstep(0.0h, 0.16h, (half)IN.rawUv.y) * (1.0h - smoothstep(0.76h, 1.0h, (half)IN.rawUv.y));

                half geoMask = lerp(1.0h, circleMask, _RadialFade);
                geoMask *= lerp(1.0h, vFade, _FadeEnds);

                half pulse = _PulseSpeed > 0.01h ? (0.88h + 0.12h * (half)sin(_Time.y * _PulseSpeed + IN.rawUv.y * 5.0)) : 1.0h;
                half distFade = 1.0h - smoothstep(45.0h, 95.0h, (half)length(_WorldSpaceCameraPos - IN.posWS));

                half3 rgb = lerp(_OuterColor.rgb, _CoreColor.rgb, saturate(lum * 1.2h)) * _Intensity * pulse * distFade * IN.color.rgb;
                half alpha = saturate(mask * geoMask * distFade * IN.color.a);

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
