Shader "FlowPaintTool2/FlowPaint2"
{
	Properties
	{
		[NoScaleOffset]
		_MainTex("Main Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			Cull Off

			CGPROGRAM

			#pragma vertex VertexShaderStage_FlowPaint
			#pragma fragment FragmentShaderStage_FlowPaint

			#include "Paint2.hlsl"

			ENDCG
		}
	}
}
