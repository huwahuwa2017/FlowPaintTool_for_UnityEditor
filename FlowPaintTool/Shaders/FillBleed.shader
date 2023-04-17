Shader "FlowPaintTool2/FillBleed"
{
	Properties
	{
		[NoScaleOffset]
		_MainTex("Main Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM

			#pragma vertex VertexShaderStage_FillBleed
			#pragma fragment FragmentShaderStage_FillBleed

			#include "FillCutoutBleed.hlsl"

			ENDCG
		}
	}
}
