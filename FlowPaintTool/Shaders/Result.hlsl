#include "UnityCG.cginc"
#include "TargetUVChannel.hlsl"

struct I2V
{
    float4 lPos : POSITION;
    float3 lNormal : NORMAL;
    float4 lTangent : TANGENT;
    float2 uv : TARGET_UV_CHANNEL;
};

struct V2F
{
    float4 cPos : SV_POSITION;
    float2 uv : TEXCOORD0;
};

sampler2D _MainTex;

V2F VertexShaderStage_ColorResult(I2V input)
{
    V2F output = (V2F) 0;
    output.cPos = UnityObjectToClipPos(input.lPos);
    output.uv = input.uv;
    return output;
}

float4 FragmentShaderStage_ColorResult(V2F input) : SV_Target
{
    float4 color = tex2D(_MainTex, input.uv);
    
#if (!defined(UNITY_COLORSPACE_GAMMA)) && (!defined(IS_SRGB))
    color.rgb = GammaToLinearSpace(color.rgb);
#endif
    
    return color;
}



struct TessellationFactor
{
    float tessFactor[3] : SV_TessFactor;
    float insideTessFactor : SV_InsideTessFactor;
};

struct V2G
{
    float3 wPos : TEXCOORD0;
    float2 uv : TEXCOORD1;
    float3x3 matrixT2W : TEXCOORD2;
};

struct G2F
{
    float4 cPos : SV_POSITION;
};

float _DisplayNormalAmount;
float _DisplayNormalLength;

float3x3 Matrix_T2W(float3 normal, float4 tangent)
{
    float3 wNormal = UnityObjectToWorldNormal(normal);
    float3 wTangent = normalize(mul((float3x3) UNITY_MATRIX_M, tangent.xyz));
    float3 wBinormal = cross(wNormal, wTangent) * (tangent.w * unity_WorldTransformParams.w);
    
    return float3x3
    (
        wTangent.x, wBinormal.x, wNormal.x,
        wTangent.y, wBinormal.y, wNormal.y,
        wTangent.z, wBinormal.z, wNormal.z
    );
}

float3x3 ScalarMul(float3x3 mat, float scalar)
{
    return mat * scalar;
}

V2G VertexShaderStage_FlowResult(I2V input)
{
    float4 wPos = mul(UNITY_MATRIX_M, input.lPos);
    float3x3 matrixT2W = Matrix_T2W(input.lNormal, input.lTangent);
    
    V2G output = (V2G) 0;
    output.wPos = wPos.xyz;
    output.uv = input.uv;
    output.matrixT2W = matrixT2W;
    return output;
}

[domain("tri")]
[partitioning("integer")]
[outputtopology("triangle_cw")]
[patchconstantfunc("PatchConstantFunction_FlowResult")]
[outputcontrolpoints(3)]
V2G HullShaderStage_FlowResult(InputPatch<V2G, 3> input, uint id : SV_OutputControlPointID)
{
    return input[id];
}

TessellationFactor PatchConstantFunction_FlowResult(InputPatch<V2G, 3> input)
{
    float area = length(cross(input[1].wPos - input[0].wPos, input[2].wPos - input[0].wPos)) * 0.5;
    float temp_4 = min(sqrt(area) * _DisplayNormalAmount, 64.0);
    
    TessellationFactor output;
    output.tessFactor[0] = max(temp_4, 1.0);
    output.tessFactor[1] = max(temp_4, 1.0);
    output.tessFactor[2] = max(temp_4, 1.0);
    output.insideTessFactor = temp_4;
    return output;
}

[domain("tri")]
V2G DomainShaderStage_FlowResult(TessellationFactor tf, const OutputPatch<V2G, 3> input, float3 bary : SV_DomainLocation)
{
    V2G output = (V2G) 0;
    output.wPos = input[0].wPos * bary.x + input[1].wPos * bary.y + input[2].wPos * bary.z;
    output.uv = input[0].uv * bary.x + input[1].uv * bary.y + input[2].uv * bary.z;
    output.matrixT2W = ScalarMul(input[0].matrixT2W, bary.x) + ScalarMul(input[1].matrixT2W, bary.y) + ScalarMul(input[2].matrixT2W, bary.z);
    return output;
}

[maxvertexcount(2)]
void GeometryShaderStage_FlowResult(triangle V2G input[3], inout LineStream<G2F> stream)
{
    float inv3 = 1.0 / 3.0;
    
    float3 wPos = (input[0].wPos + input[1].wPos + input[2].wPos) * inv3;
    float2 uv = (input[0].uv + input[1].uv + input[2].uv) * inv3;
    float3x3 matrixT2W = ScalarMul(input[0].matrixT2W + input[1].matrixT2W + input[2].matrixT2W, inv3);

    float3 color = tex2Dlod(_MainTex, float4(uv, 0.0, 0.0)).rgb;
    float3 wDirection = color * 2.0 - 1.0;
    wDirection = normalize(mul(matrixT2W, wDirection));
    
    G2F output = (G2F) 0;
    
    output.cPos = UnityWorldToClipPos(wPos);
    stream.Append(output);
    
    output.cPos = UnityWorldToClipPos(wPos + wDirection * _DisplayNormalLength);
    stream.Append(output);
}

float4 FragmentShaderStage_FlowResult(G2F input) : SV_Target
{
    return float4(0.0, 1.0, 1.0, 1.0);
}
