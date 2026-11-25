Shader "AbyssalDungeon/LightShaft"
{
    Properties
    {
        _Color ("Beam Color", Color) = (1.0, 0.86, 0.62, 0.13)
        _Intensity ("Intensity", Range(0.0, 2.0)) = 0.85
        _FresnelPower ("Edge Softness (Fresnel)", Range(0.5, 6.0)) = 2.6
        _ScrollSpeed ("Dust Shimmer Speed", Range(0.0, 2.0)) = 0.20
        _FadeDist ("Camera Near Fade Distance", Range(1.0, 10.0)) = 3.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "AbyssalLightShaftPass"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                float3 worldNorm  : TEXCOORD2;
                half fogFactor    : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _FresnelPower;
                half _ScrollSpeed;
                half _FadeDist;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = posInputs.positionCS;
                OUT.worldPos = posInputs.positionWS;
                OUT.worldNorm = normInputs.normalWS;
                OUT.uv = IN.uv;
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 viewVec = _WorldSpaceCameraPos.xyz - IN.worldPos;
                float camDist = length(viewVec);
                float3 viewDir = viewVec / max(camDist, 0.0001);

                // Ultra-soft Fresnel silhouette fade so cone edges dissolve seamlessly into mist
                float ndotv = saturate(abs(dot(normalize(IN.worldNorm), viewDir)));
                half edgeFade = (half)pow(ndotv, _FresnelPower);

                // Safe monotonic HLSL smoothstep fade at floor (uv.y=0) and upper clerestory (uv.y=1)
                half vy = saturate((half)IN.uv.y);
                half bottomFade = smoothstep(0.02h, 0.36h, vy);
                half topFade = 1.0h - smoothstep(0.62h, 1.00h, vy);
                half verticalFade = bottomFade * topFade;

                // Subtle animated volumetric light streaks & dust shimmer
                float t = _Time.y * _ScrollSpeed;
                half streak1 = (half)(sin(IN.uv.x * 18.84955 + t * 1.1) * 0.5 + 0.5);
                half streak2 = (half)(sin(IN.uv.x * 31.4159 - t * 0.7 + IN.uv.y * 3.5) * 0.5 + 0.5);
                half dustShimmer = lerp(0.74h, 1.16h, streak1 * 0.6h + streak2 * 0.4h);

                // Smoothly dissolve when third-person camera moves close to the beam
                half camFade = (half)saturate((camDist - 2.4) / max(_FadeDist, 0.5));

                half alpha = saturate(_Color.a * edgeFade * verticalFade * dustShimmer * camFade);
                half3 rgb = _Color.rgb * (_Intensity * dustShimmer);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
