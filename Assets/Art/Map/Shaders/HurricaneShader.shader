Shader "FF/HurricaneSprite"
{
    Properties
    {
        _MainTex       ("Hurricane Texture", 2D) = "white" {}
        _Alpha         ("Alpha",          Range(0, 1)) = 0.8
        _BuildProgress ("Build Progress", Range(0, 1)) = 0.0
        _Rotation      ("Rotation (rad)", Float)       = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+3" }

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
            float     _BuildProgress;
            float     _Rotation;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Rotar UVs alrededor del centro (el ojo del huracán)
                float2 centered = i.uv - 0.5;
                float  sinR = sin(_Rotation);
                float  cosR = cos(_Rotation);
                float2 rotated = float2(
                    cosR * centered.x - sinR * centered.y,
                    sinR * centered.x + cosR * centered.y
                );
                float2 uv = rotated + 0.5;

                // Si la rotación saca el UV fuera del quad → transparent
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    return fixed4(0, 0, 0, 0);

                fixed4 c = tex2D(_MainTex, uv);

                // Luminancia → densidad
                float density = (c.r + c.g + c.b) * 0.333;
                density = max(density, c.a);

                float dist = length(centered) * 2.0; // 0=centro, 1=borde

                // Animación: aparece DESDE los bordes hacia el centro
                // threshold empieza en 1 (nada visible) y baja a 0 (todo visible)
                float threshold = 1.0 - _BuildProgress;
                float reveal    = smoothstep(threshold - 0.20, threshold + 0.05, dist);

                // Borde exterior siempre se desvanece
                float edgeFade = 1.0 - smoothstep(0.78, 1.00, dist);

                float alpha = density * reveal * edgeFade * _Alpha;
                return fixed4(1.0, 1.0, 1.0, alpha);
            }
            ENDCG
        }
    }
}
