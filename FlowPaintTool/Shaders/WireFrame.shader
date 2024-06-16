Shader "FlowPaintTool2/WireFrame"
{
	Properties
	{
		_MainColor("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_WireFrameColor("Wire Frame Color", Color) = (0.5, 0.5, 0.5, 1.0)
		_Width("Width", Range(0.0, 99.0)) = 1.0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Geometry"
			"RenderType" = "Opaque"
		}

		Pass
		{
			CGPROGRAM

			#pragma require geometry

			#pragma vertex VertexShaderStage_WireFrame
			#pragma geometry GeometryShaderStage_WireFrame
			#pragma fragment FragmentShaderStage_WireFrame

			#include "WireFrame.hlsl"

			ENDCG
		}
	}
}