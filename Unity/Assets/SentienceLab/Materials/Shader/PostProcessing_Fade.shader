﻿Shader "Hidden/Sentience Lab/Post Processing/Fade"
{
	Properties
	{
		_Colour( "Color",       Color)      = (1,1,1,1)
		_Fade(   "Fade Factor", Range(0,1)) = 1
		_MainTex("Texture",     2D)         = "white" { }
	}
	
	SubShader
	{
		//Rendering settings
		Cull   Off 
		ZTest  Always
		ZWrite Off
		Fog { Mode Off } 

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4    _Colour;
			float     _Fade;
			sampler2D _MainTex;

			struct v2f
			{
				float4 pos : POSITION;
				half2  uv  : TEXCOORD0;
			};

			// Vertex Shader 
			v2f vert(appdata_img v) 
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv  = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
				return o;
			}

			// Fragment Shader
			fixed4 frag(v2f i) : COLOR
			{
				fixed  fade = _Colour.a * _Fade;
				return fixed4(lerp(tex2D(_MainTex, i.uv).rgb, _Colour.rgb, fade), 1);
			}
			ENDCG

		}
	}

	Fallback "Diffuse"
}
