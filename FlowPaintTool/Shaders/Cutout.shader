Shader "FlowPaintTool2/Cutout"
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

			#pragma vertex VertexShaderStage_Cutout
			#pragma fragment FragmentShaderStage_Cutout

			#include "FillCutoutBleed.hlsl"

			ENDCG
		}
	}
}
