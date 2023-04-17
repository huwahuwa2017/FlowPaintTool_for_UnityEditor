Shader "FlowPaintTool2/ColorPaint2"
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

			#pragma vertex VertexShaderStage_ColorPaint
			#pragma fragment FragmentShaderStage_ColorPaint

			#include "Paint2.hlsl"

			ENDCG
		}
	}
}
