// Shader: Hidden/FresnelAdditive (URP)
// Use as an Override Material in a Render Objects feature (or on an overlay camera).
Shader "Custom/FresnelAdditive"
{
    Properties{
        _EdgeColor ("Edge Color", Color) = (0.75, 0.95, 1, 1)
        _Power     ("Rim Power", Range(0.2,8)) = 2
        _Strength  ("Rim Strength", Range(0,3)) = 1
        _DistStart ("Distance Fade Start (m)", Range(0,50)) = 0
        _DistEnd   ("Distance Fade End (m)",   Range(0,50)) = 10
    }
    SubShader{
        Tags{ "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass{
            Name "FresnelAdd"
            // Add edges over whatever is already rendered
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS: POSITION; float3 normalOS: NORMAL; };
            struct Varyings  { float4 positionHCS: SV_POSITION; float3 nWS:TEXCOORD0; float3 vWS:TEXCOORD1; float3 posWS:TEXCOORD2; };

            float4 _EdgeColor;
            float  _Power, _Strength, _DistStart, _DistEnd;

            Varyings vert(Attributes IN){
                Varyings o;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(posWS);
                o.nWS = TransformObjectToWorldNormal(IN.normalOS);
                o.vWS = GetWorldSpaceViewDir(posWS);
                o.posWS = posWS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.nWS);
                float3 v = normalize(i.vWS);

                // View-dependent rim
                float fres = pow(1.0 - saturate(abs(dot(n, v))), _Power) * _Strength;

                // Optional distance fade so far geometry isn't too bright
                float d = distance(GetCameraPositionWS(), i.posWS);
                float fade = 1.0;
                if (_DistEnd > _DistStart) {
                    fade = saturate( ( _DistEnd - d ) / max(1e-4, (_DistEnd - _DistStart)) );
                }

                return half4(_EdgeColor.rgb * fres * fade, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
