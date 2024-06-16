Shader "FlowPaintTool2/Fill2"
{
	SubShader
	{
		Pass
		{
			Cull Off

			CGPROGRAM

			#pragma multi_compile_local UV_CHANNEL_0 UV_CHANNEL_1 UV_CHANNEL_2 UV_CHANNEL_3 UV_CHANNEL_4 UV_CHANNEL_5 UV_CHANNEL_6 UV_CHANNEL_7

			#pragma vertex VertexShaderStage_Fill
			#pragma fragment FragmentShaderStage_Fill

			#include "DrawRendererShader.hlsl"

			ENDCG
		}
	}
}
