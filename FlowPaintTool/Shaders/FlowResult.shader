Shader "FlowPaintTool2/FlowResult"
{
	Properties
	{
		[NoScaleOffset]
		_MainTex("Main Texture", 2D) = "white" {}
		_DisplayNormalAmount("Display Normal Amount", Float) = 64.0
		_DisplayNormalLength("Display Normal Length", Float) = 0.02
	}

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