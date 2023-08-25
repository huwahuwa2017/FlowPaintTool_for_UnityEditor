Shader "FlowPaintTool2/UnpackNormal"
{
	Properties
	{
		// Input from Blit
		[HideInInspector]
		_MainTex("Main Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM

			#pragma vertex VertexShaderStage
			#pragma fragment FragmentShaderStage_UnpackNormal

			#include "BlitShader.hlsl"

			ENDCG
		}
	}
}
