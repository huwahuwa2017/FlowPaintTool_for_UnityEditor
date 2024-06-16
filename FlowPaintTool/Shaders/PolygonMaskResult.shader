Shader "FlowPaintTool2/PolygonMaskResult"
{
	Properties
	{
		_Color0("Mask off", Color) = (0.0, 0.0, 0.0, 1.0)
		_Color1("Mask on", Color) = (1.0, 1.0, 1.0, 1.0)
		_WireFrameColor("Wire Frame Color", Color) = (0.5, 0.5, 0.5, 1.0)
		_Width("Frame Width", Range(0.0, 99.0)) = 1.0
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

			#pragma vertex VertexShaderStage_PolygonMaskResult
			#pragma geometry GeometryShaderStage_PolygonMaskResult
			#pragma fragment FragmentShaderStage_PolygonMaskResult

			#include "WireFrame.hlsl"

			ENDCG
		}
	}
}