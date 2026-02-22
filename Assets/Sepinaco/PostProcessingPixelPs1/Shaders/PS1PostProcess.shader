Shader "Sepinaco/PS1PostProcess"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Float) = 4.0
        _ColorDepth ("Color Depth (levels per channel)", Float) = 32.0
        _JitterIntensity ("Jitter Intensity", Float) = 0.002
        _JitterSpeed ("Jitter Speed", Float) = 30.0
        _DitherIntensity ("Dither Intensity", Float) = 0.03
        _Time2 ("Time Param", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _PixelSize;
            float _ColorDepth;
            float _JitterIntensity;
            float _JitterSpeed;
            float _DitherIntensity;
            float _Time2;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Fast pseudo-random based on screen coords
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Bayer 4x4 ordered dithering — branchless, Metal-compatible
            float bayer4x4(float2 pos)
            {
                float2 p = floor(fmod(pos, 4.0));

                float4 colSel = float4(
                    step(abs(p.x - 0.0), 0.5),
                    step(abs(p.x - 1.0), 0.5),
                    step(abs(p.x - 2.0), 0.5),
                    step(abs(p.x - 3.0), 0.5));

                float r0 = dot(float4( 0,  8,  2, 10), colSel);
                float r1 = dot(float4(12,  4, 14,  6), colSel);
                float r2 = dot(float4( 3, 11,  1,  9), colSel);
                float r3 = dot(float4(15,  7, 13,  5), colSel);

                float4 rowSel = float4(
                    step(abs(p.y - 0.0), 0.5),
                    step(abs(p.y - 1.0), 0.5),
                    step(abs(p.y - 2.0), 0.5),
                    step(abs(p.y - 3.0), 0.5));

                return dot(float4(r0, r1, r2, r3), rowSel) / 16.0;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // --- 1. Pixelation: snap UVs to a low-resolution grid ---
                float2 resolution = _MainTex_TexelSize.zw; // screen size in pixels
                float2 pixelGrid = floor(resolution / _PixelSize);
                float2 snappedUV = floor(uv * pixelGrid) / pixelGrid;

                // --- 2. PS1 texture jitter: per-"pixel" UV displacement ---
                float timeSlice = floor(_Time2 * _JitterSpeed);
                float2 jitterSeed = snappedUV * 137.0 + timeSlice;
                float2 jitter = float2(
                    hash(jitterSeed) - 0.5,
                    hash(jitterSeed + 71.7) - 0.5
                ) * _JitterIntensity;

                float2 finalUV = clamp(snappedUV + jitter, 0.0, 1.0);

                fixed4 col = tex2D(_MainTex, finalUV);

                // --- 3. Ordered dithering (Bayer 4x4) ---
                float2 screenPos = snappedUV * pixelGrid;
                float dither = (bayer4x4(screenPos) - 0.5) * _DitherIntensity;
                col.rgb += dither;

                // --- 4. Color depth reduction (PS1: 5 bits = 32 levels per channel) ---
                float levels = max(_ColorDepth, 2.0);
                col.rgb = floor(col.rgb * (levels - 1.0) + 0.5) / (levels - 1.0);

                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
