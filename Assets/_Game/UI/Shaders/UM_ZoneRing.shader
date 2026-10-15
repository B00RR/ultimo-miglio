// Fallback ground ring for ProximityZone / UpgradePad (04_UI §2.1). VFX may replace it with the toon version;
// keep the _Fill / _Color / _Pulse properties. Put on a flat quad (UV 0..1) lying on the floor.
Shader "UltimoMiglio/UI/ZoneRing"
{
    Properties
    {
        _Color ("Fill Color", Color) = (0.957, 0.769, 0.188, 1)
        _TrackColor ("Track Color", Color) = (0.231, 0.165, 0.125, 0.45)
        _Fill ("Fill", Range(0,1)) = 0
        _Pulse ("Pulse", Range(0,1)) = 0
        _Radius ("Radius", Range(0,0.5)) = 0.42
        _Thickness ("Thickness", Range(0,0.2)) = 0.06
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
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
            half4 frag (V i) : SV_Target
            {
                float2 p = i.uv - 0.5;
                float r = length(p);
                float t = _Thickness * (1 + _Pulse * 0.4);
                float ring = 1 - smoothstep(t * 0.5 - fwidth(r), t * 0.5 + fwidth(r), abs(r - _Radius));
                float ang = frac(atan2(p.x, p.y) / 6.2831853 + 1.0);   // 0 at top, clockwise
                half4 c = ang <= _Fill ? _Color : _TrackColor;
                c.a *= ring;
                return c;
            }
            ENDHLSL
        }
    }
}
