Shader "FlowPaintTool2/FillBleed"
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

			#pragma multi_compile_local UV_CHANNEL_0 UV_CHANNEL_1 UV_CHANNEL_2 UV_CHANNEL_3 UV_CHANNEL_4 UV_CHANNEL_5 UV_CHANNEL_6 UV_CHANNEL_7

			#pragma vertex VertexShaderStage_FillBleed
			#pragma fragment FragmentShaderStage_FillBleed

			#include "FillCutoutBleed.hlsl"

			ENDCG
		}
	}
}
