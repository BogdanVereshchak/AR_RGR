Shader "Custom/ShadowOnly"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Geometry-1" }
        Pass
        {
            Name "ShadowCatcher"
            Tags { "LightMode"="UniversalForward" }
            // Мультиплікативний блендинг: множить колір камери на наш колір
            Blend DstColor Zero
            // Не записуємо глибину — інші об'єкти рендеряться поверх
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                // Отримуємо тіньові координати для нашої точки
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                // shadowAttenuation: 1.0 = без тіні, 0.0 = повна тінь
                half shadow = mainLight.shadowAttenuation;
                // Множимо камеру на shadow: де тінь — темніє, де ні — без змін
                return half4(shadow, shadow, shadow, 1.0);
            }
            ENDHLSL
        }
    }
}