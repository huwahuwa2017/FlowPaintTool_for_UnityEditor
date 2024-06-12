Shader "FlowPaintTool2/WorldPosition"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM

			#pragma vertex VertexShaderStage
			#pragma fragment FragmentShaderStage

			#include "UnityCG.cginc"

			struct I2V
			{
				float4 lPos : POSITION;
			};

			struct V2F
			{
				float4 cPos : SV_POSITION;
				float3 wPos : TEXCOORD0;
			};

			V2F VertexShaderStage(I2V input)
			{
				float3 wPos = mul(UNITY_MATRIX_M, input.lPos).xyz;

				V2F output = (V2F)0;
				output.cPos = UnityWorldToClipPos(float4(wPos, 1.0));
				output.wPos = wPos;
				return output;
			}

			float4 FragmentShaderStage(V2F input) : SV_Target
			{
				return float4(input.wPos, 1.0);
			}

			ENDCG
		}
	}
}
