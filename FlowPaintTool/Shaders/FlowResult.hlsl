#include "UnityCG.cginc"
#include "TargetUVChannel.hlsl"

struct TessellationFactor
{
    float tessFactor[3] : SV_TessFactor;
    float insideTessFactor : SV_InsideTessFactor;
};

struct I2V
{
    float4 lPos : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 uv : TARGET_UV_CHANNEL;
};

struct V2G
{
    float3 wPos : TEXCOORD0;
    float4 cPos : TEXCOORD1;
    float2 uv : TEXCOORD2;
    float3x3 matrixT2W : TEXCOORD3;
};

struct G2F
{
    float4 cPos : SV_POSITION;
    float2 uv : TEXCOORD0;
    bool directionDisplay : TEXCOORD1;
};

sampler2D _MainTex;

float _DisplayNormalAmount;
float _DisplayNormalLength;

static float _Reciprocal3 = 1.0 / 3.0;

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

V2G VertexShaderStage(I2V input)
{
    float4 wPos = mul(UNITY_MATRIX_M, input.lPos);
    float3x3 matrixT2W = Matrix_T2W(input.normal, input.tangent);
    
    V2G output = (V2G) 0;
    output.wPos = wPos.xyz;
    output.cPos = mul(UNITY_MATRIX_VP, wPos);
    output.uv = input.uv;
    output.matrixT2W = matrixT2W;
    return output;
}

[domain("tri")]
[partitioning("integer")]
[outputtopology("triangle_cw")]
[patchconstantfunc("PatchConstantFunction")]
[outputcontrolpoints(3)]
V2G HullShaderStage(InputPatch<V2G, 3> input, uint id : SV_OutputControlPointID)
{
    return input[id];
}

TessellationFactor PatchConstantFunction(InputPatch<V2G, 3> input)
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
V2G DomainShaderStage(TessellationFactor tf, const OutputPatch<V2G, 3> input, float3 bary : SV_DomainLocation)
{
    V2G output = (V2G) 0;
    output.wPos = input[0].wPos * bary.x + input[1].wPos * bary.y + input[2].wPos * bary.z;
    output.cPos = input[0].cPos * bary.x + input[1].cPos * bary.y + input[2].cPos * bary.z;
    output.uv = input[0].uv * bary.x + input[1].uv * bary.y + input[2].uv * bary.z;
    output.matrixT2W = ScalarMul(input[0].matrixT2W, bary.x) + ScalarMul(input[1].matrixT2W, bary.y) + ScalarMul(input[2].matrixT2W, bary.z);
    return output;
}

[maxvertexcount(7)]
void GeometryShaderStage(triangle V2G input[3], inout TriangleStream<G2F> stream)
{
    float3 wPos = (input[0].wPos + input[1].wPos + input[2].wPos) * _Reciprocal3;
    float2 uv = (input[0].uv + input[1].uv + input[2].uv) * _Reciprocal3;
    float3x3 matrixT2W = ScalarMul(input[0].matrixT2W + input[1].matrixT2W + input[2].matrixT2W, _Reciprocal3);

    float3 color = tex2Dlod(_MainTex, float4(uv, 0.0, 0.0)).rgb;
    float3 wDirection = color * 2.0 - 1.0;
    wDirection = normalize(mul(matrixT2W, wDirection));
    //wDirection = normalize(wTangent * wDirection.x + wBinormal * wDirection.y + wNormal * wDirection.z);
    
    float3 viewDir = wPos - _WorldSpaceCameraPos;
    float3 temp1 = normalize(cross(viewDir, wDirection));
    
    float3 pos0 = wPos;
    float3 pos1 = wPos + (wDirection * 0.5 + temp1 * 0.02) * _DisplayNormalLength;
    float3 pos2 = wPos + (wDirection * 0.5 - temp1 * 0.02) * _DisplayNormalLength;
    float3 pos3 = wPos + wDirection * _DisplayNormalLength;
    
    G2F output = (G2F) 0;
    
    output.cPos = input[0].cPos;
    output.uv = input[0].uv;
    output.directionDisplay = false;
    stream.Append(output);
        
    output.cPos = input[1].cPos;
    output.uv = input[1].uv;
    output.directionDisplay = false;
    stream.Append(output);
        
    output.cPos = input[2].cPos;
    output.uv = input[2].uv;
    output.directionDisplay = false;
    stream.Append(output);
    
    if ((!any(color)) || (_DisplayNormalAmount == 0.0))
        return;
    
    stream.RestartStrip();
    
    output.cPos = UnityWorldToClipPos(pos0);
    output.uv = uv;
    output.directionDisplay = true;
    stream.Append(output);
    
    output.cPos = UnityWorldToClipPos(pos1);
    output.uv = uv;
    output.directionDisplay = true;
    stream.Append(output);
    
    output.cPos = UnityWorldToClipPos(pos2);
    output.uv = uv;
    output.directionDisplay = true;
    stream.Append(output);
    
    output.cPos = UnityWorldToClipPos(pos3);
    output.uv = uv;
    output.directionDisplay = true;
    stream.Append(output);
}

float4 FragmentShaderStage(G2F input) : SV_Target
{
    float4 color = input.directionDisplay ? float4(0.0, 1.0, 1.0, 1.0) : tex2D(_MainTex, input.uv);
    
#if !defined(UNITY_COLORSPACE_GAMMA)
    color.rgb = GammaToLinearSpace(color.rgb);
#endif
    
    return color;

}
