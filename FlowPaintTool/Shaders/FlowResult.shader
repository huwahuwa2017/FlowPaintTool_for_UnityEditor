Shader "FlowPaintTool2/FlowResult"
{
	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
		}
		
		Pass
		{
			CGPROGRAM

			#pragma multi_compile_local UV_CHANNEL_0 UV_CHANNEL_1 UV_CHANNEL_2 UV_CHANNEL_3 UV_CHANNEL_4 UV_CHANNEL_5 UV_CHANNEL_6 UV_CHANNEL_7
			#pragma multi_compile_local _ IS_SRGB

			#pragma vertex VertexShaderStage_ColorResult
			#pragma fragment FragmentShaderStage_ColorResult

			#include "Result.hlsl"

			ENDCG
		}

		Pass
		{
			CGPROGRAM

			#pragma multi_compile_local UV_CHANNEL_0 UV_CHANNEL_1 UV_CHANNEL_2 UV_CHANNEL_3 UV_CHANNEL_4 UV_CHANNEL_5 UV_CHANNEL_6 UV_CHANNEL_7

			#pragma require tessellation
			#pragma require geometry

			#pragma vertex VertexShaderStage_FlowResult
			#pragma hull HullShaderStage_FlowResult
			#pragma domain DomainShaderStage_FlowResult
			#pragma geometry GeometryShaderStage_FlowResult
			#pragma fragment FragmentShaderStage_FlowResult

			#include "Result.hlsl"

			ENDCG
		}
	}
}