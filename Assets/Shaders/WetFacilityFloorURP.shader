Shader "Horror/WetFacilityFloorURP"
{
    Properties
    {
        _MainTex        ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap        ("Normal Map", 2D) = "bump" {}
        _RoughnessMap   ("Roughness (sRGB off)", 2D) = "white" {}
        _AOMap          ("Ambient Occlusion (sRGB off)", 2D) = "white" {}

        _Color          ("Base Tint", Color) = (0.55, 0.58, 0.53, 1)
        _TileMeters     ("Texture size (meters per tile)", Float) = 2
        _NormalStrength ("Normal Strength", Range(0,3)) = 1.2
        _AOStrength     ("AO Strength", Range(0,1)) = 0.8

        _WetnessMask    ("Wetness Mask (R = puddle areas, sRGB off)", 2D) = "white" {}
        _PuddleMeters   ("Puddle pattern size (meters)", Float) = 10
        _WetnessAmount  ("Global Wetness", Range(0,1)) = 0.5
        _WetDarken      ("Wet Darkening", Range(0,1)) = 0.5
        _Smoothness     ("Dry Smoothness", Range(0,1)) = 1
        _WetSmoothness  ("Wet Smoothness", Range(0,1)) = 0.95

        _RippleCellMeters ("Ripple spacing (meters)", Float) = 0.8
        _RippleRate       ("Ripple Speed", Float) = 1
        _RippleStrength   ("Ripple Strength", Range(0,4)) = 1.2
        _RippleFade       ("Ripple fade distance (meters)", Float) = 12

        _DripPuddleRadius   ("Puddle size under ceiling drips (meters)", Float) = 0.9
        _DripRippleStrength ("Drip ripple strength", Range(0,3)) = 1.0

        _EmissionMap    ("Emission Mask (crack lines)", 2D) = "black" {}
        _EmissionColor  ("Emergency Emission Color", Color) = (0.9, 0.05, 0.05, 1)
        _PulseSpeed     ("Emission Pulse Speed", Float) = 1.2
        _PulseMin       ("Emission Pulse Min", Range(0,1)) = 0.1

        [Header(Lamp light on the floor)]
        _LightGain      ("Direct light gain", Range(0,6)) = 1.5
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

            #pragma vertex vert
            #pragma fragment frag

            //  light shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            // Additional lights
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            // Additional light shadows
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            // Soft shadows
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            // Screen space occlusion
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            // TEXTURES
         

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);

            TEXTURE2D(_AOMap);
            SAMPLER(sampler_AOMap);

            TEXTURE2D(_WetnessMask);
            SAMPLER(sampler_WetnessMask);

            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);


            // MATERIAL VARIABLES
            

            CBUFFER_START(UnityPerMaterial)

                float4 _EmissionMap_ST;

                float4 _Color;
                float4 _EmissionColor;

                float _TileMeters;
                float _NormalStrength;
                float _AOStrength;

                float _PuddleMeters;
                float _WetnessAmount;
                float _WetDarken;

                float _Smoothness;
                float _WetSmoothness;

                float _RippleCellMeters;
                float _RippleRate;
                float _RippleStrength;
                float _RippleFade;

                float _DripPuddleRadius;
                float _DripRippleStrength;

                float _PulseSpeed;
                float _PulseMin;

                float _LightGain;

            CBUFFER_END


            // DRIP DATA
            

            // Set every frame by FacilityDrips.cs
            //
            // xy = world xz position
            // z  = drip period
            // w  = phase

            float4 _DripData[16];

            float _DripCount;

            float _FacilityTime;


            // VERTEX STRUCTURES
          

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
            };


            // VERTEX SHADER
      

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs =
                    GetVertexPositionInputs(
                        IN.positionOS.xyz
                    );

                VertexNormalInputs normInputs =
                    GetVertexNormalInputs(
                        IN.normalOS,
                        IN.tangentOS
                    );

                OUT.positionHCS =
                    posInputs.positionCS;

                OUT.positionWS =
                    posInputs.positionWS;

                OUT.normalWS =
                    normInputs.normalWS;

                OUT.tangentWS =
                    float4(
                        normInputs.tangentWS,
                        IN.tangentOS.w *
                        GetOddNegativeScale()
                    );

                OUT.uv =
                    IN.uv;

                return OUT;
            }

            //  RIPPLE FUNCTION
            

            float2 RippleGradientCheap(
                float2 p,
                float t
            )
            {
                float2 cell =
                    floor(p);

                float2 f =
                    frac(p);

                float2 g =
                    float2(0, 0);


                // CENTER
             
                {
                    float2 c =
                        float2(0, 0);

                    float2 s =
                        cell + c;

                    float2 h =
                        frac(
                            sin(
                                float2(
                                    dot(
                                        s,
                                        float2(127.1, 311.7)
                                    ),
                                    dot(
                                        s,
                                        float2(269.5, 183.3)
                                    )
                                )
                            )
                            *
                            43758.5453
                        );

                    float2 d =
                        f - (c + h);

                    float dist =
                        length(d) + 0.0001;

                    float ph =
                        frac(
                            t * 0.4 +
                            h.x * 7.0
                        );

                    float x0 =
                        dist -
                        ph * 0.9;

                    float env =
                        exp(
                            -x0 * x0 * 120.0
                        )
                        *
                        (1.0 - ph);

                    g +=
                        (d / dist)
                        *
                        cos(x0 * 60.0)
                        *
                        env;
                }


                // RIGHT
              

                {
                    float2 c =
                        float2(1, 0);

                    float2 s =
                        cell + c;

                    float2 h =
                        frac(
                            sin(
                                float2(
                                    dot(
                                        s,
                                        float2(127.1, 311.7)
                                    ),
                                    dot(
                                        s,
                                        float2(269.5, 183.3)
                                    )
                                )
                            )
                            *
                            43758.5453
                        );

                    float2 d =
                        f - (c + h);

                    float dist =
                        length(d) + 0.0001;

                    float ph =
                        frac(
                            t * 0.4 +
                            h.x * 7.0
                        );

                    float x0 =
                        dist -
                        ph * 0.9;

                    float env =
                        exp(
                            -x0 * x0 * 120.0
                        )
                        *
                        (1.0 - ph);

                    g +=
                        (d / dist)
                        *
                        cos(x0 * 60.0)
                        *
                        env;
                }

                // LEFT
              

                {
                    float2 c =
                        float2(-1, 0);

                    float2 s =
                        cell + c;

                    float2 h =
                        frac(
                            sin(
                                float2(
                                    dot(
                                        s,
                                        float2(127.1, 311.7)
                                    ),
                                    dot(
                                        s,
                                        float2(269.5, 183.3)
                                    )
                                )
                            )
                            *
                            43758.5453
                        );

                    float2 d =
                        f - (c + h);

                    float dist =
                        length(d) + 0.0001;

                    float ph =
                        frac(
                            t * 0.4 +
                            h.x * 7.0
                        );

                    float x0 =
                        dist -
                        ph * 0.9;

                    float env =
                        exp(
                            -x0 * x0 * 120.0
                        )
                        *
                        (1.0 - ph);

                    g +=
                        (d / dist)
                        *
                        cos(x0 * 60.0)
                        *
                        env;
                }

                // TOP
            
                {
                    float2 c =
                        float2(0, 1);

                    float2 s =
                        cell + c;

                    float2 h =
                        frac(
                            sin(
                                float2(
                                    dot(
                                        s,
                                        float2(127.1, 311.7)
                                    ),
                                    dot(
                                        s,
                                        float2(269.5, 183.3)
                                    )
                                )
                            )
                            *
                            43758.5453
                        );

                    float2 d =
                        f - (c + h);

                    float dist =
                        length(d) + 0.0001;

                    float ph =
                        frac(
                            t * 0.4 +
                            h.x * 7.0
                        );

                    float x0 =
                        dist -
                        ph * 0.9;

                    float env =
                        exp(
                            -x0 * x0 * 120.0
                        )
                        *
                        (1.0 - ph);

                    g +=
                        (d / dist)
                        *
                        cos(x0 * 60.0)
                        *
                        env;
                }


                // BOTTOM
               

                {
                    float2 c =
                        float2(0, -1);

                    float2 s =
                        cell + c;

                    float2 h =
                        frac(
                            sin(
                                float2(
                                    dot(
                                        s,
                                        float2(127.1, 311.7)
                                    ),
                                    dot(
                                        s,
                                        float2(269.5, 183.3)
                                    )
                                )
                            )
                            *
                            43758.5453
                        );

                    float2 d =
                        f - (c + h);

                    float dist =
                        length(d) + 0.0001;

                    float ph =
                        frac(
                            t * 0.4 +
                            h.x * 7.0
                        );

                    float x0 =
                        dist -
                        ph * 0.9;

                    float env =
                        exp(
                            -x0 * x0 * 120.0
                        )
                        *
                        (1.0 - ph);

                    g +=
                        (d / dist)
                        *
                        cos(x0 * 60.0)
                        *
                        env;
                }


                return g;
            }


            // FRAGMENT SHADER
            

            half4 frag(Varyings IN) : SV_Target
            {
               

                float3 nWS0 =
                    normalize(
                        IN.normalWS
                    );

                float3 tWS =
                    normalize(
                        IN.tangentWS.xyz
                    );

                float3 bWS =
                    cross(
                        nWS0,
                        tWS
                    )
                    *
                    IN.tangentWS.w;

                float3x3 TBN =
                    float3x3(
                        tWS,
                        bWS,
                        nWS0
                    );

                float3 viewWS =
                    GetWorldSpaceNormalizeViewDir(
                        IN.positionWS
                    );

                float2 worldUV =
                    IN.positionWS.xz;


             
                // WETNESS
               

                half mask =
                    SAMPLE_TEXTURE2D(
                        _WetnessMask,
                        sampler_WetnessMask,
                        worldUV /
                        _PuddleMeters
                    ).r;

                half wet =
                    smoothstep(
                        0.45,
                        0.55,
                        mask +
                        (_WetnessAmount - 0.5)
                    );


                // PERMANENT PUDDLES UNDER DRIPS
               

                int dripN =
                    min(
                        (int)_DripCount,
                        16
                    );

                [loop]
                for (
                    int di = 0;
                    di < dripN;
                    di++
                )
                {
                    float2 difference =
                        worldUV -
                        _DripData[di].xy;

                    float distanceSquared =
                        dot(
                            difference,
                            difference
                        );

                    float maxRadius =
                        _DripPuddleRadius;

                    if (
                        distanceSquared <
                        maxRadius *
                        maxRadius
                    )
                    {
                        float dd =
                            sqrt(
                                distanceSquared
                            )
                            +
                            (mask - 0.5) *
                            0.4;

                        float puddle =
                            1.0 -
                            smoothstep(
                                _DripPuddleRadius *
                                0.55,

                                _DripPuddleRadius,

                                dd
                            );

                        wet =
                            max(
                                wet,
                                (half)puddle
                            );
                    }
                }


                // UV
               

                float2 uv =
                    worldUV /
                    _TileMeters;


                // NORMAL MAP
                

                half3 nTS =
                    UnpackNormal(
                        SAMPLE_TEXTURE2D(
                            _BumpMap,
                            sampler_BumpMap,
                            uv
                        )
                    );

                nTS.xy *=
                    _NormalStrength;


                // RIPPLE DISTANCE FADE
                

                float camDist =
                    distance(
                        IN.positionWS,
                        _WorldSpaceCameraPos
                    );

                float rippleFade =
                    saturate(
                        1.0 -
                        camDist /
                        _RippleFade
                    );

                rippleFade *=
                    rippleFade;


                //   RIPPLES
                

                if (
                    wet > 0.02 &&
                    rippleFade > 0.01
                )
                {
                    float2 g =
                        RippleGradientCheap(
                            worldUV /
                            _RippleCellMeters,
                            _Time.y *
                            _RippleRate
                        );

                    nTS.xy +=
                        -g
                        *
                        _RippleStrength
                        *
                        rippleFade
                        *
                        wet;


                   
                    // DRIP RIPPLES
                  

                    [loop]
                    for (
                        int dj = 0;
                        dj < dripN;
                        dj++
                    )
                    {
                        float2 dv =
                            worldUV -
                            _DripData[dj].xy;

                        float dl2 =
                            dot(
                                dv,
                                dv
                            );


                        // Only calculate ripple if the pixel
                        // is within 2 metres of the drip.

                        if (
                            dl2 < 4.0
                        )
                        {
                            float period =
                                _DripData[dj].z;

                            float phase =
                                _DripData[dj].w;

                            float ra =
                                fmod(
                                    _FacilityTime +
                                    phase,
                                    period
                                )
                                -
                                (
                                    period -
                                    1.8
                                );


                            if (
                                ra > 0.0
                            )
                            {
                                float dl =
                                    sqrt(
                                        dl2
                                    )
                                    +
                                    0.0001;

                                float x0 =
                                    dl -
                                    ra * 0.5;

                                float env =
                                    exp(
                                        -x0 *
                                        x0 *
                                        60.0
                                    )
                                    *
                                    (
                                        1.0 -
                                        ra / 1.8
                                    );

                                nTS.xy -=
                                    (
                                        dv /
                                        dl
                                    )
                                    *
                                    cos(
                                        x0 *
                                        40.0
                                    )
                                    *
                                    env
                                    *
                                    _DripRippleStrength
                                    *
                                    rippleFade;
                            }
                        }
                    }
                }



                nTS =
                    normalize(
                        nTS
                    );

                float3 normalWS =
                    normalize(
                        mul(
                            nTS,
                            TBN
                        )
                    );



                half3 albedo =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv
                    ).rgb
                    *
                    _Color.rgb;


                // Darken wet areas.

                albedo *=
                    lerp(
                        1.0,
                        1.0 -
                        _WetDarken,
                        wet
                    );


             

                half rough =
                    SAMPLE_TEXTURE2D(
                        _RoughnessMap,
                        sampler_RoughnessMap,
                        uv
                    ).r;

                half smoothness =
                    lerp(
                        _Smoothness *
                        (1.0 - rough),

                        _WetSmoothness,

                        wet
                    );



                half ao =
                    lerp(
                        1.0,

                        SAMPLE_TEXTURE2D(
                            _AOMap,
                            sampler_AOMap,
                            uv
                        ).r,

                        _AOStrength
                    );


                half pulse =
                    _PulseMin
                    +
                    (1.0 -
                    _PulseMin)
                    *
                    (
                        sin(
                            _Time.y *
                            _PulseSpeed
                        )
                        *
                        0.5
                        +
                        0.5
                    );


                half3 emission =
                    SAMPLE_TEXTURE2D(
                        _EmissionMap,
                        sampler_EmissionMap,
                        TRANSFORM_TEX(
                            IN.uv,
                            _EmissionMap
                        )
                    ).rgb
                    *
                    _EmissionColor.rgb
                    *
                    pulse;


                

                SurfaceData s =
                    (SurfaceData)0;

                s.albedo =
                    albedo;

                s.metallic =
                    0;

                s.specular =
                    0;

                s.smoothness =
                    smoothness;

                s.occlusion =
                    ao;

                s.emission =
                    emission;

                s.alpha =
                    1;

                s.normalTS =
                    half3(
                        0,
                        0,
                        1
                    );


               

                InputData inputData =
                    (InputData)0;

                inputData.positionWS =
                    IN.positionWS;

                inputData.positionCS =
                    IN.positionHCS;

                inputData.normalWS =
                    normalWS;

                inputData.viewDirectionWS =
                    viewWS;

                inputData.shadowCoord =
                    TransformWorldToShadowCoord(
                        IN.positionWS
                    );

                inputData.vertexLighting =
                    half3(
                        0,
                        0,
                        0
                    );

                inputData.bakedGI =
                    SampleSH(
                        normalWS
                    );

                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(
                        IN.positionHCS
                    );

                inputData.shadowMask =
                    half4(
                        1,
                        1,
                        1,
                        1
                    );



                half4 color =
                    UniversalFragmentPBR(
                        inputData,
                        s
                    );


                color.rgb *=
                    _LightGain;



                return half4(
                    color.rgb,
                    1
                );
            }

            ENDHLSL
        }
    }

    FallBack
        "Universal Render Pipeline/Lit"
}