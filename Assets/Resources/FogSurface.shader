Shader "SGPR/Fog Surface"
{
    Properties
    {
        _Color ("Fog Color", Color) = (0.58, 0.60, 0.62, 1)
        _Opacity ("Surface Opacity", Range(0, 1)) = 0.4
        _HorizonOpacity ("Horizon Opacity", Range(0, 1)) = 0.95
        _IntersectionFade ("Intersection Fade", Float) = 1.5
        _PatternSize ("Pattern Size", Float) = 80
        _DriftSpeed ("Drift Speed", Vector) = (0.4, 0.15, 0, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _Color, _DriftSpeed;
            float _Opacity, _HorizonOpacity, _IntersectionFade, _PatternSize;

            struct Attributes { float4 position : POSITION; };
            struct Varyings
            {
                float4 position : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
                float3 worldPosition : TEXCOORD1;
                float eyeDepth : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.screenPosition = ComputeScreenPos(output.position);
                output.worldPosition = mul(unity_ObjectToWorld, input.position).xyz;
                output.eyeDepth = -UnityObjectToViewPos(input.position).z;
                output.worldNormal = UnityObjectToWorldNormal(float3(0, 1, 0));
                return output;
            }
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            float Noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                            lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), f.x), f.y);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 view = normalize(_WorldSpaceCameraPos.xyz - input.worldPosition);
                float grazing = pow(1 - saturate(abs(dot(view, normalize(input.worldNormal)))), 3);
                float2 pattern = (input.worldPosition.xz - _Time.y * _DriftSpeed.xy) / max(1, _PatternSize);
                float variation = lerp(0.85, 1.0, Noise(pattern) * 0.7 + Noise(pattern * 2.1) * 0.3);
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,
                    UNITY_PROJ_COORD(input.screenPosition)));
                float softIntersection = saturate((depth - input.eyeDepth) / max(0.01, _IntersectionFade));
                float alpha = lerp(_Opacity * variation, _HorizonOpacity, grazing) * softIntersection * _Color.a;
                return half4(_Color.rgb, saturate(alpha));
            }
            ENDCG
        }
    }
    Fallback Off
}
