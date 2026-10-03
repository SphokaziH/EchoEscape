// URP wall shader for AmbientCG Plaster001 (Color, NormalGL, Roughness, AmbientOcclusion).
// - World-space triplanar mapping: no UVs needed, scaled cubes never stretch the texture.
// - Matte: environment reflections are switched off and smoothness is capped, so no milky sheen.
// - Procedural grime in world space: floor dirt, ceiling stains, water streaks and blotches.
// - Reads the global float _FacilityPower (0 = power off, 1 = power on), set from WallPower.cs.
Shader "Echo/FacilityWall"
{
    Properties
    {
        [Header(Plaster maps)]
        _BaseMap("Color", 2D) = "white" {}
        _BaseColor("Tint", Color) = (0.5, 0.52, 0.5, 1)
        [Normal] _BumpMap("Normal (NormalGL)", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0, 2)) = 1.2
        _RoughnessMap("Roughness", 2D) = "white" {}
        _SmoothnessScale("Smoothness Scale (keep low for plaster)", Range(0, 1)) = 0.1
        _OcclusionMap("Ambient Occlusion", 2D) = "white" {}
        _OcclusionStrength("AO Strength", Range(0, 1)) = 1

        [Header(Relief without moving lights)]
        _KeyLightDir("Fake key light direction (world, points where light travels)", Vector) = (0.4, -0.7, 0.3, 0)
        _KeyLightStrength("Fake key light strength", Range(0, 1)) = 0.5
        _CavityStrength("Cavity darkening in grooves", Range(0, 1)) = 0.6

        [Header(Mapping)]
        _TileSize("Metres per texture repeat", Float) = 2
        _BlendSharpness("Triplanar Blend Sharpness", Range(1, 16)) = 6

        [Header(Grime and decay)]
        _Desaturate("Desaturate", Range(0, 1)) = 0.35
        _DirtAmount("Dirt Amount", Range(0, 1)) = 0.85
        _DirtColor("Dirt Colour (multiplier)", Color) = (0.22, 0.2, 0.17, 1)
        _FloorY("World Y of the floor", Float) = 0
        _FloorGrimeHeight("Floor grime height (m)", Float) = 1.2
        _CeilingHeight("Ceiling height above floor (m)", Float) = 3
        _CeilingGrimeHeight("Ceiling grime height (m)", Float) = 0.8

        [Header(Power)]
        [Toggle] _UseGlobalPower("Use global power (_FacilityPower)", Float) = 1
        _LocalPower("Local power (when not global)", Range(0, 1)) = 0
        _LightInfluence("Dynamic light influence (0 = walls ignore lights)", Range(0, 1)) = 0.25
        _OffAlbedo("Surface brightness, power off", Range(0, 1)) = 0.6
        _AmbientOff("Ambient light, power off", Range(0, 1)) = 0.05
        _AmbientOn("Ambient light, power on", Range(0, 2)) = 0.35
        _AmbientTint("Ambient tint", Color) = (0.8, 0.88, 0.85, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // Plaster does not mirror the sky. This must come before the URP includes.
            #define _ENVIRONMENTREFLECTIONS_OFF 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            TEXTURE2D(_RoughnessMap);
            TEXTURE2D(_OcclusionMap);

            float _FacilityPower; // global, set by WallPower.cs

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _BumpScale;
                half _SmoothnessScale;
                half _OcclusionStrength;
                float _TileSize;
                float _BlendSharpness;
                half _Desaturate;
                half _DirtAmount;
                half4 _DirtColor;
                float _FloorY;
                float _FloorGrimeHeight;
                float _CeilingHeight;
                float _CeilingGrimeHeight;
                half _UseGlobalPower;
                half _LocalPower;
                half _LightInfluence;
                float4 _KeyLightDir;
                half _KeyLightStrength;
                half _CavityStrength;
                half _OffAlbedo;
                half _AmbientOff;
                half _AmbientOn;
                half4 _AmbientTint;
            CBUFFER_END

            // ---- small value-noise helpers for world-space grime ----
            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float VNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = Hash13(i);
                float n100 = Hash13(i + float3(1, 0, 0));
                float n010 = Hash13(i + float3(0, 1, 0));
                float n110 = Hash13(i + float3(1, 1, 0));
                float n001 = Hash13(i + float3(0, 0, 1));
                float n101 = Hash13(i + float3(1, 0, 1));
                float n011 = Hash13(i + float3(0, 1, 1));
                float n111 = Hash13(i + float3(1, 1, 1));
                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            float Fbm(float3 p)
            {
                float v = 0.0, a = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    v += a * VNoise(p);
                    p *= 2.03;
                    a *= 0.5;
                }
                return v;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vp = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs vn = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vp.positionCS;
                output.positionWS = vp.positionWS;
                output.normalWS = vn.normalWS;
                output.fogFactor = ComputeFogFactor(vp.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 nWS = normalize(input.normalWS);
                float3 wp = input.positionWS;

                // ---- triplanar weights and projections ----
                float3 blend = pow(abs(nWS), _BlendSharpness);
                blend /= (blend.x + blend.y + blend.z + 1e-5);

                float2 uvX = wp.zy / _TileSize;
                float2 uvY = wp.xz / _TileSize;
                float2 uvZ = wp.xy / _TileSize;

                half3 albedo =
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX).rgb * blend.x +
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY).rgb * blend.y +
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ).rgb * blend.z;
                albedo *= _BaseColor.rgb;

                half rough =
                    SAMPLE_TEXTURE2D(_RoughnessMap, sampler_BaseMap, uvX).r * blend.x +
                    SAMPLE_TEXTURE2D(_RoughnessMap, sampler_BaseMap, uvY).r * blend.y +
                    SAMPLE_TEXTURE2D(_RoughnessMap, sampler_BaseMap, uvZ).r * blend.z;

                half ao =
                    SAMPLE_TEXTURE2D(_OcclusionMap, sampler_BaseMap, uvX).r * blend.x +
                    SAMPLE_TEXTURE2D(_OcclusionMap, sampler_BaseMap, uvY).r * blend.y +
                    SAMPLE_TEXTURE2D(_OcclusionMap, sampler_BaseMap, uvZ).r * blend.z;

                // ---- triplanar normal map (whiteout blend) ----
                half3 tnX = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, uvX));
                half3 tnY = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, uvY));
                half3 tnZ = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, uvZ));
                tnX.xy *= _BumpScale;
                tnY.xy *= _BumpScale;
                tnZ.xy *= _BumpScale;

                tnX = half3(tnX.xy + nWS.zy, abs(tnX.z) * nWS.x);
                tnY = half3(tnY.xy + nWS.xz, abs(tnY.z) * nWS.y);
                tnZ = half3(tnZ.xy + nWS.xy, abs(tnZ.z) * nWS.z);

                float3 normalWS = normalize(tnX.zyx * blend.x + tnY.xzy * blend.y + tnZ.xyz * blend.z);

                // ---- decay: desaturate, then layer grime ----
                half luma = dot(albedo, half3(0.299h, 0.587h, 0.114h));
                albedo = lerp(half3(luma, luma, luma), albedo, 1.0h - _Desaturate);

                float h = wp.y - _FloorY;
                // water streaks run vertically, so stretch the noise along Y
                float streak = Fbm(float3((wp.x + wp.z) * 1.7, wp.y * 0.18, (wp.x - wp.z) * 0.3));
                streak = smoothstep(0.45, 0.8, streak) * saturate(h / _CeilingHeight + 0.3);
                // big stains and blotches
                float blotch = smoothstep(0.4, 0.75, Fbm(wp * 0.45));
                // dirt gathers at the floor and under the ceiling
                float floorG = 1.0 - saturate(h / max(_FloorGrimeHeight, 0.01));
                floorG *= floorG;
                float ceilG = saturate((h - (_CeilingHeight - _CeilingGrimeHeight)) / max(_CeilingGrimeHeight, 0.01));
                ceilG *= ceilG;

                half dirt = saturate(streak * 0.55 + blotch * 0.45 + floorG * 0.9 + ceilG * 0.6) * _DirtAmount;
                albedo = lerp(albedo, albedo * _DirtColor.rgb, dirt);
                rough = saturate(rough + dirt * 0.2h);

                // Cavity: darken where the normal map tilts away from the flat surface, so grooves
                // and bumps read even with no light on them. Independent of camera and lights.
                half slope = saturate(dot(normalWS, nWS));
                albedo *= lerp(1.0h, pow(slope, 16.0h), _CavityStrength);

                // ---- power state: 0 = dark, 1 = bright ----
                half p = saturate(_UseGlobalPower > 0.5 ? _FacilityPower : _LocalPower);

                SurfaceData surf = (SurfaceData)0;
                surf.albedo = albedo * lerp(_OffAlbedo, 1.0h, p);
                surf.metallic = 0;
                surf.specular = 0;
                surf.smoothness = saturate((1.0h - rough) * _SmoothnessScale);
                surf.occlusion = lerp(1.0h, ao, _OcclusionStrength);
                surf.emission = 0;
                surf.alpha = 1;
                surf.normalTS = half3(0, 0, 1);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                // Stable base brightness: flat ambient that only depends on power and the wall's
                // world-space direction, never on where the camera or a moving light is.
                half amb = lerp(_AmbientOff, _AmbientOn, p);
                // A fixed fake light direction shades the bump normals the same way from every angle.
                half3 keyL = normalize(-_KeyLightDir.xyz + 1e-5);
                half key = saturate(dot(normalWS, keyL) * 0.75h + 0.25h);
                half shade = lerp(0.8h + 0.2h * normalWS.y, key * 1.4h, _KeyLightStrength);
                half3 ambient = amb * _AmbientTint.rgb * shade * surf.albedo * surf.occlusion;

                inputData.bakedGI = half3(0, 0, 0);
                half4 color = UniversalFragmentPBR(inputData, surf); // direct lights only
                color.rgb = color.rgb * _LightInfluence + ambient;   // lights only nudge the result
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        // Reuse URP Lit's utility passes so shadows, depth prepass and SSAO normals work.
        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
        UsePass "Universal Render Pipeline/Lit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Lit/DEPTHNORMALS"
    }
}
