Shader "FishSort/WaterSurface"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex WaterVertex
            #pragma fragment WaterFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; };
            Varyings WaterVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color;
                return output;
            }
            half4 WaterFragment(Varyings input) : SV_Target { return input.color; }
            ENDHLSL
        }
    }
}
