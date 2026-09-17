Shader "Horror/WetFacilityFloorURP"
{
    // URP version - hand-written HLSL forward pass (no Shader Graph).
    // Demonstrates: manual lighting model (diffuse + Blinn-Phong specular + ambient SH),
    // wetness-driven surface properties, animated tangent-space ripple normals,
    // pulsing emissive cracks.

    Properties
    {
        _MainTex        ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap        ("Normal Map", 2D) = "bump" {}
        _WetnessMask    ("Wetness Mask (R = puddle areas)", 2D) = "white" {}
        _WetnessTiling  ("Wetness Tiling (higher = smaller, more scattered puddles)", Float) = 3.0
        _Color          ("Base Tint", Color) = (1,1,1,1)

        _Smoothness     ("Dry Smoothness", Range(0,1)) = 0.15
        _WetSmoothness  ("Wet Smoothness", Range(0,1)) = 0.92
        _WetnessAmount  ("Global Wetness", Range(0,1)) = 0.5

        _RippleNormal   ("Ripple Normal Map", 2D) = "bump" {}
        _RippleTiling   ("Ripple Tiling (higher = smaller patches)", Float) = 4.0
        _RippleSpeed    ("Ripple Pan Speed (xy)", Vector) = (0.05, 0.03, 0, 0)
        _RippleStrength ("Ripple Strength", Range(0,1)) = 0.35

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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);      SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);      SAMPLER(sampler_BumpMap);
            TEXTURE2D(_WetnessMask);  SAMPLER(sampler_WetnessMask);
            TEXTURE2D(_RippleNormal); SAMPLER(sampler_RippleNormal);
            TEXTURE2D(_EmissionMap);  SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                half _Smoothness;
                half _WetSmoothness;
                half _WetnessAmount;
                half _WetnessTiling;
                half _RippleTiling;
                float4 _RippleSpeed;
                half _RippleStrength;
                float4 _EmissionColor;
                half _PulseSpeed;
                half _PulseMin;
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
                float4 tangentWS   : TEXCOORD3; // xyz tangent, w = sign for bitangent
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
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // --- Wetness mask drives albedo, smoothness and normal blend ---
                // _WetnessTiling repeats the mask across the floor so puddles read as
                // scattered patches rather than one wet area covering everything.
                half wetMask = SAMPLE_TEXTURE2D(_WetnessMask, sampler_WetnessMask, IN.uv * _WetnessTiling).r * _WetnessAmount;

                // --- Two panning ripple-normal layers for moving puddles ---
                // _RippleTiling scales the ripple UVs independently of the albedo/mesh UVs,
                // so raising it shrinks each ripple patch without affecting other textures.
                float2 rippleBaseUV = IN.uv * _RippleTiling;
                float2 rippleUV1 = rippleBaseUV + _RippleSpeed.xy * _Time.y;
                float2 rippleUV2 = rippleBaseUV * 1.7 - _RippleSpeed.xy * _Time.y * 0.6;
                half3 ripple1 = UnpackNormal(SAMPLE_TEXTURE2D(_RippleNormal, sampler_RippleNormal, rippleUV1));
                half3 ripple2 = UnpackNormal(SAMPLE_TEXTURE2D(_RippleNormal, sampler_RippleNormal, rippleUV2));
                half3 rippleNormalTS = normalize(ripple1 + ripple2);

                half3 baseNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv));
                half3 blendedNormalTS = normalize(lerp(baseNormalTS, rippleNormalTS, wetMask * _RippleStrength));

                // Tangent space -> world space
                float3 bitangentWS = cross(IN.normalWS, IN.tangentWS.xyz) * IN.tangentWS.w;
                float3x3 TBN = float3x3(IN.tangentWS.xyz, bitangentWS, IN.normalWS);
                float3 normalWS = normalize(mul(blendedNormalTS, TBN));

                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;
                albedo.rgb *= lerp(1.0, 0.55, wetMask); // wet concrete reads darker

                half smoothness = lerp(_Smoothness, _WetSmoothness, wetMask);

                // --- Manual lighting model: diffuse + Blinn-Phong specular + SH ambient ---
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = albedo.rgb * mainLight.color * NdotL * mainLight.shadowAttenuation;

                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                half specPower = lerp(8.0, 128.0, smoothness);
                half spec = pow(saturate(dot(normalWS, halfDir)), specPower) * smoothness;
                half3 specular = mainLight.color * spec;

                half3 ambient = SampleSH(normalWS) * albedo.rgb;

                // --- Pulsing emissive cracks ---
                half pulse = _PulseMin + (1.0 - _PulseMin) * (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5);
                half3 crackEmission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb * _EmissionColor.rgb * pulse;

                half3 color = diffuse + specular + ambient + crackEmission;
                return half4(color, albedo.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
