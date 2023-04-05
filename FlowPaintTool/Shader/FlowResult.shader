Shader "FlowPaintTool2/FlowResult"
{
	Properties
	{
		[NoScaleOffset]
		_MainTex("Main Texture", 2D) = "white" {}
		_TessFactor("TessFactor", Float) = 64.0
		_DirectionScale("DirectionScale", Float) = 0.02
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