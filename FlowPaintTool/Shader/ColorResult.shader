Shader "FlowPaintTool2/ColorResult"
{
	Properties
	{
		[NoScaleOffset]
		_MainTex("Main Texture", 2D) = "white" {}
	}

	SubShader
	{
		Tags
		{
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
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

			#pragma multi_compile_local _ IS_SRGB

			#pragma vertex VertexShaderStage
			#pragma fragment FragmentShaderStage

			#include "ColorResult.hlsl"

			ENDCG
		}
	}
}