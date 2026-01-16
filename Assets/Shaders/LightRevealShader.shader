Shader "Custom/LightRevealShader"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _RevealColor ("Reveal Color", Color) = (1,1,1,1)
        _BlackoutColor ("Blackout Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Overlay"
        }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float4 _RevealColor;
            float4 _BlackoutColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the mask texture
                fixed4 mask = tex2D(_MaskTex, i.uv);

                // If mask alpha > 0, show the revealed area (transparent/scene visible)
                // Otherwise show blackout
                float maskValue = mask.a;

                // Return blackout color where mask is 0, transparent where mask exists
                return lerp(_BlackoutColor, fixed4(0,0,0,0), maskValue);
            }
            ENDCG
        }
    }
}
