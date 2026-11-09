Shader "AbyssalDungeon/SkillVfxAdditive"
{
    Properties
    {
        _MainTex ("VFX Texture", 2D) = "white" {}
        [HDR] _MainColor ("Core HDR Color", Color) = (1.0, 0.88, 0.52, 1.0)
        [HDR] _EdgeColor ("Outer Aura Color", Color) = (0.95, 0.32, 0.12, 1.0)
        _Opacity ("Master Opacity", Range(0, 3)) = 1.0
        _RadialMask ("Use Radial Edge Mask", Range(0, 1)) = 1.0
        _PulseSpeed ("Pulse Speed", Float) = 4.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+18"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "SkillVfxAdditiveForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float2 rawUv       : TEXCOORD1;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainColor;
                float4 _EdgeColor;
                float  _Opacity;
                float  _RadialMask;
                float  _PulseSpeed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.rawUv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float lum = max(tex.r, max(tex.g, tex.b));
                float mask = lum * tex.a;

                // Strictly zero out quad edges when _RadialMask > 0 so square borders can NEVER appear
                float2 centered = IN.rawUv * 2.0 - 1.0;
                float r = length(centered);
                float circleEdge = 1.0 - smoothstep(0.78, 0.97, r);
                float vEdge = smoothstep(0.0, 0.08, IN.rawUv.y) * (1.0 - smoothstep(0.88, 1.0, IN.rawUv.y));
                float hEdge = 1.0 - smoothstep(0.78, 0.98, abs(centered.x));
                float geoMask = lerp(vEdge * hEdge, circleEdge, _RadialMask);

                float pulse = 0.88 + 0.12 * sin(_Time.y * _PulseSpeed + r * 6.0);
                half3 col = lerp(_EdgeColor.rgb, _MainColor.rgb, saturate(lum * 1.15)) * pulse * IN.color.rgb;
                float alpha = saturate(mask * geoMask * _Opacity * IN.color.a);

                return half4(col * alpha, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}

