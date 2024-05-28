Shader "FlowPaintTool2/RangeVisualization"
{
	SubShader
	{
		Tags
		{
			"Queue" = "Overlay"
		}

		GrabPass
        {
            "_FPT_RangeVisualization"
        }

		Pass
		{
			ZWrite Off
			ZTest Always
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#pragma vertex VertexShaderStage
			#pragma fragment FragmentShaderStage

			#include "UnityCG.cginc"

			struct I2V
			{
				float4 lPos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct V2F
			{
				float4 cPos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Texture2D _FPT_RangeVisualization;

			V2F VertexShaderStage(I2V input)
			{
				V2F output = (V2F)0;
				output.cPos = UnityObjectToClipPos(input.lPos);
				output.uv = input.uv * 2.0 - 1.0;
				return output;
			}

			half4 FragmentShaderStage(V2F input) : SV_Target
			{
				float temp0 = length(input.uv);
				temp0 = pow(temp0, 16);
				clip(1.0 - temp0);

				float4 color = 1.0 - _FPT_RangeVisualization[uint2(input.cPos.xy)];
				color.a = temp0;
				return color;
			}

			ENDCG
		}
	}
}
