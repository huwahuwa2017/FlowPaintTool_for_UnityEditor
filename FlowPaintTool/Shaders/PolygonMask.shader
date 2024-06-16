Shader "TexelReadWrite/PolygonMask"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM

			#pragma require geometry

			#pragma vertex VertexShaderStage_PolygonMask
			#pragma geometry GeometryShaderStage_PolygonMask
			#pragma fragment FragmentShaderStage_PolygonMask

			#include "DrawRendererShader.hlsl"

			ENDCG
		}
	}
}
