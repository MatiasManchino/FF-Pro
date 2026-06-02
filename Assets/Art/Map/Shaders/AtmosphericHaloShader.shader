Shader "Custom/AtmosphericHalo"
{
    Properties
    {
        _HaloColor      ("Halo Color",      Color)        = (0.9, 1, 1, 1)
        _SunDir         ("Sun Direction",   Vector)       = (0.9, 0.7, 0.7, 1)
        _FresnelPow     ("Fresnel Power",   Range(1, 8))  = 2.0
        _Intensity      ("Intensity",       Range(0, 6))  = 6
        _SunWrap        ("Sun Wrap",        Range(-1, 1)) = 0.9
        _BacklitFactor  ("Backlit Factor",  Range(0, 1))  = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent+2" "RenderType"="Transparent" }
        Blend SrcAlpha One   // Additive: el halo suma luz, no mezcla
        Cull Back
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _HaloColor;
            float3 _SunDir;
            float  _FresnelPow;
            float  _Intensity;
            float  _SunWrap;
            float  _BacklitFactor;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f
            {
                float4 pos         : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos         = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);

                // Fresnel: máximo en el limbo (ángulo rasante cámara-superficie)
                float fresnel = pow(1.0 - saturate(dot(N, V)), _FresnelPow);

                // Factor solar: halo visible donde el sol ilumina + wrap suave al terminador
                float sunFactor = saturate(dot(N, normalize(_SunDir)) + _SunWrap);

                // Suprimir cuando la cámara está del lado iluminado
                float alpha = fresnel * sunFactor * _Intensity * _BacklitFactor;

                return float4(_HaloColor.rgb, saturate(alpha));
            }
            ENDCG
        }
    }
}
