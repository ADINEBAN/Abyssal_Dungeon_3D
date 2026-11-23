Shader "AbyssalDungeon/CharacterRimLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.45
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.25
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)
        _EmissionMap("Emission Map", 2D) = "white" {}
        [HDR] _RimColor("Rim Light Color", Color) = (0.38, 0.78, 1.0, 1.0)
        _RimPower("Rim Power", Range(0.8, 8.0)) = 2.6
        _RimIntensity("Rim Intensity", Range(0.0, 4.0)) = 1.35
        _TopFillBoost("Top Contour Boost", Range(0.0, 1.0)) = 0.28
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

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _BumpScale;
                half   _Smoothness;
                half   _Metallic;
                half4  _EmissionColor;
                half4  _RimColor;
                half   _RimPower;
                half   _RimIntensity;
                half   _TopFillBoost;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = NormalizeNormalPerVertex(normInputs.normalWS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 L = normalize(mainLight.direction);
                float3 H = normalize(L + V);

                // Wrapped Half-Lambert diffuse so dark dungeon shadows never crush character details into pure black
                half NdotL = saturate(dot(N, L) * 0.68h + 0.32h);
                half shadowAtten = lerp(0.42h, 1.0h, mainLight.shadowAttenuation * mainLight.distanceAttenuation);
                half3 directDiffuse = mainLight.color * (NdotL * shadowAtten);

                // Specular highlight for armor/weapons
                half specPow = exp2(4.0h + _Smoothness * 6.5h);
                half spec = pow(saturate(dot(N, H)), specPow) * (_Smoothness * 0.65h) * shadowAtten;
                half3 directSpec = mainLight.color * spec * lerp(0.25h, 1.0h, _Metallic);

                // Additional point lights (torches, braziers, skill crystals)
                half3 addLightSum = half3(0, 0, 0);
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint li = 0u; li < pixelLightCount; ++li)
                {
                    Light aLight = GetAdditionalLight(li, IN.positionWS);
                    half aNdotL = saturate(dot(N, aLight.direction) * 0.6h + 0.4h);
                    addLightSum += aLight.color * (aNdotL * aLight.distanceAttenuation * aLight.shadowAttenuation);
                }
                #endif

                // Ambient + Top Contour Fill
                half3 shAmbient = max(SampleSH(N), half3(0.18h, 0.19h, 0.24h));
                half topContour = saturate(N.y * 0.5h + 0.5h) * _TopFillBoost;
                half3 litColor = texCol.rgb * (directDiffuse + addLightSum + shAmbient + topContour) + directSpec;

                // Silhouette-separating Fresnel Rim Light (strongest along grazing edges & upper shoulders/head)
                half nv = 1.0h - saturate(dot(N, V));
                half fresnel = pow(nv, max(0.8h, _RimPower));
                half verticalMask = saturate(N.y * 0.35h + 0.75h);
                half3 rimGlow = _RimColor.rgb * (fresnel * _RimIntensity * verticalMask);

                // Emission
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb * _EmissionColor.rgb;

                half3 finalRGB = litColor + rimGlow + emission;
                finalRGB = MixFog(finalRGB, IN.fogFactor);
                return half4(finalRGB, 1.0h);
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
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

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

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}

