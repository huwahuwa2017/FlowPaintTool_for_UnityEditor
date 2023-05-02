Shader "FlowPaintTool2/Cutout"
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
			#pragma fragment FragmentShaderStage_Cutout

			#include "BlitShader.hlsl"

			ENDCG
		}
	}
}
