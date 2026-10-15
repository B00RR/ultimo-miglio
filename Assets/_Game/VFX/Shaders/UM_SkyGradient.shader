// Ultimo Miglio - cielo a gradiente (v2.0): quad/sfera dietro la scena, unlit, 3 colori + orizzonte morbido. WebGL2 OK.
Shader "UltimoMiglio/FX/SkyGradient"
{
    Properties
    {
        _Top ("Top", Color) = (0.47,0.69,0.88,1)
        _Horizon ("Horizon", Color) = (0.99,0.86,0.67,1)
        _Bottom ("Bottom", Color) = (0.96,0.78,0.62,1)
        _HorizonY ("Horizon (0..1)", Range(0,1)) = 0.45
        _Soft ("Softness", Range(0.01,1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Top; half4 _Horizon; half4 _Bottom; half _HorizonY; half _Soft;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V vert(A i) { V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                half y = (half)i.uv.y;
                half up = smoothstep(_HorizonY, saturate(_HorizonY + _Soft), y);
                half dn = smoothstep(_HorizonY, max(_HorizonY - _Soft, 0.0h), y);
                half3 c = lerp(_Horizon.rgb, _Top.rgb, up);
                c = lerp(c, _Bottom.rgb, dn);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
