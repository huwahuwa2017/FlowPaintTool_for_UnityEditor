Shader "FlowPaintTool2/Bleed2"
{
	Properties
	{
		[NoScaleOffset]
		_MainTex("Main Texture", 2D) = "white" {}
		[NoScaleOffset]
		_FillTex("Fill Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM

			#pragma vertex VertexShaderStage_Bleed
			#pragma fragment FragmentShaderStage_Bleed

			#include "FillCutoutBleed.hlsl"

			ENDCG
		}
	}
}
