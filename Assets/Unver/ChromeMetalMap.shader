Shader "GG/ChromeMetalMap"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB) Smoothness (A)", 2D) = "white" {}
        [NoScaleOffset] _Normal ("Normal", 2D) = "bump" {}
        [NoScaleOffset] _Metalness ("Metalness", 2D) = "black" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Scale ("Scale", Range(0.0001,1)) = 0.1
        _Contrast ("Contrast", Range(0,10)) = 1
        _Amount ("Amount", Range(0,3)) = 1
        _Saturation ("Saturation", Range(0,1)) = 1
        _ContributeNormal ("ContributeNormal", Range(0,0.5)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _Normal;
        sampler2D _Metalness;
        
        half _Glossiness;
        half _Metallic;
        half _Scale;
        half _Contrast;
        half _Amount;
        fixed4 _Color;
        fixed _ContributeNormal;
        fixed _Saturation;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };
        
        ///////////////////////////////////////////////////////////////////////////////////////////
        //
        //  This part (hash and MyNoise functions) is copied as-is from:
        //  https://gist.github.com/nukeop/9877f5dc39f6ef57716a75e0aa0ffd27
        //  Thanks, nukeop!
        //  
        ///////////////////////////////////////////////////////////////////////////////////////////
        float hash(float n)
        {
            return frac(sin(n) * 43758.5453);
        }

        float MyNoise(float3 x)
        {
            // The noise function returns a value in the range -1.0f -> 1.0f

            float3 p = floor(x);
            float3 f = frac(x);

            f = f * f * (3.0 - 2.0 * f);
            float n = p.x + p.y * 57.0 + 113.0 * p.z;

            return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                    lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
        }
        //////////////////////////////
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            fixed4 metalColor =tex2D (_Metalness, IN.uv_MainTex); 
            o.Metallic = (metalColor.r + metalColor.g + metalColor.b) / 3;
            o.Smoothness = _Glossiness * metalColor.a;
            o.Normal = UnpackNormal (tex2D (_Normal, IN.uv_MainTex));
            
            fixed3 normalizedDir = lerp(normalize(IN.viewDir), o.Normal, _ContributeNormal);
            
            half3 xLerp = sin(IN.uv_MainTex.x / _Scale * normalizedDir.z * normalizedDir.y );
            half3 yLerp = cos(IN.uv_MainTex.y / _Scale * normalizedDir.z * normalizedDir.x );

            float distortion = MyNoise(xLerp) * normalizedDir.x * _Contrast;
            
            fixed3 xComponent = lerp(normalizedDir, -normalizedDir, xLerp) * distortion;
            fixed3 yComponent = lerp(-normalizedDir, normalizedDir, yLerp) * distortion;

            fixed3 emissionColor =(xComponent + yComponent) * _Amount * o.Smoothness;
            o.Emission = lerp((emissionColor.r + emissionColor.g + emissionColor.b) / 3, emissionColor, _Saturation);
        }
        ENDCG
    }
    FallBack "Diffuse"
}