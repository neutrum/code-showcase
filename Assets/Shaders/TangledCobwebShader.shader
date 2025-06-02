Shader "Custom/TangledCobwebShader"
{
    Properties
    {
        _MainTex ("Cobweb Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _EdgeFade ("Edge Fade", Range(0.01, 0.5)) = 0.15
        _AlphaBoost ("Alpha Boost", Range(0.1, 5.0)) = 2.0
        _AlphaCutoff ("Alpha Cutoff", Range(0.01, 0.9)) = 0.3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Smoothness;
            float _Metallic;
            float _EdgeFade;
            float _AlphaBoost;
            float _AlphaCutoff;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = posInputs.positionWS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float4 tex = tex2D(_MainTex, uv);

                // Adjust alpha strength
               tex.a = pow(max(tex.a, 0.001), 1.0 / _AlphaBoost);

                // Edge fade
                float2 fade = smoothstep(0.0, _EdgeFade, uv) * smoothstep(0.0, _EdgeFade, 1.0 - uv);
                float alpha = tex.a * fade.x * fade.y * _Color.a;

                if (alpha < _AlphaCutoff) discard;

                float3 albedo = tex.rgb * _Color.rgb;
                float3 normalWS = normalize(IN.normalWS);

                // Lighting (NO normal flip!)
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, -mainLight.direction));
                float3 lighting = mainLight.color * NdotL + 0.15; // slight ambient boost

                float3 finalColor = albedo * lighting;
                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
