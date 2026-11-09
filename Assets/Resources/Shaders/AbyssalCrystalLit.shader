Shader "AbyssalDungeon/CrystalLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (0.18, 0.55, 0.88, 1)
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1.0
        _EmissionMap("Vein Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0.20, 0.85, 1.45, 1)
        [HDR] _RimColor("Fresnel Rim Color", Color) = (0.45, 0.90, 1.80, 1)
        _FresnelPower("Fresnel Power", Range(0.5, 8.0)) = 2.6
        _Smoothness("Facet Smoothness", Range(0.0, 1.0)) = 0.86
        _PulseSpeed("Pulse Speed", Range(0.0, 8.0)) = 2.2
        _PulseAmp("Pulse Amplitude", Range(0.0, 1.0)) = 0.32
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 250

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EmissionColor;
            half4 _RimColor;
            half _BumpScale;
            half _FresnelPower;
            half _Smoothness;
            half _PulseSpeed;
            half _PulseAmp;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);     SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 posWS      : TEXCOORD1;
                half3 normalWS    : TEXCOORD2;
                half4 tangentWS   : TEXCOORD3;
                half fogFactor    : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.posWS = posInputs.positionWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = normInputs.normalWS;
                OUT.tangentWS = half4(normInputs.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 albedoTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 albedo = albedoTex.rgb * _BaseColor.rgb;

                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
                half3 bitangentWS = IN.tangentWS.w * cross(IN.normalWS, IN.tangentWS.xyz);
                half3 N = normalize(normalTS.x * IN.tangentWS.xyz + normalTS.y * bitangentWS + normalTS.z * IN.normalWS);
                half3 V = SafeNormalize(GetCameraPositionWS() - IN.posWS);

                float4 shadowCoord = TransformWorldToShadowCoord(IN.posWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndl = saturate(dot(N, mainLight.direction));
                half3 H = SafeNormalize(mainLight.direction + V);
                half specPow = exp2(6.0h * _Smoothness + 2.0h);
                half spec = pow(saturate(dot(N, H)), specPow) * _Smoothness;

                half3 lit = albedo * (SampleSH(N) + mainLight.color * (ndl * mainLight.shadowAttenuation))
                          + mainLight.color * (spec * mainLight.shadowAttenuation * 0.85h);

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint li = 0u; li < lightCount; ++li)
                {
                    Light addLight = GetAdditionalLight(li, IN.posWS);
                    half addNdl = saturate(dot(N, addLight.direction));
                    half atten = addLight.distanceAttenuation * addLight.shadowAttenuation;
                    half3 addH = SafeNormalize(addLight.direction + V);
                    half addSpec = pow(saturate(dot(N, addH)), specPow) * _Smoothness * 0.5h;
                    lit += addLight.color * atten * (albedo * addNdl + addSpec);
                }
                #endif

                half fresnel = pow(1.0h - saturate(dot(N, V)), _FresnelPower);
                half pulse = 1.0h + sin(_Time.y * _PulseSpeed + IN.posWS.y * 2.4h + IN.posWS.x * 0.7h) * _PulseAmp;
                half3 veinEmi = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb * _EmissionColor.rgb * pulse;

                half3 color = lit + veinEmi + _RimColor.rgb * (fresnel * pulse);
                color = MixFog(color, IN.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(input.normalOS);
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                output.positionCS = posCS;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}

