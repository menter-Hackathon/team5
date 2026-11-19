// プレイヤーの体。_Individuality（0〜1）で彩度・不透明度・ロールの色の混ざり方が変わる
// 0: 灰色・不透明度 0.25 / 0.5: 彩度 0.5・不透明度 0.7 / 1: 彩度 1・不透明・ロールの色を 30% 混ぜる
Shader "Team5/PlayerIndividuality"
{
    Properties
    {
        _Individuality ("Individuality", Range(0, 1)) = 0.5
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _RoleColor ("Role Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.431, 0.416, 0.525, 1)
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Body"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On // 半透明でも自分の体の奥が透けて見えないように
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "IndividualityCommon.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _RoleColor;
                half4 _ShadowColor;
                half _ShadowStrength;
                half _Individuality;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half individuality = saturate(_Individuality);
                half3 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;

                // ロールの色は 0.5 から混ざり始め、1 で 30%
                color = lerp(color, color * _RoleColor.rgb, 0.3 * saturate((individuality - 0.5) * 2.0));

                // 彩度 = 個性
                half gray = dot(color, half3(0.299, 0.587, 0.114));
                color = lerp(gray.xxx, color, individuality);

                // 陰は2段（アートディレクションどおり、グラデーションにしない）
                Light mainLight = GetMainLight();
                half ndotl = dot(normalize(input.normalWS), mainLight.direction);
                half lit = step(0.0, ndotl);
                color = lerp(color * lerp(1.0, _ShadowColor.rgb, _ShadowStrength), color, lit);

                return half4(color, IndividualityOpacity(individuality));
            }
            ENDHLSL
        }
    }
}
