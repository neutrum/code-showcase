Shader "Custom/TangledCobwebTriPlanar"
{
    Properties
    {
        _MainTex ("Cobweb Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TilingScale ("Tiling Scale", Float) = 0.2
        _EdgeFade ("Edge Fade", Range(0.01, 0.5)) = 0.1
        _AlphaCutoff ("Alpha Cutoff", Range(0.01, 0.9)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 objectPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _TilingScale;
            float _EdgeFade;
            float _AlphaCutoff;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.objectPos = IN.positionOS.xyz;
                return OUT;
            }

            float4 SampleTriPlanar(float3 posWS, float3 normalWS)
            {
                float3 absN = abs(normalWS);
                absN = pow(absN, 4.0);
                absN /= (absN.x + absN.y + absN.z + 1e-5); // avoid div zero

                float2 uvX = posWS.yz * _TilingScale;
                float2 uvY = posWS.xz * _TilingScale;
                float2 uvZ = posWS.xy * _TilingScale;

                float4 xProj = tex2D(_MainTex, uvX);
                float4 yProj = tex2D(_MainTex, uvY);
                float4 zProj = tex2D(_MainTex, uvZ);

                return xProj * absN.x + yProj * absN.y + zProj * absN.z;
            }

            float CalculateEdgeFade(float3 objectPos)
            {
                float3 fadeCoord = abs(objectPos) / 5.0; // approximate bounding box
                float3 fade = smoothstep(1.0, 1.0 - _EdgeFade, fadeCoord);
                return saturate(fade.x * fade.y * fade.z);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float4 tex = SampleTriPlanar(IN.positionWS, normalWS);

                float fade = CalculateEdgeFade(IN.objectPos);
                float alpha = tex.a * fade * _Color.a;

                if (alpha < _AlphaCutoff)
                    discard;

                float3 albedo = tex.rgb * _Color.rgb;

                Light light = GetMainLight();
                float NdotL = saturate(dot(normalWS, -light.direction));
                float3 litColor = albedo * (light.color * NdotL + 0.1);

                return float4(litColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
