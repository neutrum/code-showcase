Shader "Custom/HeightBasedTriplanar"
{
    Properties
    {
        _FloorTex ("Floor Texture", 2D) = "white" {}
        _WebTex ("Web Texture", 2D) = "white" {}
        _TableTex ("Table Texture", 2D) = "white" {}
        _Scale ("Triplanar Scale", Float) = 1
        _HeightMin ("Min Height", Float) = 0
        _HeightMax ("Max Height", Float) = 2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _FloorTex;
            sampler2D _WebTex;
            sampler2D _TableTex;
            float _Scale;
            float _HeightMin;
            float _HeightMax;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float height = i.worldPos.y;
                float slope = dot(normalize(i.worldNormal), float3(0,1,0)); // 1 = flat top

                // Floor: base layer
                float isFloor = step(height, _HeightMin);

                // Top flat areas (table)
                float isTop = step(0.85, slope) * step(_HeightMax - 0.1, height);

                // Slope = webs (steep faces)
                float isSlope = step(0.2, slope) * step(slope, 0.85);

                // Fake vertical wall beneath slope (cubic volume fill)
                float isVerticalFill = step(height, _HeightMax - 0.05) * step(0.2, slope);

                float total = isFloor + isTop + isSlope + isVerticalFill + 0.0001;
                isFloor /= total;
                isTop /= total;
                isSlope /= total;
                isVerticalFill /= total;

                float2 uvXZ = i.worldPos.xz * _Scale;
                float2 uvYZ = i.worldPos.zy * _Scale;
                float2 uvXY = i.worldPos.xy * _Scale;

                float4 floorCol = tex2D(_FloorTex, uvXZ);
                float4 topCol = tex2D(_TableTex, uvXZ);
                float4 webCol = tex2D(_WebTex, uvYZ);
                float4 wallFillCol = tex2D(_TableTex, uvYZ);

                float3 rgb = floorCol.rgb * isFloor + webCol.rgb * isSlope + topCol.rgb * isTop + wallFillCol.rgb * isVerticalFill;
                float alpha = webCol.a * isSlope + isTop + isFloor + isVerticalFill;

                return float4(rgb, saturate(alpha));
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
