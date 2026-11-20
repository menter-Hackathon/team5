// クリア画面の3Dキャラ用（ResultView の clearPortraitShader に設定する）
// ゲーム用の SH_PlayerIndividuality は半透明のため、近接表示でちらつく。
// このシェーダーは不透明で、色が戻った姿（個性 1）をそのまま表示する。
// ResultCharacterPreview が _BaseMap / _BaseColor / _Smoothness を設定し、
// MaterialPropertyBlock で _Individuality / _RoleColor を渡す。
// 陰の付け方は SH_PlayerIndividuality と同じ（2段・グラデーションにしない）。
Shader "Team5/ClearPortrait"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.25
        _Individuality ("Individuality", Range(0, 1)) = 1
        _RoleColor ("Role Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.431, 0.416, 0.525, 1)
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Portrait"
            Tags { "LightMode" = "UniversalForward" }

            Blend Off
            ZWrite On
            Cull Off // 軽くしたモデルのすき間から背景が白く透けないよう、裏面も描く（裏面は法線を反転）

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
                half _Individuality;
                half4 _RoleColor;
                half4 _ShadowColor;
                half _ShadowStrength;
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
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                half individuality = saturate(_Individuality);
                half3 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;

                // ゲーム中と同じ：ロールの色は個性 0.5 から混ざり始め、1 で 30%
                color = lerp(color, color * _RoleColor.rgb, 0.3 * saturate((individuality - 0.5) * 2.0));

                // 彩度 = 個性（クリア時は 1 なので元の色のまま）
                half gray = dot(color, half3(0.299, 0.587, 0.114));
                color = lerp(gray.xxx, color, individuality);

                // 陰は2段
                Light mainLight = GetMainLight();
                half3 normalWS = normalize(input.normalWS) * (isFrontFace ? 1.0 : -1.0);
                half ndotl = dot(normalWS, mainLight.direction);
                half lit = step(0.0, ndotl);
                half3 shadowed = color * lerp(half3(1, 1, 1), _ShadowColor.rgb, _ShadowStrength);
                half3 litColor = color * max(mainLight.color, half3(0.6, 0.6, 0.6));
                half3 result = lerp(shadowed, litColor, lit);

                // うすいハイライト（_Smoothness が大きいほど鋭く）
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 halfDir = SafeNormalize(mainLight.direction + viewDirWS);
                half spec = pow(saturate(dot(normalWS, halfDir)), exp2(10.0 * _Smoothness + 1.0));
                result += mainLight.color * spec * _Smoothness * 0.5 * lit;

                return half4(result, 1);
            }
            ENDHLSL
        }
    }
}
