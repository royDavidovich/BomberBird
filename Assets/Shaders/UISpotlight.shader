// A dark wash over the whole screen with a soft round hole in it and a thin ring around the
// hole. Drawn by CageSpotlight on a UI Image stretched over the canvas, so the Image's UVs
// run 0-1 across the screen, the same space as a camera viewport point.
//
// Distances are measured in screen heights: the horizontal offset is scaled by the aspect
// ratio, so the hole stays round on any screen shape.
//
// The Image's own colour alpha multiplies everything, which is how the spotlight fades out
// without touching the hole.
Shader "BomberBird/UI/Spotlight"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_DimColor ("Dim Color", Color) = (0.02, 0.02, 0.06, 0.7)
		_Center ("Center (viewport)", Vector) = (0.5, 0.5, 0, 0)
		_Aspect ("Aspect (width / height)", Float) = 1.7777
		_Radius ("Hole Radius (screen heights)", Float) = 0.1
		_Softness ("Edge Softness (screen heights)", Float) = 0.03
		_RingColor ("Ring Color", Color) = (0.96, 0.8, 0.47, 1)
		_RingWidth ("Ring Width (screen heights)", Float) = 0.004
		_RingAlpha ("Ring Alpha", Range(0, 1)) = 0
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
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			fixed4 _DimColor;
			float4 _Center;
			float _Aspect;
			float _Radius;
			float _Softness;
			fixed4 _RingColor;
			float _RingWidth;
			float _RingAlpha;

			v2f vert(appdata i_In)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(i_In.vertex);
				o.color = i_In.color;
				o.uv = i_In.uv;
				return o;
			}

			fixed4 frag(v2f i_In) : SV_Target
			{
				float2 offset = i_In.uv - _Center.xy;
				offset.x *= _Aspect;
				float distance = length(offset);

				// Clear inside the hole, rising to the full wash across the soft edge.
				float dim = _DimColor.a * smoothstep(_Radius, _Radius + _Softness, distance);

				// A band on the hole's edge, fading out on both sides of it.
				float ring = _RingAlpha * (1.0 - smoothstep(0.0, _RingWidth, abs(distance - _Radius)));

				fixed4 colour;
				colour.rgb = lerp(_DimColor.rgb, _RingColor.rgb, saturate(ring / max(dim + ring, 0.0001)));
				colour.a = saturate(dim + ring) * i_In.color.a;

				return colour;
			}
			ENDCG
		}
	}
}
