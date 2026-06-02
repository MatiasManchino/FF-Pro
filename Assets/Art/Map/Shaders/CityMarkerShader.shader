Shader "FF/CityMarker"
{
    Properties
    {
        _Color      ("Color base",      Color)  = (1, 0.88, 0, 1)
        _Selected   ("Selected",        Float)  = 0
        _PulseSpeed ("Pulse Speed",     Float)  = 0.45
        _Brightness ("Brightness",      Float)  = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+4" }

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

            fixed4 _Color;
            float  _Selected;
            float  _PulseSpeed;
            float  _Brightness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv   = i.uv - 0.5;
                float  dist = length(uv) * 2.0;  // 0 = centro, 1 = borde del quad

                // ── Núcleo sólido ─────────────────────────────────────────────
                float core = 1.0 - smoothstep(0.09, 0.13, dist);

                // ── Anillo interior (fino) ────────────────────────────────────
                float ring1 = smoothstep(0.21, 0.25, dist) - smoothstep(0.25, 0.28, dist);

                // ── Anillo exterior (más grueso, el "borde" principal) ────────
                float ring2 = smoothstep(0.32, 0.36, dist) - smoothstep(0.36, 0.42, dist);

                // ── Tick marks: 4 marcas cortas entre los anillos ─────────────
                // Normalizar dirección y medir perpendicular a ejes
                float2 uvN = uv / max(0.001, length(uv));
                float  inGap = step(0.28, dist) * step(dist, 0.32);
                float  tickH = (1.0 - smoothstep(0.0, 0.10, abs(uvN.y))) * inGap;
                float  tickV = (1.0 - smoothstep(0.0, 0.10, abs(uvN.x))) * inGap;
                float  ticks = saturate(tickH + tickV) * 0.55;

                // ── Pulso expansivo ────────────────────────────────────────────
                float  t          = frac(_Time.y * _PulseSpeed);
                float  pulseR     = 0.42 + t * 0.50;           // expande 0.42 → 0.92
                float  pulseW     = lerp(0.045, 0.020, t);     // se adelgaza al crecer
                float  pulse      = smoothstep(pulseR - pulseW, pulseR,         dist)
                                  - smoothstep(pulseR,          pulseR + pulseW, dist);
                float  pulseAlpha = pow(1.0 - t, 1.8) * 0.70;

                // ── Anillo de selección (hover) ────────────────────────────────
                float  selRing = (smoothstep(0.14, 0.17, dist) - smoothstep(0.17, 0.21, dist))
                                 * _Selected;

                // ── Combinar alpha ─────────────────────────────────────────────
                float solidA = saturate(core + ring1 * 0.85 + ring2 * 0.90 + ticks);
                float totalA = saturate(solidA + pulse * pulseAlpha + selRing * 0.65);

                if (totalA < 0.008) discard;

                // ── Color ──────────────────────────────────────────────────────
                fixed3 base    = _Color.rgb;
                fixed3 coreCol = lerp(base, fixed3(1, 1, 0.85), 0.65);   // núcleo casi blanco
                fixed3 selCol  = fixed3(1.0, 0.55, 0.05);                  // naranja-ámbar al hover
                fixed3 pulCol  = lerp(base, fixed3(1, 1, 1), 0.35);        // pulse más claro

                fixed3 col = base;
                col = lerp(col, coreCol,  core);
                col = lerp(col, pulCol,   pulse * pulseAlpha * 0.50);
                col = lerp(col, selCol,   selRing * _Selected);

                col *= _Brightness;

                return fixed4(col, totalA * _Color.a);
            }
            ENDCG
        }
    }
}
