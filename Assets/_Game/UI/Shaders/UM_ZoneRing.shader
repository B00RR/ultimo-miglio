// Zona a terra stile pad: riempimento pieno tenue + bordo bruno pulito (no tratteggio/griglia).
Shader "UltimoMiglio/UI/ZoneRing"
{
    Properties
    {
        _Color ("Fill Color", Color) = (0.957, 0.769, 0.188, 0.72)
        _TrackColor ("Border Color", Color) = (0.231, 0.165, 0.125, 0.95)
        _Fill ("Fill", Range(0,1)) = 0
        _Pulse ("Pulse", Range(0,1)) = 0
        _Radius ("Corner Soft", Range(0,0.5)) = 0.14
        _Thickness ("Border Thickness", Range(0,0.2)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Color; half4 _TrackColor; half _Fill; half _Pulse; half _Radius; half _Thickness;
            CBUFFER_END
            V vert (A i) { V o; UNITY_SETUP_INSTANCE_ID(i); o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; return o; }
            float sdRoundBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }
            half4 frag (V i) : SV_Target
            {
                float2 p = i.uv - 0.5;
                float aa = max(fwidth(p.x), fwidth(p.y)) * 1.25;
                float th = _Thickness * (1.0 + _Pulse * 0.2);
                float corner = max(0.04, _Radius);
                float2 halfSize = float2(0.5 - th * 0.35, 0.5 - th * 0.35);
                float d = sdRoundBox(p, halfSize, corner);
                float inside = 1.0 - smoothstep(-aa, aa, d);
                // Opaco abbastanza da non far leggere la fuga del pavimento come "griglia gialla"
                half fillA = lerp(0.55h, 0.82h, _Fill) * (1.0h + _Pulse * 0.08h);
                half4 fill = _Color; fill.a = fillA * inside;
                float border = 1.0 - smoothstep(th * 0.55 - aa, th * 0.55 + aa, abs(d));
                half4 edge = _TrackColor; edge.a *= border * lerp(0.85h, 1.0h, _Fill);
                half4 c = fill;
                c.rgb = lerp(c.rgb, edge.rgb, saturate(edge.a));
                c.a = max(fill.a, edge.a);
                return c;
            }
            ENDHLSL
        }
    }
}
