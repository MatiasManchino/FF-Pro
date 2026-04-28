// EarthBlendShader.shader
Shader "Custom/EarthBlend"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BlendTex ("Blend Texture", 2D) = "white" {}
        _Blend ("Blend Amount", Range(0, 1)) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        CGPROGRAM
        #pragma surface surf Standard
        
        struct Input
        {
            float2 uv_MainTex;
        };
        
        sampler2D _MainTex;
        sampler2D _BlendTex;
        float _Blend;
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 main = tex2D (_MainTex, IN.uv_MainTex);
            fixed4 blend = tex2D (_BlendTex, IN.uv_MainTex);
            o.Albedo = lerp(main, blend, _Blend).rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}