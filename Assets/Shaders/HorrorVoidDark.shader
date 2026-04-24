// Shader: Hidden/HorrorSilhouetteOpaque_v2 (URP)
// Opaque dark fill + rim, with distance fade on rim and horizontal suppression.
// Replace the previous version with this.

Shader "Hidden/HorrorSilhouetteOpaque_v2"
{
    Properties{
        _FillColor     ("Fill Color", Color) = (0.03, 0.04, 0.06, 1)
        _FillStrength  ("Fill Strength", Range(0,1)) = 0.25
        _RimColor      ("Rim Color", Color) = (0.75, 0.95, 1, 1)
        _RimPower      ("Rim Power", Range(0.2,8)) = 2.2
        _RimStrength   ("Rim Strength", Range(0,3)) = 1.2
        _RimDistWeight ("Rim Distance Weight (0=no fade, 1=full fade)", Range(0,1)) = 1
        _HorizSuppress ("Suppress Rim on Horizontal (0..1)", Range(0,1)) = 0.85
        _EdgeBoost     ("Edge Boost (screen-space normals)", Range(0,4)) = 0.0
        _DistStart     ("Distance Fade Start (m)", Range(0,50)) = 2
        _DistEnd       ("Distance Fade End (m)",   Range(0,50)) = 12
        _UpBoost       ("Horizontal Surface Boost", Range(0,1)) = 0.15
        _Grain         ("Film Grain Amount", Range(0,0.5)) = 0.08
    }
    SubShader{
        Tags{ "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass{
            Name "HorrorOpaque"
            ZWrite On
            ZTest LEqual
            Cull Back
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS: POSITION; float3 normalOS: NORMAL; };
            struct Varyings {
                float4 posHCS: SV_POSITION;
                float3 nWS   : TEXCOORD0;
                float3 vWS   : TEXCOORD1;
                float3 posWS : TEXCOORD2;
            };

            float4 _FillColor, _RimColor;
            float  _FillStrength, _RimPower, _RimStrength, _RimDistWeight, _HorizSuppress, _EdgeBoost;
            float  _DistStart, _DistEnd, _UpBoost, _Grain;

            // lightweight hash for grain
            float hash31(float3 p){
                p = frac(p*0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x+p.y)*p.z);
            }

            Varyings vert(Attributes IN){
                Varyings o;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                o.posHCS = TransformWorldToHClip(posWS);
                o.nWS = TransformObjectToWorldNormal(IN.normalOS);
                o.vWS = GetWorldSpaceViewDir(posWS);
                o.posWS = posWS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.nWS);
                float3 v = normalize(i.vWS);

                // Distance fade factor (1 near -> 0 far)
                float d = distance(GetCameraPositionWS(), i.posWS);
                float distFade = 1.0;
                if (_DistEnd > _DistStart){
                    distFade = saturate( ( _DistEnd - d ) / max(1e-4, (_DistEnd - _DistStart)) );
                }

                // Base fill (dark body), floors slightly brighter
                float upTerm = saturate(dot(n, float3(0,1,0))) * _UpBoost;
                float fill = saturate(_FillStrength + upTerm);
                float g = (hash31(i.posWS*1.37) - 0.5) * _Grain;

                // Fresnel rim
                float rim = pow(1.0 - saturate(abs(dot(n, v))), _RimPower) * _RimStrength;

                // Suppress rim on horizontal surfaces (floors/ceilings)
                // abs(n.y)=1 for perfectly up/down; bring it toward 0 with suppression
                float horizAtten = 1.0 - (abs(n.y) * _HorizSuppress);
                rim *= saturate(horizAtten);

                // Optional: boost only at actual edges using normal gradients (kills flat-floor glow)
                float edge = 0.0;
                if (_EdgeBoost > 0.0){
                    // ddx/ddy need SM3+, OK on URP
                    float3 ndx = ddx(n);
                    float3 ndy = ddy(n);
                    edge = saturate((length(ndx) + length(ndy)) * _EdgeBoost);
                    rim *= saturate(edge + 0.0001); // avoid zeroing everything
                }

                // Fade rim with distance (prevents far floor from glowing)
                rim *= lerp(1.0, distFade, _RimDistWeight);

                float3 col = _FillColor.rgb * (fill * distFade + g) + _RimColor.rgb * rim;
                return half4(saturate(col), 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
