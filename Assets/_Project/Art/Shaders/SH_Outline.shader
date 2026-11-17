// 背面法の輪郭。裏返しの面を法線方向に太らせて描く
// 太さは _Individuality × _OutlineWidth（個性 0 で 0、0.5 で 0.01、1 で 0.02）
Shader "Team5/Outline"
{
    Properties
    {
        _Individuality ("Individuality", Range(0, 1)) = 0.5
        _OutlineWidth ("Outline Width (at Individuality 1)", Float) = 0.02
        _OutlineColor ("Outline Color", Color) = (0.231, 0.208, 0.188, 1)
    }

    SubShader
    {
        // 体より先に描く
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-1" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "IndividualityCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                half _Individuality;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                positionWS += normalWS * (_OutlineWidth * saturate(_Individuality));
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half individuality = saturate(_Individuality);
                // 太さ 0 でも裏面が半透明の体ごしに見えないよう、低い個性では消す
                half alpha = IndividualityOpacity(individuality) * smoothstep(0.0, 0.1, individuality);
                clip(alpha - 0.001);
                return half4(_OutlineColor.rgb, alpha * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}
