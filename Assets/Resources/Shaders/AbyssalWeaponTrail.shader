Shader "AbyssalDungeon/WeaponTrail"
{
    Properties
    {
        _MainTex ("Slash Texture (Crescent/Trail)", 2D) = "white" {}
        [HDR] _MainColor ("Slash Core Color", Color) = (2.2, 1.9, 1.3, 1.0)
        [HDR] _EdgeColor ("Slash Edge Tint", Color) = (1.4, 0.45, 0.12, 1.0)
        _Progress ("Slash Progress", Range(0, 1)) = 0.5
        _Opacity ("Master Opacity", Range(0, 3)) = 1.0
        _ScrollSpeed ("Energy Scroll Speed", Float) = 4.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+15"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "WeaponSlashUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

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
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainColor;
                float4 _EdgeColor;
                float  _Progress;
                float  _Opacity;
                float  _ScrollSpeed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // uv.x: 0 (trailing start) -> 1 (leading blade tip/head)
                // uv.y: 0 (inner radius / hilt) -> 1 (outer razor cutting edge)
                float u = saturate(IN.uv.x);
                float v = saturate(IN.uv.y);

                // Sample artistic slash crescent / streak texture
                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Leading head sweep & trailing tail dissipation
                float headMask = 1.0 - smoothstep(_Progress, min(1.0, _Progress + 0.18), u);
                float tailStart = max(0.0, _Progress - 0.78);
                float tailMask = smoothstep(tailStart, min(1.0, tailStart + 0.45), u);
                float sweepMask = headMask * tailMask;

                // Outer razor edge: bright crisp cutting blade rim (never fade outer edge to 0!)
                float innerFade = smoothstep(0.0, 0.28, v);
                float razorTip = pow(v, 2.2);

                // Dynamic scrolling energy ripple along arc
                float streakWave = sin((u * 14.0 - _Time.y * _ScrollSpeed) + v * 4.0) * 0.5 + 0.5;
                float energyBand = lerp(0.85, 1.45, streakWave);

                // Vibrant HDR blend: outer edge glows white/core, inner body holds rich accent
                half3 col = lerp(_EdgeColor.rgb, _MainColor.rgb * 1.6, saturate(razorTip * 1.35 + 0.1));
                col *= texCol.rgb * energyBand * IN.color.rgb;

                // Alpha composite
                float alpha = saturate(sweepMask * innerFade * texCol.a * _Opacity * IN.color.a);
                return half4(col * alpha, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
