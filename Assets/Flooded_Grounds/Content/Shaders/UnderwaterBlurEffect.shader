Shader "Hidden/UnderwaterBlurEffect"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BlurIntensity ("Blur Intensity", Range(0, 10)) = 4.8
        _Distortion ("Distortion", Range(0, 1)) = 0.16
        _UnderwaterColor ("Water Color", Color) = (0.06, 0.38, 0.48, 1.0)
        _DeepWaterColor ("Deep Color", Color) = (0.02, 0.15, 0.22, 1.0)
        _VignettePower ("Vignette Power", Range(0, 5)) = 1.5
        _Transition ("Transition", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurIntensity;
            float _Distortion;
            float4 _UnderwaterColor;
            float4 _DeepWaterColor;
            float _VignettePower;
            float _Transition;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_Transition <= 0.001)
                {
                    return tex2D(_MainTex, i.uv);
                }

                // 1. Gợn sóng khúc xạ ánh sáng đại dương tự nhiên (Organic Ocean Wave Refraction)
                float time = _Time.y * 1.8;
                float waveX = sin(i.uv.y * 24.0 + time) * 0.0045 * _Distortion * _Transition;
                float waveY = cos(i.uv.x * 20.0 + time * 1.2) * 0.0038 * _Distortion * _Transition;
                waveX += sin(i.uv.y * 48.0 - time * 2.5) * 0.0018 * _Distortion * _Transition;
                waveY += cos(i.uv.x * 42.0 + time * 2.2) * 0.0015 * _Distortion * _Transition;

                float2 distUV = i.uv + float2(waveX, waveY);

                // 2. Mờ quang học mất tiêu cự sinh học (16-Tap Concentric Vogel Spiral Blur)
                // Giác mạc người (n=1.376) tiếp xúc với nước biển (n=1.333) mất 70% khúc xạ hội tụ,
                // tiêu điểm rơi tít phía sau võng mạc (+43 Diopter), tạo vòng mờ Bokeh hình cầu tự nhiên.
                float blurRadius = _BlurIntensity * _Transition * 3.8;
                float2 texel = _MainTex_TexelSize.xy * blurRadius;

                // 16 điểm lấy mẫu theo đường xoắn ốc Fibonacci phân bố đều theo diện tích vòng tròn
                float2 spiral[16];
                spiral[0]  = float2( 0.177,  0.000);
                spiral[1]  = float2(-0.226,  0.207);
                spiral[2]  = float2( 0.035, -0.394);
                spiral[3]  = float2( 0.285,  0.371);
                spiral[4]  = float2(-0.522, -0.092);
                spiral[5]  = float2( 0.495, -0.315);
                spiral[6]  = float2(-0.165,  0.616);
                spiral[7]  = float2(-0.316, -0.608);
                spiral[8]  = float2( 0.685,  0.250);
                spiral[9]  = float2(-0.712,  0.294);
                spiral[10] = float2( 0.343, -0.734);
                spiral[11] = float2( 0.254,  0.809);
                spiral[12] = float2(-0.765, -0.443);
                spiral[13] = float2( 0.897, -0.197);
                spiral[14] = float2(-0.548,  0.779);
                spiral[15] = float2(-0.126, -0.976);

                half4 accumColor = tex2D(_MainTex, distUV) * 0.18;
                float totalWeight = 0.18;

                // Lấy mẫu với độ lệch sắc sai (Chromatic Dispersion) quang học
                float chromScale = 0.0035 * blurRadius;

                for (int k = 0; k < 16; k++)
                {
                    float2 sampleOffset = spiral[k] * texel;
                    float2 sampleUV = distUV + sampleOffset;

                    half r = tex2D(_MainTex, sampleUV + float2(chromScale, 0.0)).r;
                    half g = tex2D(_MainTex, sampleUV).g;
                    half b = tex2D(_MainTex, sampleUV - float2(chromScale, 0.0)).b;
                    half a = tex2D(_MainTex, sampleUV).a;

                    half4 col = half4(r, g, b, a);

                    float distFromCenter = length(spiral[k]);
                    float weight = 1.0 - distFromCenter * 0.45;

                    accumColor += col * weight;
                    totalWeight += weight;
                }

                half4 blurredCol = accumColor / totalWeight;

                // 3. Hấp thụ quang phổ nước biển sâu (Red light absorption -> Aqua/Teal dominant)
                half3 seaColor = lerp(blurredCol.rgb, _UnderwaterColor.rgb, 0.40);
                seaColor = seaColor * _DeepWaterColor.rgb * 2.25;

                // 4. Viền tối góc nhìn lặn sâu (Pressure Eye Vignette)
                float2 centerUV = (i.uv - 0.5) * 2.0;
                float vig = saturate(1.0 - dot(centerUV, centerUV) * 0.32 * _VignettePower);
                seaColor *= (vig * 0.68 + 0.32);

                half4 finalColor = half4(seaColor, blurredCol.a);
                half4 orig = tex2D(_MainTex, i.uv);

                return lerp(orig, finalColor, _Transition);
            }
            ENDCG
        }
    }
    FallBack Off
}
