Shader "FlowPaintTool2/Fill2"
{
	SubShader
	{
		Pass
		{
			Cull Off

			CGPROGRAM

			#pragma vertex VertexShaderStage_Fill
			#pragma fragment FragmentShaderStage_Fill

			#include "FillCutoutBleed.hlsl"

			ENDCG
		}
	}
}
