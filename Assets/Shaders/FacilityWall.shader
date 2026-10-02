
Shader "Echo/FacilityWall"
{
    Properties
    {
        [Header(Plaster maps)]
        _BaseMap("Color", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        [Normal] _BumpMap("Normal (NormalGL)", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0, 2)) = 1
        _RoughnessMap("Roughness", 2D) = "white" {}
        _SmoothnessScale("Smoothness Scale", Range(0, 1)) = 1
        _OcclusionMap("Ambient Occlusion", 2D) = "white" {}
        _OcclusionStrength("AO Strength", Range(0, 1)) = 1

        [Header(Mapping)]
        _TileSize("Metres per texture repeat", Float) = 2
        _BlendSharpness("Triplanar Blend Sharpness", Range(1, 16)) = 6

        [Header(Power)]
        [Toggle] _UseGlobalPower("Use global power (_FacilityPower)", Float) = 1
        _LocalPower("Local power (when not global)", Range(0, 1)) = 0
        _OffAlbedo("Surface brightness, power off", Range(0, 1)) = 0.6
        _AmbientOff("Ambient light, power off", Range(0, 1)) = 0.01
        _AmbientOn("Ambient light, power on", Range(0, 2)) = 0.3
        _AmbientTint("Ambient tint", Color) = (0.85, 0.9, 1, 1)
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
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

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
                half _UseGlobalPower;
                half _LocalPower;
                half _OffAlbedo;
                half _AmbientOff;
                half _AmbientOn;
                half4 _AmbientTint;
            CBUFFER_END

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

                // ---- triplanar weights and projections ----
                float3 blend = pow(abs(nWS), _BlendSharpness);
                blend /= (blend.x + blend.y + blend.z + 1e-5);

                float2 uvX = input.positionWS.zy / _TileSize;
                float2 uvY = input.positionWS.xz / _TileSize;
                float2 uvZ = input.positionWS.xy / _TileSize;

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

                // Flat, slightly directional ambient that fades with power.
                half amb = lerp(_AmbientOff, _AmbientOn, p);
                inputData.bakedGI = amb * _AmbientTint.rgb * (0.8h + 0.2h * normalWS.y);

                half4 color = UniversalFragmentPBR(inputData, surf);
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
