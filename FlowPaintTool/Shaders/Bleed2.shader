Shader "FlowPaintTool2/Bleed2"
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
			#pragma fragment FragmentShaderStage_Bleed

			#include "BlitShader.hlsl"

			ENDCG
		}
	}
}
