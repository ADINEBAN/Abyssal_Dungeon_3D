Shader "AbyssalDungeon/WeaponAura"
{
    Properties
    {
        _MainTex ("Weapon Aura / Rune Texture", 2D) = "white" {}
        [HDR] _CoreColor ("Core HDR Color", Color) = (1.85, 1.48, 0.62, 1.0)
        [HDR] _RimColor ("Outer Rim / Element Color", Color) = (0.85, 0.42, 0.14, 1.0)
        _Intensity ("Intensity", Range(0.1, 3.0)) = 1.15
        _Opacity ("Master Opacity", Range(0.0, 2.5)) = 1.0
        _ScrollSpeed ("UV Scroll (XY)", Vector) = (-1.6, 0.25, 0.0, 0.0)
        _FresnelPower ("Fresnel Rim Power", Range(0.5, 6.0)) = 2.0
        _FresnelBoost ("Fresnel Rim Boost", Range(0.0, 2.0)) = 0.85
        _PulseSpeed ("Pulse Speed", Range(0.0, 12.0)) = 4.0
        _ShimmerAmp ("3D Vertex Shimmer", Range(0.0, 0.05)) = 0.008
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+16"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 150

        Pass
        {
            Name "WeaponAuraForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _CoreColor;
                half4 _RimColor;
                half _Intensity;
                half _Opacity;
                float4 _ScrollSpeed;
                half _FresnelPower;
                half _FresnelBoost;
                half _PulseSpeed;
                half _ShimmerAmp;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float3 posWS       : TEXCOORD3;
                half4 color        : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 posOS = IN.positionOS.xyz;
                if (_ShimmerAmp > 0.0005h)
                {
                    float wave = sin(_Time.y * 9.0 + posOS.y * 14.0 + posOS.x * 8.0);
                    posOS += IN.normalOS * (wave * _ShimmerAmp * IN.color.a);
                }

                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.posWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float2 scrolledUv = IN.uv + _ScrollSpeed.xy * _Time.y;
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scrolledUv);
                half lum = max(tex.r, max(tex.g, tex.b));
                half texMask = lerp(0.42h, 1.0h, lum) * lerp(0.55h, 1.0h, tex.a);

                float3 nWS = normalize(IN.normalWS);
                float3 vWS = normalize(IN.viewDirWS);
                half ndv = abs((half)dot(nWS, vWS));
                half fresnel = pow(saturate(1.0h - ndv), _FresnelPower);

                half pulse = _PulseSpeed > 0.01h
                    ? (0.88h + 0.12h * (half)sin(_Time.y * _PulseSpeed + IN.uv.x * 6.2831))
                    : 1.0h;

                half distFade = 1.0h - smoothstep(42.0h, 88.0h, (half)length(_WorldSpaceCameraPos - IN.posWS));

                half coreBlend = saturate(lum * 0.85h + fresnel * _FresnelBoost * 0.5h);
                half3 rgb = lerp(_RimColor.rgb, _CoreColor.rgb, coreBlend) * (_Intensity * pulse * distFade) * IN.color.rgb;
                half alpha = saturate((texMask + fresnel * _FresnelBoost * 0.45h) * _Opacity * distFade * IN.color.a);

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
