Shader "Horror/WetCeilingURP"
{
    // Grimy ceiling: water stains, rust at the stain edges, and a wet glossy halo
    // around every sprinkler (positions come from FacilityDrips.cs).
    Properties
    {
        _MainTex        ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap        ("Normal Map (use NormalGL)", 2D) = "bump" {}
        _RoughnessMap   ("Roughness (sRGB off)", 2D) = "white" {}
        _AOMap          ("Ambient Occlusion (sRGB off)", 2D) = "white" {}
        _MetalMap       ("Metalness (sRGB off, optional)", 2D) = "black" {}
        _MetalStrength  ("Metal Strength", Range(0,1)) = 0
        _Color          ("Base Tint", Color) = (0.2, 0.21, 0.2, 1)
        _TileMeters     ("Texture size (meters per tile)", Float) = 2
        _NormalStrength ("Normal Strength", Range(0,3)) = 1.2
        _AOStrength     ("AO Strength", Range(0,1)) = 0.8

        _StainAmount    ("Water Stain Amount", Range(0,1)) = 0.4
        _StainScale     ("Stain pattern size (meters)", Float) = 7
        _RustColor      ("Rust Color", Color) = (0.30, 0.12, 0.04, 1)
        _DripHaloRadius ("Wet halo around sprinklers (meters)", Float) = 1.3
        _DrySmoothness  ("Dry Smoothness (x inverse roughness)", Range(0,1)) = 1
        _WetSmoothness  ("Wet Smoothness", Range(0,1)) = 0.9
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);      SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);      SAMPLER(sampler_BumpMap);
            TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_AOMap);        SAMPLER(sampler_AOMap);
            TEXTURE2D(_MetalMap);     SAMPLER(sampler_MetalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _RustColor;
                float _TileMeters;
                float _NormalStrength;
                float _AOStrength;
                float _MetalStrength;
                float _StainAmount;
                float _StainScale;
                float _DripHaloRadius;
                float _DrySmoothness;
                float _WetSmoothness;
            CBUFFER_END

            // Set every frame by FacilityDrips.cs
            float4 _DripData[16];   // xy = world xz, z = period, w = phase
            float _DripCount;
            float _FacilityTime;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float  fogFactor   : TEXCOORD2;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float VNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float FBM(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    v += a * VNoise(p);
                    p *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Build our own tangent frame so world-space UVs work on any slab orientation.
                float3 N = normalize(IN.normalWS);
                float3 up = abs(N.y) > 0.9 ? float3(0, 0, 1) : float3(0, 1, 0);
                float3 T = normalize(cross(N, up));
                float3 B = cross(N, T);
                float3x3 TBN = float3x3(T, B, N);

                float2 pw = float2(dot(IN.positionWS, T), dot(IN.positionWS, B));
                float2 uv = pw / _TileMeters;

                // --- Water stains with rusty edges ---
                float n1 = FBM(pw / _StainScale);
                float n2 = FBM(pw * 2.3 + 17.0);
                float stain = smoothstep(0.62 - _StainAmount * 0.25, 0.78 - _StainAmount * 0.25, n1);
                float edge = stain * (1.0 - stain) * 4.0;
                float rust = saturate(edge * smoothstep(0.35, 0.7, n2));

                // --- Wet halo around each sprinkler ---
                float halo = 0.0;
                int dn = (int)_DripCount;
                [loop] for (int i = 0; i < dn; i++)
                {
                    float dd = distance(IN.positionWS.xz, _DripData[i].xy) + (n1 - 0.5) * 0.6;
                    halo = max(halo, 1.0 - smoothstep(_DripHaloRadius * 0.3, _DripHaloRadius, dd));
                }
                float wetC = saturate(max(stain * 0.55, halo));

                // --- Normal ---
                half3 nTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv));
                nTS.xy *= _NormalStrength;
                nTS.xy += (VNoise(pw * 9.0 + _FacilityTime * 0.05) - 0.5) * 0.25 * wetC;   // slick, uneven wet surface
                nTS = normalize(nTS);
                float3 normalWS = normalize(mul(nTS, TBN));

                // --- Albedo / smoothness / AO ---
                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb * _Color.rgb;
                albedo *= lerp(1.0, 0.45, stain);
                albedo = lerp(albedo, _RustColor.rgb, saturate(rust * 0.7));
                albedo *= lerp(1.0, 0.5, halo);

                half rough = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv).r;
                half smoothness = lerp(_DrySmoothness * (1.0 - rough), _WetSmoothness, wetC);
                half ao = lerp(1.0, SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, uv).r, _AOStrength);

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo;
                // Metal gets covered by grime and rust where stains are
                half metal = SAMPLE_TEXTURE2D(_MetalMap, sampler_MetalMap, uv).r * _MetalStrength;
                s.metallic = metal * (1.0 - saturate(stain * 0.5 + rust * 0.8 + halo * 0.3));
                s.specular = 0;
                s.smoothness = smoothness;
                s.occlusion = ao;
                s.alpha = 1;
                s.normalTS = half3(0, 0, 1);

                InputData d = (InputData)0;
                d.positionWS = IN.positionWS;
                d.positionCS = IN.positionHCS;
                d.normalWS = normalWS;
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                d.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                d.fogCoord = IN.fogFactor;
                d.vertexLighting = half3(0, 0, 0);
                d.bakedGI = SampleSH(normalWS);
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);
                d.shadowMask = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(d, s);
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
