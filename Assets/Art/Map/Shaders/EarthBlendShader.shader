// EarthBlendShader.shader
Shader "Custom/EarthBlend"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BlendTex ("Blend Texture", 2D) = "white" {}
        _Blend ("Blend Amount", Range(0, 1)) = 0
        _NightTex ("Night Texture", 2D) = "black" {}
        _SunDir ("Sun Direction", Vector) = (1, 0, 0, 0)
        _TerminatorSoftness ("Terminator Softness", Range(0.01, 0.3)) = 0.08
        _NightBrightness ("Night Brightness", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        CGPROGRAM
        #pragma surface surf Standard vertex:vert

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNorm;
        };

        sampler2D _MainTex;
        sampler2D _BlendTex;
        float _Blend;
        sampler2D _NightTex;
        float3 _SunDir;
        float _TerminatorSoftness;
        float _NightBrightness;

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.worldNorm = UnityObjectToWorldNormal(v.normal);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 dayA  = tex2D(_MainTex,  IN.uv_MainTex);
            fixed4 dayB  = tex2D(_BlendTex, IN.uv_MainTex);
            fixed4 day   = lerp(dayA, dayB, _Blend);
            fixed4 night = tex2D(_NightTex, IN.uv_MainTex);

            float dotSun    = dot(normalize(IN.worldNorm), normalize(_SunDir));
            float dayFactor = smoothstep(-_TerminatorSoftness, _TerminatorSoftness, dotSun);

            // Day texture lit by the sun; city lights emissive (not affected by lighting)
            o.Albedo   = day.rgb * dayFactor;
            o.Emission = night.rgb * (1.0 - dayFactor) * _NightBrightness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}