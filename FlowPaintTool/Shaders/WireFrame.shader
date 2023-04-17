//Ver1 2023/02/24 16:56

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
			//Cull Off

			CGPROGRAM

			#pragma require geometry

			#pragma vertex VertexShaderStage
			#pragma geometry GeometryShaderStage
			#pragma fragment FragmentShaderStage

			#include "UnityCG.cginc"

			struct I2V
			{
				float4 lPos : POSITION;
			};

			struct V2G
			{
				float4 cPos : TEXCOORD0;
			};

			struct G2F
			{
				float4 cPos : SV_POSITION;
				float3 distance : TEXCOORD0;
			};

			fixed4 _MainColor;
			fixed4 _WireFrameColor;
			float _Width;

			V2G VertexShaderStage(I2V input)
			{
				V2G output = (V2G)0;
				output.cPos = UnityObjectToClipPos(input.lPos);
				return output;
			}

			[maxvertexcount(3)]
			void GeometryShaderStage(triangle V2G input[3], inout TriangleStream<G2F> stream)
			{
				float2 pos0 = input[0].cPos.xy * _ScreenParams.xy / input[0].cPos.w;
				float2 pos1 = input[1].cPos.xy * _ScreenParams.xy / input[1].cPos.w;
				float2 pos2 = input[2].cPos.xy * _ScreenParams.xy / input[2].cPos.w;

				float2 side0 = pos2 - pos1;
				float2 side1 = pos0 - pos2;
				float2 side2 = pos1 - pos0;

				float area = abs(side0.x * side1.y - side0.y * side1.x);

				G2F output = (G2F)0;

				output.cPos = input[0].cPos;
				output.distance = float3(area / length(side0), 0.0, 0.0);
				stream.Append(output);

				output.cPos = input[1].cPos;
				output.distance = float3(0.0, area / length(side1), 0.0);
				stream.Append(output);

				output.cPos = input[2].cPos;
				output.distance = float3(0.0, 0.0, area / length(side2));
				stream.Append(output);
			}

			fixed4 FragmentShaderStage(G2F input) : SV_Target
			{
				bool flag = (input.distance.x < _Width) || (input.distance.y < _Width) || (input.distance.z < _Width);
				fixed4 color = lerp(_MainColor, _WireFrameColor, flag);
				clip(color.a - 0.5);
				return color;
			}

			ENDCG
		}
	}
}