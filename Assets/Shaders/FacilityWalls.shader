Shader "Horror/FacilityWalls"
{
    Properties
    {
        [MainTexture] _MainTex ("Wall Albedo", 2D) = "white" {}
        _BumpMap ("Wall Normal", 2D) = "bump" {}
        _RoughnessMap ("Roughness", 2D) = "white" {}
        _AOMap ("Ambient Occlusion", 2D) = "white" {}

        [MainColor] _Color ("Wall Tint", Color) = (0.72, 0.74, 0.70, 1)
        _TextureTiling ("Texture Tiling", Range(0.1, 10)) = 1.0
        _NormalStrength ("Normal Strength", Range(0,2)) = 0.75
        _AOStrength ("AO Strength", Range(0,1)) = 0.65

        [Header(Power)]
        _PowerOff ("Power Off Brightness", Range(0,1)) = 0.10
        _PowerOn ("Power On Brightness", Range(0,2)) = 1.0
        _PowerLightInfluence ("Power Light Influence", Range(0,2)) = 1.0
        _PowerTint ("Power Tint", Color) = (0.82, 0.86, 0.80, 1)

        [Header(Direct Light)]
        _MainLightGain ("Main Light Gain", Range(0,2)) = 1.0
        _AdditionalLightGain ("Additional Light Gain", Range(0,2)) = 0.85
        _SpecularStrength ("Specular Strength", Range(0,1)) = 0.22
        _SpecularPower ("Specular Sharpness", Range(8,128)) = 48

       [Header(Horror Emergency)]
       _EmergencyTint ("Emergency Tint", Color) = (0.30, 0.015, 0.01, 1)
       _EmergencyStrength ("Emergency Tint Strength", Range(0,1)) = 0.10
       _GrimeDarken ("Extra Grime Darkness", Range(0,1)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }

        LOD 200

        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode"="UniversalForward"
            }

            HLSLPROGRAM

            #pragma target 3.5

            #pragma vertex vert
            #pragma fragment frag

            // ============================================================
            // MAIN LIGHT
            // ============================================================

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            // ============================================================
            // ADDITIONAL LIGHTS
            //
            // This is important for your FacilityCeilingBuilder.
            // Your generated red/white lights use LightType.Point.
            // ============================================================

            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma multi_compile_fragment _ _SHADOWS_SOFT


            // ============================================================
            // URP
            // ============================================================

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            // ============================================================
            // TEXTURES
            // ============================================================

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);

            TEXTURE2D(_AOMap);
            SAMPLER(sampler_AOMap);


            // ============================================================
            // MATERIAL VARIABLES
            // ============================================================

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                float4 _Color;

                float4 _PowerTint;

                float4 _EmergencyTint;


                float _TextureTiling;

                float _NormalStrength;

                float _AOStrength;


                float _PowerOff;

                float _PowerOn;

                float _PowerLightInfluence;


                float _MainLightGain;

                float _AdditionalLightGain;

                float _SpecularStrength;

                float _SpecularPower;


                float _EmergencyStrength;

                float _GrimeDarken;

            CBUFFER_END


            // ============================================================
            // GLOBAL POWER VALUE
            //
            // WallPower.cs controls this.
            //
            // 0 = power off
            // 1 = fully restored
            // ============================================================

            float _WallPower;


            // ============================================================
            // VERTEX STRUCTURES
            // ============================================================

            struct Attributes
            {
                float4 positionOS : POSITION;

                float3 normalOS : NORMAL;

                float4 tangentOS : TANGENT;

                float2 uv : TEXCOORD0;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;

                float2 uv : TEXCOORD0;

                float3 positionWS : TEXCOORD1;

                float3 normalWS : TEXCOORD2;

                float4 tangentWS : TEXCOORD3;
            };


            // ============================================================
            // VERTEX SHADER
            // ============================================================

            Varyings vert(Attributes IN)
            {
                Varyings OUT;


                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        IN.positionOS.xyz
                    );


                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(
                        IN.normalOS,
                        IN.tangentOS
                    );


                OUT.positionHCS =
                    positionInputs.positionCS;


                // Keep the original mesh UVs.
                //
                // This is important for vertical walls.
                // We do NOT use world XZ projection here.

                OUT.uv =
                    IN.uv;


                OUT.positionWS =
                    positionInputs.positionWS;


                OUT.normalWS =
                    normalInputs.normalWS;


                OUT.tangentWS =
                    float4(
                        normalInputs.tangentWS,
                        IN.tangentOS.w *
                        GetOddNegativeScale()
                    );


                return OUT;
            }


            // ============================================================
            // MAIN LIGHT
            // ============================================================

            half3 ApplyMainLight(
                half3 albedo,
                half3 normalWS,
                half3 viewDirWS,
                float3 positionWS,
                half smoothness,
                half ao
            )
            {
                half3 result = 0;


                Light mainLight =
                    GetMainLight(
                        TransformWorldToShadowCoord(
                            positionWS
                        )
                    );


                half NdotL =
                    saturate(
                        dot(
                            normalWS,
                            mainLight.direction
                        )
                    );


                half3 diffuse =
                    albedo *
                    mainLight.color *
                    NdotL *
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation *
                    _MainLightGain;


                // Cheap Blinn-Phong style highlight.

                half3 halfDir =
                    SafeNormalize(
                        mainLight.direction +
                        viewDirWS
                    );


                half NdotH =
                    saturate(
                        dot(
                            normalWS,
                            halfDir
                        )
                    );


                half specular =
                    pow(
                        NdotH,
                        _SpecularPower
                    ) *
                    _SpecularStrength *
                    smoothness;


                result +=
                    diffuse;


                result +=
                    specular *
                    mainLight.color;


                return result * ao;
            }


            // ============================================================
            // ADDITIONAL LIGHTS
            //
            // This handles the lights created by your
            // FacilityCeilingBuilder.
            //
            // IMPORTANT:
            // Unity has already culled the lights affecting the object.
            // We are NOT manually checking hundreds of lights.
            // ============================================================

            half3 ApplyAdditionalLights(
                half3 albedo,
                half3 normalWS,
                half3 viewDirWS,
                float3 positionWS,
                half smoothness,
                half ao
            )
            {
                half3 result = 0;


                #if defined(_ADDITIONAL_LIGHTS)

                    uint lightCount =
                        GetAdditionalLightsCount();


                    for (
                        uint i = 0u;
                        i < lightCount;
                        ++i
                    )
                    {
                        Light light =
                            GetAdditionalLight(
                                i,
                                positionWS
                            );


                        half NdotL =
                            saturate(
                                dot(
                                    normalWS,
                                    light.direction
                                )
                            );


                        half3 diffuse =
                            albedo *
                            light.color *
                            NdotL *
                            light.distanceAttenuation *
                            light.shadowAttenuation *
                            _AdditionalLightGain;


                        half3 halfDir =
                            SafeNormalize(
                                light.direction +
                                viewDirWS
                            );


                        half NdotH =
                            saturate(
                                dot(
                                    normalWS,
                                    halfDir
                                )
                            );


                        half specular =
                            pow(
                                NdotH,
                                _SpecularPower
                            ) *
                            _SpecularStrength *
                            smoothness;


                        result +=
                            diffuse;


                        result +=
                            specular *
                            light.color;
                    }

                #endif


                return result * ao;
            }


            // ============================================================
            // FRAGMENT SHADER
            // ============================================================

            half4 frag(Varyings IN) : SV_Target
            {
                // ========================================================
                // NORMAL BASIS
                // ========================================================

                half3 baseNormalWS =
                    normalize(
                        IN.normalWS
                    );


                half3 tangentWS =
                    normalize(
                        IN.tangentWS.xyz
                    );


                half3 bitangentWS =
                    normalize(
                        cross(
                            baseNormalWS,
                            tangentWS
                        )
                        *
                        IN.tangentWS.w
                    );


                half3x3 TBN =
                    half3x3(
                        tangentWS,
                        bitangentWS,
                        baseNormalWS
                    );


                // ========================================================
                // UV
                // ========================================================

                float2 uv =
                    IN.uv *
                    _TextureTiling;


                // ========================================================
                // ALBEDO
                // ========================================================

                half3 albedo =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv
                    ).rgb;


                albedo *=
                    _Color.rgb;


                // ========================================================
                // NORMAL MAP
                // ========================================================

                half3 normalTS =
                    UnpackNormal(
                        SAMPLE_TEXTURE2D(
                            _BumpMap,
                            sampler_BumpMap,
                            uv
                        )
                    );


                normalTS.xy *=
                    _NormalStrength;


                half3 normalWS =
                    normalize(
                        mul(
                            normalTS,
                            TBN
                        )
                    );


                // ========================================================
                // ROUGHNESS
                // ========================================================

                half roughness =
                    SAMPLE_TEXTURE2D(
                        _RoughnessMap,
                        sampler_RoughnessMap,
                        uv
                    ).r;


                half smoothness =
                    saturate(
                        1.0h -
                        roughness
                    );


                // ========================================================
                // AO
                // ========================================================

                half aoTexture =
                    SAMPLE_TEXTURE2D(
                        _AOMap,
                        sampler_AOMap,
                        uv
                    ).r;


                half ao =
                    lerp(
                        1.0h,
                        aoTexture,
                        _AOStrength
                    );


                // ========================================================
                // POWER
                // ========================================================

                half power =
                    saturate(
                        _WallPower
                    );


                // Smoothstep-style power response.
                //
                // This prevents the wall from simply popping
                // from dark to bright.

                half powerCurve =
                    power *
                    power *
                    (
                        3.0h -
                        2.0h *
                        power
                    );


                // ========================================================
                // WALL BRIGHTNESS
                // ========================================================

                half brightness =
                    lerp(
                        _PowerOff,
                        _PowerOn,
                        powerCurve
                    );


                // ========================================================
                // POWER TINT
                //
                // OFF:
                //      subtle emergency atmosphere
                //
                // ON:
                //      cold/neutral facility lighting
                // ========================================================

                half3 powerTint =
                    lerp(
                        _EmergencyTint.rgb,
                        _PowerTint.rgb,
                        powerCurve
                    );


                // ========================================================
                // VIEW DIRECTION
                // ========================================================

                half3 viewDirWS =
                    GetWorldSpaceNormalizeViewDir(
                        IN.positionWS
                    );


                // ========================================================
                // LIGHTING
                // ========================================================

                half3 lighting =
                    0;


                // Main directional light.

                lighting +=
                    ApplyMainLight(
                        albedo,
                        normalWS,
                        viewDirWS,
                        IN.positionWS,
                        smoothness,
                        ao
                    );


                // Ceiling point lights / spot lights.

                lighting +=
                    ApplyAdditionalLights(
                        albedo,
                        normalWS,
                        viewDirWS,
                        IN.positionWS,
                        smoothness,
                        ao
                    );


                // ========================================================
                // AMBIENT / BAKED GI
                // ========================================================

                half3 ambient =
                    SampleSH(
                        normalWS
                    ) *
                    albedo *
                    ao;


                // ========================================================
                // FINAL WALL COLOUR
                // ========================================================

                half3 color =
                    (
                        lighting +
                        ambient
                    )
                    *
                    brightness;


                // ========================================================
                // POWER TINT
                // ========================================================

                color *=
                    powerTint;


                // ========================================================
                // EMERGENCY RED
                //
                // Very subtle because your actual ceiling lights
                // already provide the strong red illumination.
                // ========================================================

                half emergency =
                    (
                        1.0h -
                        powerCurve
                    )
                    *
                    _EmergencyStrength;


                color +=
                    albedo *
                    _EmergencyTint.rgb *
                    emergency;


                // ========================================================
                // EXTRA DARKNESS WHILE UNPOWERED
                // ========================================================

                color *=
                    1.0h -
                    (
                        _GrimeDarken *
                        (
                            1.0h -
                            powerCurve
                        )
                    );


                // ========================================================
                // OUTPUT
                // ========================================================

                return half4(
                    color,
                    1
                );
            }

            ENDHLSL
        }
    }


    // ================================================================
    // FALLBACK
    // ================================================================

    FallBack
        "Universal Render Pipeline/Lit"
}