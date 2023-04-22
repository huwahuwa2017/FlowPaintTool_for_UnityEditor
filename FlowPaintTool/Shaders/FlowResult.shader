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

			#pragma require tessellation
			#pragma require geometry

			#pragma vertex VertexShaderStage
			#pragma hull HullShaderStage
			#pragma domain DomainShaderStage
			#pragma geometry GeometryShaderStage
			#pragma fragment FragmentShaderStage

			#include "FlowResult.hlsl"

			ENDCG
		}
	}
}