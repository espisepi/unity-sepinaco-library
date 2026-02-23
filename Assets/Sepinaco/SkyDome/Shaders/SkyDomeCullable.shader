Shader "Sepinaco/SkyDome"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Color", Color) = (0.53, 0.72, 0.96, 1.0)
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Modo Cull", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background+1" }
        LOD 100

        Cull [_Cull]
        ZWrite Off
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Force depth to the far clip plane so the dome is never
                // clipped regardless of its scale or camera far-clip distance.
                // Same technique Unity uses internally for the built-in Skybox.
                #if defined(UNITY_REVERSED_Z)
                o.vertex.z = 1.0e-6;
                #else
                o.vertex.z = o.vertex.w - 1.0e-6;
                #endif

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Color"
}
