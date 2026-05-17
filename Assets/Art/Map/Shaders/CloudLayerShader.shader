Shader "Custom/CloudLayer"
{
    Properties
    {
        _MainTex  ("Cloud Texture", 2D) = "white" {}
        _Alpha    ("Global Alpha",  Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        CGPROGRAM
        #pragma surface surf Lambert alpha:fade noforwardadd

        struct Input
        {
            float2 uv_MainTex;
        };

        sampler2D _MainTex;
        float     _Alpha;

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            // Cloud maps son grises/blancos — usamos luminancia como densidad
            float density = (c.r + c.g + c.b) * 0.333;
            o.Albedo = fixed3(1, 1, 1);
            o.Alpha  = density * _Alpha;
        }
        ENDCG
    }
}
