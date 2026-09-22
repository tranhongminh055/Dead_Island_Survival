Shader "HorrorGame/SkyBeaconBeam"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (0.9, 0.95, 1.0, 1.0)
        _Brightness ("Brightness Multiplier", Range(1.0, 10.0)) = 3.5
        _BaseFade ("Base Fade Height", Float) = 5.0
        _TopFade ("Top Fade Height", Float) = 350.0
        _RimPower ("Rim Softness Power", Range(0.5, 4.0)) = 1.8
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Blend One One
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            fixed4 _TintColor;
            float _Brightness;
            float _BaseFade;
            float _TopFade;
            float _RimPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Độ cao tương đối tính từ chân cột sáng
                float height = i.uv.y; // UV Y từ 0 (đáy) đến 1 (đỉnh)

                // 1. Mờ dần về phía đỉnh (Fade out to the sky)
                float verticalFade = pow(1.0 - saturate(height), 1.2);

                // 2. Viền mềm Fresnel hai bên để cột sáng mềm mại như thể tích không khí (Volumetric rim)
                float NdotV = abs(dot(normalize(i.worldNormal), i.viewDir));
                float rim = pow(saturate(1.0 - NdotV), _RimPower);

                // 3. Kết hợp ánh sáng rực rỡ ở tâm và viền tỏa sáng
                float alpha = (rim * 0.7 + 0.3) * verticalFade;

                fixed4 col = _TintColor * _Brightness * alpha;
                return col;
            }
            ENDCG
        }
    }
}
