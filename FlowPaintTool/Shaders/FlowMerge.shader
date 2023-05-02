Shader "FlowPaintTool2/FlowMerge"
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
			#pragma fragment FragmentShaderStage_FlowMerge

			#include "BlitShader.hlsl"

			ENDCG
		}
	}
}
