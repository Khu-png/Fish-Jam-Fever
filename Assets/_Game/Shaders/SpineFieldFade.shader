// Spine skeleton shader (same inputs as Spine/Skeleton, so SkeletonRenderer's sprite-mask support works) that can fade
// out towards the top of the bubble field, like SpriteFieldFade does for sprites. The fade is switched per renderer by
// the _FadeOn property: 1 inside a bubble, 0 anywhere else (tanks, tray, jar).
Shader "FishSort/SpineFieldFade"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Main Texture", 2D) = "black" {}
        [Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput ("Straight Alpha Texture", Int) = 0
        [HideInInspector] _StencilRef ("Stencil Reference", Float) = 1.0
        [HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
        _FadeHeight ("Fade Height", Float) = 0.5
        _FadeOn ("Fade On", Float) = 0
        _Tint ("Tint", Color) = (1,1,1,1)
        _Silhouette ("Outline silhouette", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }

        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Lighting Off

        Stencil
        {
            Ref [_StencilRef]
            Comp [_StencilComp]
            Pass Keep
        }

        Pass
        {
            Name "Normal"

            CGPROGRAM
            #pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _FieldTopY;
            float _FadeHeight;
            float _FadeOn;
            float4 _Tint;
            float _Silhouette;

            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 vertexColor : COLOR;
            };

            struct VertexOutput
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 vertexColor : COLOR;
                float worldY : TEXCOORD1;
            };

            // Vertex colours come premultiplied and in gamma space; bring them to the project's colour space.
            half4 PremultipliedToTargetSpace(half4 gammaPma)
            {
            #if UNITY_COLORSPACE_GAMMA
                return gammaPma;
            #else
                return gammaPma.a == 0 ? half4(GammaToLinearSpace(gammaPma.rgb), gammaPma.a)
                    : half4(GammaToLinearSpace(gammaPma.rgb / gammaPma.a) * gammaPma.a, gammaPma.a);
            #endif
            }

            VertexOutput vert(VertexInput v)
            {
                VertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.vertexColor = PremultipliedToTargetSpace(v.vertexColor);
                o.worldY = mul(unity_ObjectToWorld, v.vertex).y;
                return o;
            }

            float4 frag(VertexOutput i) : SV_Target
            {
                float4 texColor = tex2D(_MainTex, i.uv);
            #if defined(_STRAIGHT_ALPHA_INPUT)
                texColor.rgb *= texColor.a;
            #endif
                float fade = saturate((_FieldTopY - i.worldY) / max(_FadeHeight, 0.0001));
                fade = lerp(1.0, smoothstep(0.0, 1.0, fade), _FadeOn);
                float4 c = texColor * i.vertexColor;
                c.rgb = lerp(c.rgb * _Tint.rgb * _Tint.a, _Tint.rgb * c.a * _Tint.a, _Silhouette);
                c.a *= _Tint.a;
                return c * fade;
            }
            ENDCG
        }
    }
}
