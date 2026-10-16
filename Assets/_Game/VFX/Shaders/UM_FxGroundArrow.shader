// Ultimo Miglio - freccia guida del tutorial a terra (chevron doppio che scorre verso il bersaglio).
// Quad a terra UV 0..1 (v = avanti). Unlit trasparente, nessuna texture, animazione solo in shader (_Time): 0 costo CPU.
// WebGL2/GLES3 (target 3.0), SRP Batcher OK (un solo CBUFFER), ~20 ALU/pixel su un quad piccolo.
Shader "UltimoMiglio/FX/GroundArrow"
{
    Properties
    {
        _Color ("Colore (giallo sole #F4C430)", Color) = (0.957, 0.769, 0.188, 1)
        _EdgeColor ("Bordo (bruno #3B2A20)", Color) = (0.231, 0.165, 0.125, 1)
        _Alpha ("Alpha", Range(0,1)) = 1
        _Speed ("Scorrimento (cicli/s)", Range(0,3)) = 1.2
        _Width ("Spessore chevron", Range(0.05,0.3)) = 0.13
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-9" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color; half4 _EdgeColor; half _Alpha; half _Speed; half _Width;
            CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; return o; }

            // distanza da un chevron "^" con punta in (0.5, tip)
            half Chevron(float2 uv, half tip)
            {
                float2 p = float2(abs(uv.x - 0.5), tip - uv.y);
                return (half)abs(p.y - p.x * 0.9);
            }
            half4 frag(V i) : SV_Target
            {
                half t = frac(_Time.y * _Speed);
                half side = step(abs(i.uv.x - 0.5), 0.42h);
                half a = 0, e = 0;
                [unroll] for (int k = 0; k < 2; k++)
                {
                    half tip = 0.45h + 0.5h * frac(t + k * 0.5h);      // due chevron che scorrono in avanti
                    half fade = smoothstep(0.45h, 0.6h, tip) * smoothstep(0.98h, 0.85h, tip);
                    half d = Chevron(i.uv, tip);
                    a = max(a, smoothstep(_Width * 0.5h, _Width * 0.5h - 0.02h, d) * fade * side);
                    e = max(e, smoothstep(_Width * 0.5h + 0.035h, _Width * 0.5h + 0.015h, d) * fade * side);
                }
                half3 c = lerp(_EdgeColor.rgb, _Color.rgb, a);
                return half4(c, saturate(max(a, e * 0.8h)) * _Alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
