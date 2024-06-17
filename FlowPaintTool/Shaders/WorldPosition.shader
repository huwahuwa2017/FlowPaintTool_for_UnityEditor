Shader "FlowPaintTool2/WorldPosition"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM

			#pragma vertex VertexShaderStage_WorldPosition
			#pragma fragment FragmentShaderStage_WorldPosition

			#include "DrawRendererShader.hlsl"

			ENDCG
		}
	}
}
