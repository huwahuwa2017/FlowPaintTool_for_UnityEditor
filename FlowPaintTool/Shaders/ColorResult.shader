Shader "FlowPaintTool2/ColorResult"
{
	SubShader
	{
		Tags
		{
			"RenderType" = "Transparent"
			"Queue" = "Overlay-1"
		}

		Pass
		{
			ZWrite ON
			ColorMask 0
		}

		Pass
		{
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#pragma multi_compile_local UV_CHANNEL_0 UV_CHANNEL_1 UV_CHANNEL_2 UV_CHANNEL_3 UV_CHANNEL_4 UV_CHANNEL_5 UV_CHANNEL_6 UV_CHANNEL_7
			#pragma multi_compile_local _ IS_SRGB

			#pragma vertex VertexShaderStage_ColorResult
			#pragma fragment FragmentShaderStage_ColorResult

			#include "Result.hlsl"

			ENDCG
		}
	}
}