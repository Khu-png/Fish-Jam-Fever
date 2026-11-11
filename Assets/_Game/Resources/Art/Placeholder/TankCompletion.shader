Shader "FishSort/TankCompletion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Tank Sprite", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex FlashVertex
            #pragma fragment FlashFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };
            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END
            Varyings FlashVertex(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }
            half4 FlashFragment(Varyings input) : SV_Target
            {
                half4 tank = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // Keep the tank silhouette and a hint of its glass/rim detail as it turns white.
                half detail = dot(tank.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                half3 pearl = lerp(half3(0.84h, 0.94h, 1.0h), half3(1.0h, 1.0h, 1.0h), detail);
                return half4(pearl, tank.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
