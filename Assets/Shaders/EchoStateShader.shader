Shader "Custom/EchoStateShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (0.05, 0.05, 0.05, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        _GlowColor("Glow Color", Color) = (0, 1, 1, 1)
        _GlowStrength("Glow Strength", Range(0, 5)) = 0.7

        _RimPower("Rim Power", Range(0.5, 8)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GlowColor;
                float _GlowStrength;
                float _RimPower;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(IN.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS =
                    positionInputs.positionCS;

                OUT.positionWS =
                    positionInputs.positionWS;

                OUT.normalWS =
                    normalInputs.normalWS;

                OUT.uv =
                    TRANSFORM_TEX(IN.uv, _BaseMap);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 textureColor =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        IN.uv
                    );

                half3 baseColor =
                    textureColor.rgb * _BaseColor.rgb;

                float3 normalWS =
                    normalize(IN.normalWS);

                float3 viewDirection =
                    normalize(
                        GetCameraPositionWS() -
                        IN.positionWS
                    );

                float rim =
                    1.0 -
                    saturate(
                        dot(normalWS, viewDirection)
                    );

                rim =
                    pow(rim, _RimPower);

                half3 glow =
                    _GlowColor.rgb *
                    rim *
                    _GlowStrength;

                half3 finalColor =
                    baseColor + glow;

                return half4(
                    finalColor,
                    textureColor.a * _BaseColor.a
                );
            }

            ENDHLSL
        }
    }
}