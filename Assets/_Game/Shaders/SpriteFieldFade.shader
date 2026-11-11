// Unlit sprite that fades out towards the top of the bubble field (soft edge under the HUD).
// _FieldTopY is a global set by BubbleField; _FadeHeight is the height of the fade band below it.
Shader "FishSort/SpriteFieldFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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
            #pragma vertex FadeVert
            #pragma fragment FadeFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"
            #include "UnitySprites.cginc"

            float _FieldTopY;
            float _FadeHeight;

            struct FadeV2F
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float worldY : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            FadeV2F FadeVert(appdata_t IN)
            {
                FadeV2F OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_OUTPUT(FadeV2F, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float4 localPos = UnityFlipSprite(IN.vertex, _Flip);
                OUT.worldY = mul(unity_ObjectToWorld, localPos).y;
                OUT.vertex = UnityObjectToClipPos(localPos);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 FadeFrag(FadeV2F IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                float fade = saturate((_FieldTopY - IN.worldY) / max(_FadeHeight, 0.0001));
                c.a *= smoothstep(0.0, 1.0, fade);
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
