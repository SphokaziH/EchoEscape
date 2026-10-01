Shader "Horror/WetFacilityFloorURP"
{
    // World-space UVs: texture sizes are in METERS, so plane scale no longer matters.
    // Uses URP's PBR lighting: point/spot lights, reflection probes, shadows, fog, SSAO all work.
    Properties
    {
        _MainTex        ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap        ("Normal Map (use NormalGL)", 2D) = "bump" {}
        _RoughnessMap   ("Roughness (sRGB off)", 2D) = "white" {}
        _AOMap          ("Ambient Occlusion (sRGB off)", 2D) = "white" {}
        _HeightMap      ("Height / Displacement (sRGB off)", 2D) = "gray" {}
        _Color          ("Base Tint", Color) = (0.55, 0.58, 0.53, 1)
        _TileMeters     ("Texture size (meters per tile)", Float) = 2
        _NormalStrength ("Normal Strength", Range(0,3)) = 1.2
        _AOStrength     ("AO Strength", Range(0,1)) = 0.8
        _ParallaxScale  ("Parallax Depth", Range(0,0.08)) = 0.02

        _WetnessMask    ("Wetness Mask (R = puddle areas, sRGB off)", 2D) = "white" {}
        _PuddleMeters   ("Puddle pattern size (meters)", Float) = 10
        _WetnessAmount  ("Global Wetness", Range(0,1)) = 0.5
        _WetDarken      ("Wet Darkening", Range(0,1)) = 0.5
        _Smoothness     ("Dry Smoothness (x inverse roughness)", Range(0,1)) = 1
        _WetSmoothness  ("Wet Smoothness", Range(0,1)) = 0.95

        _RippleCellMeters ("Ripple spacing (meters)", Float) = 0.6
        _RippleRate       ("Ripple Speed", Float) = 1
        _RippleStrength   ("Ripple Strength", Range(0,4)) = 1.5
        _RippleFade       ("Ripple fade distance (meters)", Float) = 25

        _EmissionMap    ("Emission Mask (crack lines)", 2D) = "black" {}
        _EmissionColor  ("Emergency Emission Color", Color) = (0.9, 0.05, 0.05, 1)
        _PulseSpeed     ("Emission Pulse Speed", Float) = 1.2
        _PulseMin       ("Emission Pulse Min", Range(0,1)) = 0.1
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
            TEXTURE2D(_HeightMap);    SAMPLER(sampler_HeightMap);
            TEXTURE2D(_WetnessMask);  SAMPLER(sampler_WetnessMask);
            TEXTURE2D(_EmissionMap);  SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _EmissionMap_ST;
                float4 _Color;
                float4 _EmissionColor;
                float _TileMeters;
                float _NormalStrength;
                float _AOStrength;
                float _ParallaxScale;
                float _PuddleMeters;
                float _WetnessAmount;
                float _WetDarken;
                float _Smoothness;
                float _WetSmoothness;
                float _RippleCellMeters;
                float _RippleRate;
                float _RippleStrength;
                float _RippleFade;
                float _PulseSpeed;
                float _PulseMin;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 tangentWS   : TEXCOORD3;
                float  fogFactor   : TEXCOORD4;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs  = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = normInputs.normalWS;
                OUT.tangentWS   = float4(normInputs.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.uv          = IN.uv;
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            // Procedural raindrop rings. Returns the height-field gradient (xy).
            float2 RippleGradient(float2 p, float t)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                float2 g = float2(0, 0);
                [unroll] for (int y = -1; y <= 1; y++)
                {
                    [unroll] for (int x = -1; x <= 1; x++)
                    {
                        float2 c = float2(x, y);
                        float2 s = cell + c;
                        float2 h = frac(sin(float2(dot(s, float2(127.1, 311.7)),
                                                   dot(s, float2(269.5, 183.3)))) * 43758.5453);
                        float2 d = f - (c + h);
                        float dist = length(d) + 0.0001;
                        float ph = frac(t * 0.4 + h.x * 7.0);
                        float x0 = dist - ph * 0.9;
                        float env = exp(-x0 * x0 * 120.0) * (1.0 - ph);
                        g += (d / dist) * cos(x0 * 60.0) * env;
                    }
                }
                return g;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 nWS0 = normalize(IN.normalWS);
                float3 tWS  = normalize(IN.tangentWS.xyz);
                float3 bWS  = cross(nWS0, tWS) * IN.tangentWS.w;
                float3x3 TBN = float3x3(tWS, bWS, nWS0);
                float3 viewWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float3 viewTS = mul(TBN, viewWS);

                float2 worldUV = IN.positionWS.xz;

                // --- Puddles (crisp edges) ---
                half mask = SAMPLE_TEXTURE2D(_WetnessMask, sampler_WetnessMask, worldUV / _PuddleMeters).r;
                half wet = smoothstep(0.45, 0.55, mask + (_WetnessAmount - 0.5));

                // --- Parallax (flattened inside puddles) ---
                float2 uv = worldUV / _TileMeters;
                half height = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uv).r;
                uv += (height - 0.5) * _ParallaxScale * (1.0 - wet) * (viewTS.xy / (viewTS.z + 0.42));

                // --- Normal: concrete detail + ripples inside puddles ---
                half3 nTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv));
                nTS.xy *= _NormalStrength;

                float camDist = distance(IN.positionWS, _WorldSpaceCameraPos);
                float fade = saturate(1.0 - camDist / _RippleFade);
                float2 g = RippleGradient(worldUV / _RippleCellMeters, _Time.y * _RippleRate);
                nTS.xy += (-g * _RippleStrength * fade * wet);
                nTS = normalize(nTS);

                float3 normalWS = normalize(mul(nTS, TBN));

                // --- Surface ---
                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb * _Color.rgb;
                albedo *= lerp(1.0, 1.0 - _WetDarken, wet);

                half rough = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv).r;
                half smoothness = lerp(_Smoothness * (1.0 - rough), _WetSmoothness, wet);

                half ao = lerp(1.0, SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, uv).r, _AOStrength);

                half pulse = _PulseMin + (1.0 - _PulseMin) * (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5);
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, TRANSFORM_TEX(IN.uv, _EmissionMap)).rgb
                                 * _EmissionColor.rgb * pulse;

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo;
                s.metallic = 0;
                s.specular = 0;
                s.smoothness = smoothness;
                s.occlusion = ao;
                s.emission = emission;
                s.alpha = 1;
                s.normalTS = half3(0, 0, 1);

                InputData d = (InputData)0;
                d.positionWS = IN.positionWS;
                d.positionCS = IN.positionHCS;
                d.normalWS = normalWS;
                d.viewDirectionWS = viewWS;
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
