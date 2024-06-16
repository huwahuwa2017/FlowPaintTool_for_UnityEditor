Shader "TexelReadWrite/SqrMagnitude"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM

			#pragma require geometry

			#pragma vertex VertexShaderStage_SqrMagnitude
			#pragma geometry GeometryShaderStage_SqrMagnitude
			#pragma fragment FragmentShaderStage_SqrMagnitude

			#include "DrawRendererShader.hlsl"

			ENDCG
		}
	}
}
