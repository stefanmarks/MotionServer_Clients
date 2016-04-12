/// https://forums.oculus.com/community/discussion/3413/calculating-the-distortion-shader-parameters
/// Syntax reference: see http://docs.unity3d.com/Manual/SL-Shader.html

Shader "VR/LensDistortion" 
{
	Properties
	{
		_Distortion("Distortion Coefficients", Vector) = (1.0,0,0,0)
		_ChromaticAberration("_ChromaticAberration", Vector) = (0, 0, 0, 0)
		_ScaleIn("ScaleIn",   Range(0.00,1.00)) = 0.0 
		_ScaleOut("ScaleOut", Range(0.00,1.00)) = 0.0
		_Center("Center of Projection", Vector) = (.5,.5,0,0)
		_MainTex("Base (RGB)", 2D) = "" {}
	}
		
	CGINCLUDE
	#include "UnityCG.cginc"

	struct v2f 
	{
		float4 pos : SV_POSITION;
		float2 uv  : TEXCOORD0;
	};


	float4    _Center;
	float4    _Distortion;
	float4    _ChromaticAberration;
	float     _ScaleIn;
	float     _ScaleOut;
	sampler2D _MainTex;


	v2f vert(appdata_img v)
	{
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv  = v.texcoord.xy;
		return o;
	}


	float2 Distort(float2 p, float2 chromaticAberration)
	{
		p = p - _Center.xy;
		p = p *_ScaleIn;
		
		float r2    = p.x * p.x + p.y * p.y;
		float rDist =   _Distortion.x	
			          + _Distortion.y * r2 
		              + _Distortion.z * r2 * r2
					  + _Distortion.w * r2 * r2 * r2;
		rDist = rDist * (chromaticAberration.x + chromaticAberration.y * r2);
		
		p = p * rDist * _ScaleOut;
		p = p + _Center.xy;

		return p;
	}



	half4 frag(v2f i) : SV_Target
	{
		float4 color_red, color_green, color_blue;
		float4 color;

		float2 uvR = Distort(i.uv, _ChromaticAberration.xy);
		float2 uvG = Distort(i.uv,            float2(1, 0));
		float2 uvB = Distort(i.uv, _ChromaticAberration.zw);

		float r = tex2D(_MainTex, uvR).r;
		float g = tex2D(_MainTex, uvG).g;
		float b = tex2D(_MainTex, uvB).b;

		float2 pos = abs(uvG.xy - float2(0.5, 0.5)) * 2;
		float  fade = 1 - max(pos.x, pos.y);
		fade = pow(clamp(fade * 20, 0, 1), 2);
		color = float4(r, g, b, 1) * float4(fade, fade, fade, 1);

		return color;
	}
	ENDCG

	Subshader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			ENDCG
		}
	}
}
