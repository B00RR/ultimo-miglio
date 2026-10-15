// Ultimo Miglio - "Diorama Solare" toon shader (URP, mobile, SRP Batcher compatible)
// WebGL2/GLES3 safe: target 3.0, no compute/geometry/tessellation, no SRV arrays, few keywords.
// Single main light, stepped soft ramp (2-3 bands), warm shadow tint (never pure black), rim,
// receives main-light shadows, fog. Optional inverted-hull outline pass (pass "SRPDefaultUnlit"):
// Per-material toggle _OutlineOn; UM_QualityTier enables the pass only when _OutlineOn=1 AND tier Alto (else pass disabled = zero cost).
Shader "UltimoMiglio/ToonDiorama"
{
    Properties
    {
        [MainTexture] _BaseMap ("Palette Atlas (sRGB, no mip, Point)", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        [Header(Ramp)]
        [IntRange] _Bands ("Bands", Range(2,3)) = 3
        _RampSoftness ("Band Softness", Range(0.001,0.3)) = 0.06
        _RampOffset ("Terminator Offset", Range(-0.5,0.5)) = 0.0
        _ShadowTint ("Shadow Tint (viola fresco, v2.0)", Color) = (0.55,0.50,0.72,1)
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.6
        [Header(Rim)]
        _RimColor ("Rim Color (Crema)", Color) = (1,0.957,0.839,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 3.5
        _RimStrength ("Rim Strength", Range(0,1)) = 0.3
        [Header(Variazione mondo)]
        _NoiseStrength ("Floor/Wall Tint Variation (0=off)", Range(0,0.2)) = 0
        _NoiseScale ("Variation Scale (1/m)", Range(0.05,2)) = 0.35
        _TileVar ("Floor Tile Variation", Range(0,0.3)) = 0
        _TileSize ("Floor Tile Size (m)", Range(0.25,4)) = 1
        _FloorTint ("Floor Warm Tint", Color) = (1,1,1,1)
        [Header(Profondita v2.0)]
        _HeightDark ("Height Gradient (base darker)", Range(0,0.5)) = 0.18
        _HeightRange ("Height Range (m)", Range(0.5,6)) = 2.5
        _WallAO ("Wall Base AO", Range(0,0.6)) = 0.3
        _WallAOHeight ("Wall AO Height (m)", Range(0.05,1.5)) = 0.5
        _TopLight ("Top Face Highlight", Range(0,0.4)) = 0.1
        [Header(Emission)]
        _EmissionColor ("Emission (night lamps)", Color) = (0,0,0,1)
        [Header(Outline)]
        [Toggle] _OutlineOn ("Outline (chars/robots/interactive only)", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0.231,0.184,0.165,1)
        _OutlineWidth ("Outline Width (m)", Range(0,0.05)) = 0.012
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Bands; half _RampSoftness; half _RampOffset;
            half4 _ShadowTint; half _ShadowStrength;
            half4 _RimColor; half _RimPower; half _RimStrength;
            half4 _EmissionColor; half _NoiseStrength; half _NoiseScale;
            half _TileVar; half _TileSize; half4 _FloorTint;
            half _HeightDark; half _HeightRange; half _WallAO; half _WallAOHeight; half _TopLight;
            half4 _OutlineColor; half _OutlineWidth; half _OutlineOn;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        // Globali v2.0 (UM_LookSettings): grading in-shader (post OFF su WebGL) + vignetta a terra in world-space
        half _UM_Grade; half _UM_Exposure; half _UM_Contrast; half _UM_Saturation; half _UM_Glow; half4 _UM_Lift;
        float4 _UM_Focus; half _UM_VignetteRange; half _UM_VignetteDark; half4 _UM_VignetteColor;
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT  // toon uses step() anyway; kept for URP keyword parity
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float2 uvLM:TEXCOORD1; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings {
                float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1;
                half3 normalWS:TEXCOORD2; half4 fogAndVL:TEXCOORD3; // xyz vertex add. lights, w fog
                DECLARE_LIGHTMAP_OR_SH(uvLM, sh, 4); half vao:TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i, o);
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap); o.vao = i.color.a; // AO di vertice (alpha), 1 = nessuna
                OUTPUT_LIGHTMAP_UV(i.uvLM, unity_LightmapST, o.uvLM);
                OUTPUT_SH(o.normalWS, o.sh);
                half3 vl = 0;
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    vl = VertexLighting(p.positionWS, o.normalWS);
                #endif
                o.fogAndVL = half4(vl, ComputeFogFactor(p.positionCS.z));
                return o;
            }

            float Hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            half ValueNoise(float2 p)
            {
                float2 i0 = floor(p); float2 f = frac(p); f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i0), b = Hash21(i0 + float2(1, 0)), c = Hash21(i0 + float2(0, 1)), d = Hash21(i0 + 1);
                return (half)lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            half Ramp(half ndl)
            {
                half x = saturate(ndl * 0.5h + 0.5h + _RampOffset);   // half-lambert
                half steps = _Bands - 1.0h;
                half s = x * steps;
                half f = floor(s);
                half t = smoothstep(0.5h - _RampSoftness * steps, 0.5h + _RampSoftness * steps, frac(s));
                return saturate((f + t) / steps);
            }

            // Tonemap morbido + contrasto + saturazione + lift viola + "fake bloom" (solo ALU)
            half3 Grade(half3 c)
            {
                c *= _UM_Exposure;
                half lum = dot(c, half3(0.299h, 0.587h, 0.114h));
                c += c * smoothstep(0.65h, 1.1h, lum) * _UM_Glow * 0.35h;
                c = c / (1.0h + c * 0.18h) * 1.18h;
                c = saturate((c - 0.5h) * _UM_Contrast + 0.5h);
                lum = dot(c, half3(0.299h, 0.587h, 0.114h));
                c = lerp(lum.xxx, c, _UM_Saturation);
                c = c + _UM_Lift.rgb * (1.0h - c) * 0.35h * (1.0h - lum);
                return saturate(c);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                // Cheap low-frequency world-space variation (2 value-noise octaves, ALU only, no texture). 0 = off.
                if (_NoiseStrength > 0.001h)
                {
                    float2 p = i.positionWS.xz * _NoiseScale;
                    half nv = ValueNoise(p) * 0.65h + ValueNoise(p * 2.7 + 17.3) * 0.35h;
                    albedo.rgb *= 1.0h + (nv - 0.5h) * 2.0h * _NoiseStrength;
                }
                half3 n = normalize(i.normalWS);
                half up = saturate(n.y);
                half isFloor = step(0.9h, up) * (1.0h - step(0.25h, (half)i.positionWS.y));
                if (_TileVar > 0.001h && isFloor > 0.5h)
                {
                    float2 cell = floor(i.positionWS.xz / _TileSize);
                    albedo.rgb *= lerp(1.0h.xxx, _FloorTint.rgb, 1.0h) * (1.0h + ((half)Hash21(cell) - 0.5h) * 2.0h * _TileVar);
                }
                float4 sc = TransformWorldToShadowCoord(i.positionWS);
                Light L = GetMainLight(sc);
                half ramp = Ramp(dot(n, L.direction));
                half shadow = smoothstep(0.3h, 0.7h, L.shadowAttenuation); // toon edge, but keeps PCF filtering (no stair-stepping)
                half lit = min(ramp, shadow);
                half3 shadowCol = lerp(1.0h.xxx, _ShadowTint.rgb, _ShadowStrength);
                half3 direct = lerp(shadowCol, L.color, lit); // v2.0: ombre colorate (tinta fredda) contro luce calda
                half3 ambient = SAMPLE_GI(i.uvLM, i.sh, n) * 0.3h;
                half3 col = albedo.rgb * (direct * 0.85h + ambient);
                float3 pw = i.positionWS;
                half hgt = saturate((half)pw.y / _HeightRange);
                col *= lerp(1.0h - _HeightDark, 1.0h, hgt);
                half vertical = 1.0h - up;
                col *= (1.0h - _WallAO * vertical * (1.0h - smoothstep(0.0h, _WallAOHeight, (half)pw.y))) * lerp(0.55h, 1.0h, i.vao);
                col *= 1.0h + _TopLight * smoothstep(0.7h, 0.95h, n.y) * saturate((half)pw.y * 2.0h) * lit;
                #ifdef _ADDITIONAL_LIGHTS
                    uint cnt = GetAdditionalLightsCount();
                    for (uint li = 0u; li < cnt; ++li) {
                        Light a = GetAdditionalLight(li, i.positionWS);
                        half d = smoothstep(0.0h, 0.6h, saturate(dot(n, a.direction)) * a.distanceAttenuation); // v1.4: wide soft falloff, no hard pool edge
                        col += albedo.rgb * a.color * d * 0.6h; // energy compensates the wider falloff
                    }
                #endif
                col += albedo.rgb * i.fogAndVL.xyz;
                half3 v = SafeNormalize(GetWorldSpaceViewDir(i.positionWS));
                half fr = pow(1.0h - saturate(dot(n, v)), _RimPower);
                fr = smoothstep(0.25h, 0.7h, fr) * _RimStrength * (1.0h - isFloor);
                col += fr * lerp(unity_AmbientSky.rgb * 0.5h, _RimColor.rgb, lit);
                col += _EmissionColor.rgb;
                if (_UM_VignetteDark > 0.001h)
                {
                    half dist = (half)distance(pw.xz, _UM_Focus.xz);
                    half vg = smoothstep((half)_UM_Focus.w, (half)_UM_Focus.w + _UM_VignetteRange, dist) * _UM_VignetteDark;
                    col = lerp(col, col * _UM_VignetteColor.rgb, vg);
                }
                if (_UM_Grade > 0.5h) col = Grade(col);
                col = MixFog(col, i.fogAndVL.w);
                return half4(col, 1);
            }
            ENDHLSL
        }

        // Inverted hull outline (opt-in per tier). Not rendered if pass disabled on the material.
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS:SV_POSITION; half fog:TEXCOORD0; };
            V vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i);
                float3 ws = TransformObjectToWorld(i.positionOS.xyz) + TransformObjectToWorldNormal(i.normalOS) * (_OutlineWidth * _OutlineOn);
                o.positionCS = TransformWorldToHClip(ws);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(V i) : SV_Target { return half4(MixFog(_OutlineColor.rgb, i.fog), 1); }
            ENDHLSL
        }

        // Own ShadowCaster/DepthOnly (no LitInput.hlsl include: keeps a single UnityPerMaterial layout = SRP Batcher OK)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection; float3 _LightPosition;
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vert(A i) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 ws = TransformObjectToWorld(i.positionOS.xyz);
                float3 nws = TransformObjectToWorldNormal(i.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 ld = normalize(_LightPosition - ws);
                #else
                    float3 ld = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, nws, ld));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return cs;
            }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct A { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vert(A i) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(i); return TransformObjectToHClip(i.positionOS.xyz); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
