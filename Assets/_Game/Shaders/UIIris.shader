// Full-screen cover with a soft round hole in the middle, for the screen transition. _Radius is the size of the hole as a
// fraction of the screen height: big = nothing covered, 0 = everything covered. _Feather is the width of the soft edge.
// The cover is the loading-screen background: a flat colour with the faint fish / drop pattern and a soft glow in the middle.
Shader "FishSort/UIIris"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Cover Color", Color) = (0.18, 0.21, 0.9, 1)
        _Radius ("Radius", Float) = 2
        _Feather ("Feather", Float) = 0.18
        _PatternTex ("Pattern (white, mostly transparent)", 2D) = "black" {}
        _PatternAspect ("Pattern Width / Height", Float) = 1.056
        _PatternStrength ("Pattern Strength", Float) = 3
        _GlowStrength ("Centre Glow", Float) = 0.14
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Radius;
            float _Feather;
            sampler2D _PatternTex;
            float _PatternAspect;
            float _PatternStrength;
            float _GlowStrength;

            struct VertexInput
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct VertexOutput
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            VertexOutput vert(VertexInput v)
            {
                VertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(VertexOutput i) : SV_Target
            {
                float2 screenUv = i.pos.xy / _ScreenParams.xy;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float distanceFromCenter = length((screenUv - 0.5) * float2(aspect, 1.0));
                float cover = saturate((distanceFromCenter - (_Radius - _Feather)) / max(_Feather, 0.0001));
                fixed4 color = _Color * i.color;
                // One pattern tile is as wide as the screen; the glow fades out towards the edges.
                float2 patternUv = float2(screenUv.x, screenUv.y * _PatternAspect / aspect);
                fixed pattern = tex2D(_PatternTex, patternUv).a * _PatternStrength;
                color.rgb = lerp(color.rgb, fixed3(1, 1, 1), saturate(pattern));
                color.rgb += _GlowStrength * (1.0 - saturate(distanceFromCenter / 0.6));
                color.a *= smoothstep(0.0, 1.0, cover);
                return color;
            }
            ENDCG
        }
    }
}
