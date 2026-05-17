Shader "FF/CloudSprite"
{
    Properties
    {
        _MainTex ("Cloud Texture", 2D) = "white" {}
        _Alpha   ("Alpha",  Range(0, 1)) = 0.4
        _EdgeStart ("Edge Fade Start", Range(0.3, 0.9)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+2" }

        ZWrite Off
        ZTest  LEqual
        Blend  SrcAlpha OneMinusSrcAlpha
        Cull   Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float     _Alpha;
            float     _EdgeStart;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);

                // Multiplicar alpha × luminancia:
                // - Píxel transparente (a=0, rgb=cualquier cosa): density = 0 ✓
                // - Nube blanca opaca (a=1, rgb=1): density = 1 ✓
                // - Fondo oscuro sin alpha (a=1, rgb≈0): density ≈ 0 ✓
                // Funciona tanto con sprites transparentes como con fotos de nubes
                float lum     = (c.r + c.g + c.b) * 0.333;
                float density = c.a * lum;

                // Degradado radial: el borde siempre desaparece suavemente
                float2 centered = i.uv - 0.5;
                float  dist     = length(centered) * 2.0; // 0=centro 1=borde
                float  edge     = 1.0 - smoothstep(_EdgeStart, 1.0, dist);

                fixed  alpha = density * edge * _Alpha;
                return fixed4(1.0, 1.0, 1.0, alpha);
            }
            ENDCG
        }
    }
}
