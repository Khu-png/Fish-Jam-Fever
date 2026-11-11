// Thin outline around a sprite: the quad is enlarged by _Pad (room for the line) and every pixel that has
// sprite alpha within _OutlineWidth (in texture UV) is painted in the tint colour. Drawn behind the body.
// _FadeEnabled = 1 also fades it out towards the top of the bubble field.
Shader "FishSort/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width (texture UV)", Range(0.002, 0.05)) = 0.012
        _Pad ("Quad Padding Scale", Range(1, 1.5)) = 1.12
        _FadeEnabled ("Fade At Field Top (0/1)", Float) = 0
        _FadeHeight ("Fade Height", Float) = 0.5
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"
            #include "UnitySprites.cginc"

            float _FieldTopY;
            float _OutlineWidth;
            float _Pad;
            float _FadeEnabled;
            float _FadeHeight;

            struct OutlineV2F
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float worldY : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            OutlineV2F OutlineVert(appdata_t IN)
            {
                OutlineV2F OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_OUTPUT(OutlineV2F, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float4 localPos = UnityFlipSprite(IN.vertex, _Flip);
                localPos.xy *= _Pad;
                OUT.worldY = mul(unity_ObjectToWorld, localPos).y;
                OUT.vertex = UnityObjectToClipPos(localPos);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 OutlineFrag(OutlineV2F IN) : SV_Target
            {
                // Content keeps its original size; the extra padding around it holds the outline.
                float2 uv = (IN.texcoord - 0.5) * _Pad + 0.5;
                fixed shape = 0;
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * 0.3926991;
                    float2 sampleUv = uv + float2(cos(angle), sin(angle)) * _OutlineWidth;
                    fixed inside = step(0.0, sampleUv.x) * step(sampleUv.x, 1.0) * step(0.0, sampleUv.y) * step(sampleUv.y, 1.0);
                    shape = max(shape, SampleSpriteTexture(sampleUv).a * inside);
                }
                float fade = saturate((_FieldTopY - IN.worldY) / max(_FadeHeight, 0.0001));
                float fadeFactor = lerp(1.0, smoothstep(0.0, 1.0, fade), _FadeEnabled);
                fixed alpha = shape * IN.color.a * fadeFactor;
                return fixed4(IN.color.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}
