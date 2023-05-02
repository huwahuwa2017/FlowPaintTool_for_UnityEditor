Shader "FlowPaintTool2/ColorMerge"
{
	Properties
	{
		// Input from Blit
		[HideInInspector]
		_MainTex("Main Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM

			#pragma vertex VertexShaderStage
			#pragma fragment FragmentShaderStage_ColorMerge

			#include "BlitShader.hlsl"

			ENDCG
		}
	}
}
