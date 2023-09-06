Shader "Custom/ImageOverlayShader"
{
    Properties
    {
        _MainTex1 ("Image1", 2D) = "white" {}
        _MainTex2 ("Image2", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex1;
            sampler2D _MainTex2;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col1 = tex2D(_MainTex1, i.uv);
                half4 col2 = tex2D(_MainTex2, i.uv);
                half4 finalColor = lerp(col2, col1, col1.a);
                return finalColor;
            }
            ENDCG
        }
    }
}
