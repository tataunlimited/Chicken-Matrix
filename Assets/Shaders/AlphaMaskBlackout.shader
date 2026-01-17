Shader "Custom/AlphaMaskBlackout"
{
    Properties
    {
        _MaskTex ("Mask Texture", 2D) = "black" {}
        _BlackoutColor ("Blackout Color", Color) = (0,0,0,1)
        _Softness ("Edge Softness", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 screenUV : TEXCOORD1;
            };

            sampler2D _MaskTex;
            float4 _MaskTex_ST;
            float4 _BlackoutColor;
            float _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MaskTex);

                // Calculate screen UV for mask sampling
                o.screenUV = ComputeScreenPos(o.pos).xy / ComputeScreenPos(o.pos).w;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the mask texture using screen space UVs
                fixed4 mask = tex2D(_MaskTex, i.screenUV);

                // Use mask alpha to determine visibility
                // Higher alpha in mask = more transparent blackout (reveals scene)
                float reveal = mask.a;

                // Apply softness
                reveal = smoothstep(0, _Softness, reveal);

                // Output blackout color with alpha based on reveal
                return float4(_BlackoutColor.rgb, _BlackoutColor.a * (1 - reveal));
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
