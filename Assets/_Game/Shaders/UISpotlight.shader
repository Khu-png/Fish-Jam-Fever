// Full-screen dark layer with up to four rounded-rectangle windows that stay clear. Used to light up the tanks the fishing rod
// can fish for. The windows are given in screen UV (0..1 of the layer, which
// covers the whole screen): xy = centre, zw = half size, both as a fraction of the screen.
Shader "FishSort/UISpotlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Dim Colour", Color) = (0, 0.03, 0.12, 0.75)
        _Hole0 ("Window 0", Vector) = (0, 0, 0, 0)
        _Hole1 ("Window 1", Vector) = (0, 0, 0, 0)
        _Hole2 ("Window 2", Vector) = (0, 0, 0, 0)
        _Hole3 ("Window 3", Vector) = (0, 0, 0, 0)
        _HoleCount ("Window Count", Float) = 0
        _Feather ("Soft Edge", Float) = 0.015
        _Corner ("Corner Radius", Float) = 0.015
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
            float4 _Hole0;
            float4 _Hole1;
            float4 _Hole2;
            float4 _Hole3;
            float _HoleCount;
            float _Feather;
            float _Corner;

            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct VertexOutput
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            VertexOutput vert(VertexInput v)
            {
                VertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // Distance from the point to a rounded rectangle (negative inside), in height units.
            float WindowDistance(float2 p, float4 hole, float aspect)
            {
                float2 centre = float2(hole.x * aspect, hole.y);
                float2 half = float2(hole.z * aspect, hole.w) - _Corner;
                float2 q = abs(p - centre) - half;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _Corner;
            }

            fixed4 frag(VertexOutput i) : SV_Target
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = float2(i.uv.x * aspect, i.uv.y);
                float nearest = 1000.0;
                float4 holes[4] = { _Hole0, _Hole1, _Hole2, _Hole3 };
                for (int k = 0; k < 4; k++)
                {
                    if (k < (int)_HoleCount)
                    {
                        nearest = min(nearest, WindowDistance(p, holes[k], aspect));
                    }
                }

                float dimAmount = smoothstep(0.0, _Feather, nearest);
                fixed4 result = _Color;
                result.a = _Color.a * dimAmount;
                result *= i.color;
                return result;
            }
            ENDCG
        }
    }
}
