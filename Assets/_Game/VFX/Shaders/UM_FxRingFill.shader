// Ultimo Miglio - anello di riempimento del pad upgrade (versione VFX di UM_ZoneRing: stesse proprietà _Fill/_Color/_Pulse).
// Quad a terra con UV 0..1. Unlit trasparente, WebGL2/GLES3 (target 3.0), nessuna texture, compatibile SRP Batcher.
Shader "UltimoMiglio/FX/RingFill"
{
    Properties
    {
        _Color ("Fill Color", Color) = (0.957, 0.769, 0.188, 1)
        _TrackColor ("Track Color", Color) = (0.953, 0.89, 0.765, 0.3) // #alone: crema #F3E3C3 traslucido; il marrone scuro a progresso 0 creava l'alone scuro
        _GlowColor ("Edge Glow", Color) = (1, 0.957, 0.839, 1)
        _Fill ("Fill", Range(0,1)) = 0
        _Pulse ("Pulse", Range(0,1)) = 0
        _Flash ("Flash", Range(0,1)) = 0
        _Radius ("Radius", Range(0,0.5)) = 0.4
        _Thickness ("Thickness", Range(0,0.2)) = 0.07
        _Softness ("Edge Softness", Range(0.001,0.05)) = 0.012
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color; half4 _TrackColor; half4 _GlowColor;
                half _Fill; half _Pulse; half _Flash; half _Radius; half _Thickness; half _Softness;
            CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv - 0.5; return o; }
            half4 frag(V i) : SV_Target
            {
                float r = length(i.uv);
                float ang = atan2(i.uv.x, i.uv.y) / 6.2831853 + 0.5;          // 0..1, parte da "ore 6"
                half th = _Thickness * (1.0 + 0.25 * _Pulse + 0.6 * _Flash);
                half band = smoothstep(_Softness, 0, abs(r - _Radius) - th * 0.5);
                half filled = step(ang, _Fill);
                half head = smoothstep(0.03, 0.0, abs(ang - _Fill)) * step(0.001, _Fill) * step(_Fill, 0.999); // punta luminosa
                half4 c = lerp(_TrackColor, _Color, filled);
                c.rgb = lerp(c.rgb, _GlowColor.rgb, saturate(head + _Flash));
                // disco interno durante il flash
                half disc = smoothstep(_Radius, _Radius - 0.08, r) * _Flash * 0.45;
                c.a = saturate(c.a * band + disc);
                c.rgb = lerp(c.rgb, _GlowColor.rgb, disc > 0.001 ? _Flash : 0);
                return c;
            }
            ENDHLSL
        }
    }
}
