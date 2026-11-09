Shader "AbyssalDungeon/WeaponTrail"
{
    Properties
    {
        [HDR] _MainColor ("Slash Core Color", Color) = (1.0, 0.75, 0.35, 1.0)
        [HDR] _EdgeColor ("Slash Edge Tint", Color) = (0.95, 0.25, 0.12, 1.0)
        _Progress ("Slash Progress", Range(0, 1)) = 0.5
        _Opacity ("Master Opacity", Range(0, 2)) = 1.0
        _ScrollSpeed ("Energy Scroll Speed", Float) = 3.5
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
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // uv.x: 0 (tail) -> 1 (leading blade edge)
                // uv.y: 0 (inner hilt radius) -> 1 (outer blade tip)
                float u = saturate(IN.uv.x);
                float v = saturate(IN.uv.y);

                // Leading head sweep & trailing tail dissipation
                float headMask = 1.0 - smoothstep(_Progress, min(1.0, _Progress + 0.14), u);
                float tailStart = max(0.0, _Progress - 0.72);
                float tailMask = smoothstep(tailStart, min(1.0, tailStart + 0.42), u);

                // Outer blade razor edge highlight + soft inner fade
                float radialMask = smoothstep(0.05, 0.55, v) * (1.0 - smoothstep(0.88, 1.0, v));
                float razorTip = smoothstep(0.68, 0.94, v);

                // Subtle procedural plasma streak along the arc (branch-free)
                float streakWave = sin((u * 18.0 - _Time.y * _ScrollSpeed) + v * 5.0) * 0.5 + 0.5;
                float energyBand = lerp(0.72, 1.25, streakWave);

                half3 col = lerp(_EdgeColor.rgb, _MainColor.rgb, saturate(razorTip * 0.85 + u * 0.35));
                col *= energyBand * IN.color.rgb;

                float alpha = saturate(headMask * tailMask * radialMask * _Opacity * IN.color.a);
                return half4(col * alpha, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}

