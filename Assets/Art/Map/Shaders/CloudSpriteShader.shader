Shader "FF/CloudSprite"
{
    Properties
    {
        _MainTex    ("Cloud Texture", 2D)          = "white" {}
        _Alpha      ("Alpha",  Range(0, 1))         = 0.4
        _EdgeStart  ("Edge Fade Start", Range(0.3, 0.9)) = 0.55
        _NightFactor("Night Factor", Range(0, 1))  = 1.0
        _StretchDir ("Stretch Direction XY", Vector) = (1, 0, 0, 0)
        _TipStretch ("Tip Stretch", Range(0, 0.8))   = 0.15
        _SphereR    ("Sphere Radius at Clouds", Float) = 1060
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
            float     _NightFactor;
            float4    _StretchDir;
            float     _TipStretch;
            float     _SphereR;

            v2f vert(appdata v)
            {
                // 1. Estiramiento de puntas: los extremos del quad se elongan en la
                //    dirección de traslado, el centro permanece fijo → forma de "cometa".
                float proj   = dot(v.vertex.xy, _StretchDir.xy);
                v.vertex.xy += _StretchDir.xy * (proj * _TipStretch * 1.5);

                // 2. Curvatura esférica: aproximación parabólica de la superficie del globo.
                //    Se extrae la escala world del model matrix para convertir coords de objeto
                //    a unidades world antes de calcular la sagita → z se hunde hacia la Tierra.
                float scaleX = length(unity_ObjectToWorld._m00_m10_m20);
                float scaleY = length(unity_ObjectToWorld._m01_m11_m21);
                float wx     = v.vertex.x * scaleX;
                float wy     = v.vertex.y * scaleY;
                v.vertex.z  -= (wx * wx + wy * wy) / (2.0 * _SphereR);

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
                float lum     = (c.r + c.g + c.b) * 0.353;
                float density = c.a * lum;

                // Degradado radial: el borde siempre desaparece suavemente
                float2 centered = i.uv - 0.2;
                float  dist     = length(centered) * 2.0; // 0=centro 1=borde
                float  edge     = 1.0 - smoothstep(_EdgeStart, 1.0, dist);

                fixed  alpha = density * edge * _Alpha;

                // Día: sin modificación (blanco puro = igual que antes).
                // Noche: leve oscurecimiento azulado, sutil.
                fixed3 dayCol   = fixed3(0.68,  0.7,  0.8);
                fixed3 nightCol = fixed3(0.08, 0.10, 0.20);
                fixed3 col      = lerp(nightCol, dayCol, _NightFactor);

                // De noche el fondo es casi negro: subir el alpha compensa que col*alpha
                // resulte invisible. De día no toca nada (boost = 1.0).
                fixed boost      = lerp(1.50, 1.0, _NightFactor);
                fixed finalAlpha = saturate(alpha * boost);

                return fixed4(col, finalAlpha);
            }
            ENDCG
        }
    }
}
