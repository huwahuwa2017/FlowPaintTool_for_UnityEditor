#include "HuwaTexelReadWrite.hlsl"
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
    float3 wPos : TEXCOORD0;
    float3 direction : TEXCOORD1;
};

struct F2O
{
    float4 color0 : SV_Target0;
    float color1 : SV_Target1;
};

Texture2D _PolygonMaskTex;
float4 _PolygonMaskTex_TexelSize;

Texture2D _PolygonThinningTex;
float4 _PolygonThinningTex_TexelSize;

Texture2D _PaintTex;
Texture2D _DensityTex;

float3 _HitPosition;
float3 _PreHitPosition;

int _BrushType;
float _BrushSize;
float _BrushStrength;

float3 _PaintDirection;
int _FixedHeight;
float _FixedHeightMin;
float _FixedHeightMax;

float4 _PaintColor;
int _EditRGBA;

float SafeDivision(float a, float b)
{
    return (b == 0.0) ? 0.0 : a / b;
}

float DF_Capsule(float3 p, float3 a, float3 b)
{
    float3 ap = p - a;
    float3 ab = b - a;
    return length(ap - ab * saturate(SafeDivision(dot(ap, ab), dot(ab, ab))));
}

float Density(float2 cPosXY, float invAattenuation)
{
    float attenuation = 1.0 - invAattenuation;
    
    float type0 = 1.0;
    float type1 = attenuation;
    float type2 = (3.0 - 2.0 * attenuation) * attenuation * attenuation;
    float type3 = attenuation * attenuation;
    float type4 = 1.0 - invAattenuation * invAattenuation;
    
    type0 *= _BrushType == 0;
    type1 *= _BrushType == 1;
    type2 *= _BrushType == 2;
    type3 *= _BrushType == 3;
    type4 *= _BrushType == 4;
    
    float density = type0 + type1 + type2 + type3 + type4;
    float preDensity = _DensityTex[uint2(cPosXY)].r;
    return max(preDensity, density * _BrushStrength);
}



V2F VertexShaderStage_Fill(I2V input)
{
    V2F output = (V2F) 0;
    output.cPos = float4(input.uv * 2.0 - 1.0, 0.5, 1.0);
    
#if UNITY_UV_STARTS_AT_TOP
    output.cPos.y = -output.cPos.y;
#endif
    
    return output;
}

float4 FragmentShaderStage_Fill(V2F input) : SV_Target
{
    return 1.0;
}



V2F VertexShaderStage_FlowPaint(I2V input)
{
    float3 wNormal = UnityObjectToWorldNormal(input.lNormal);
    float3 wTangent = UnityObjectToWorldDir(input.lTangent.xyz);
    float3 wBinormal = cross(wNormal, wTangent) * (input.lTangent.w * unity_WorldTransformParams.w);
    float3x3 w2t = float3x3(wTangent, wBinormal, wNormal);
    
    V2F output = (V2F) 0;
    output.cPos = float4(input.uv * 2.0 - 1.0, 0.5, 1.0);
    output.wPos = mul(UNITY_MATRIX_M, input.lPos);
    output.direction = mul(w2t, _PaintDirection);
    
#if UNITY_UV_STARTS_AT_TOP
    output.cPos.y = -output.cPos.y;
#endif
    
    return output;
}

[maxvertexcount(3)]
void GeometryShaderStage_FlowPaint(triangle V2F input[3], inout TriangleStream<V2F> stream, uint primitiveID : SV_PrimitiveID)
{
    float polygonMask;
    HTRW_TEXEL_READ(_PolygonMaskTex, primitiveID, polygonMask);
    
    float polygonThinning;
    HTRW_TEXEL_READ(_PolygonThinningTex, primitiveID, polygonThinning);
    
    if ((polygonMask != 0.0) || (polygonThinning != 0.0))
        return;
    
    stream.Append(input[0]);
    stream.Append(input[1]);
    stream.Append(input[2]);
}

F2O FragmentShaderStage_FlowPaint(V2F input)
{
    float invAattenuation = DF_Capsule(input.wPos, _PreHitPosition, _HitPosition) / _BrushSize;
    float attenuation = 1.0 - invAattenuation;
    clip(attenuation);
    
    float4 baseColor = _PaintTex[uint2(input.cPos.xy)];
    float3 vector_10 = baseColor.rgb * 2.0 - 1.0;
    float3 localDirection = normalize(input.direction);
    vector_10 = lerp(vector_10, localDirection, saturate(attenuation + (baseColor.a == 0.0)));
    vector_10 = normalize(vector_10);
    
    float z = clamp(vector_10.z, _FixedHeightMin, _FixedHeightMax);
    float2 xy = vector_10.xy;
    float xyL = length(xy);
    xy = xy / (xyL + (xyL == 0.0));
    float3 vector_11 = float3(xy * sqrt(1.0 - z * z), z);
    float3 vector_12 = lerp(vector_10, vector_11, _FixedHeight && (xyL != 0.0));
    
    F2O output = (F2O) 0;
    output.color0 = float4(vector_12 * 0.5 + 0.5, 1.0);
    output.color1 = Density(input.cPos.xy, invAattenuation);
    return output;
}



V2F VertexShaderStage_ColorPaint(I2V input)
{
    V2F output = (V2F) 0;
    output.cPos = float4(input.uv * 2.0 - 1.0, 0.5, 1.0);
    output.wPos = mul(UNITY_MATRIX_M, input.lPos);
    
#if UNITY_UV_STARTS_AT_TOP
    output.cPos.y = -output.cPos.y;
#endif
    
    return output;
}

[maxvertexcount(3)]
void GeometryShaderStage_ColorPaint(triangle V2F input[3], inout TriangleStream<V2F> stream, uint primitiveID : SV_PrimitiveID)
{
    float polygonMask;
    HTRW_TEXEL_READ(_PolygonMaskTex, primitiveID, polygonMask);
    
    float polygonThinning;
    HTRW_TEXEL_READ(_PolygonThinningTex, primitiveID, polygonThinning);
    
    if ((polygonMask != 0.0) || (polygonThinning != 0.0))
        return;
    
    stream.Append(input[0]);
    stream.Append(input[1]);
    stream.Append(input[2]);
}

F2O FragmentShaderStage_ColorPaint(V2F input)
{
    float invAattenuation = DF_Capsule(input.wPos, _PreHitPosition, _HitPosition) / _BrushSize;
    float attenuation = 1.0 - invAattenuation;
    clip(attenuation);
    
    float4 paintColor = _PaintTex[uint2(input.cPos.xy)];
    paintColor.r = lerp(paintColor.r, _PaintColor.r, _EditRGBA & 1);
    paintColor.g = lerp(paintColor.g, _PaintColor.g, (_EditRGBA >> 1) & 1);
    paintColor.b = lerp(paintColor.b, _PaintColor.b, (_EditRGBA >> 2) & 1);
    paintColor.a = lerp(paintColor.a, _PaintColor.a, (_EditRGBA >> 3) & 1);
    
    F2O output = (F2O) 0;
    output.color0 = paintColor;
    output.color1 = Density(input.cPos.xy, invAattenuation);
    return output;
}



struct V2G
{
    float4 wPos : TEXCOORD0;
};

struct G2F
{
    float4 cPos : SV_POSITION;
    float data : TEXCOORD0;
};

float _ChangeMask;

V2G VertexShaderStage_PolygonMask(I2V input)
{
    V2G output = (V2G) 0;
    output.wPos = mul(UNITY_MATRIX_M, input.lPos);
    return output;
}

[maxvertexcount(4)]
void GeometryShaderStage_PolygonMask(triangle V2G input[3], inout TriangleStream<G2F> stream, uint primitiveID : SV_PrimitiveID)
{
    float3 wPos = (input[0].wPos.xyz + input[1].wPos.xyz + input[2].wPos.xyz) / 3.0;
    
    float invAattenuation = DF_Capsule(wPos, _PreHitPosition, _HitPosition);
    bool update = invAattenuation < _BrushSize;
    
    float preData;
    HTRW_TEXEL_READ(_PolygonMaskTex, primitiveID, preData)
    
    G2F output = (G2F) 0;
    output.data = update ? _ChangeMask : (preData != 0.0);
    HTRW_SET_WRITE_TEXTURE_SIZE(_PolygonMaskTex_TexelSize.zw);
    HTRW_TEXEL_WRITE(primitiveID, output.cPos, stream);
}

float FragmentShaderStage_PolygonMask(G2F input) : SV_Target
{
    return input.data;
}



float2 _TextureSize;

V2G VertexShaderStage_SqrMagnitude(I2V input)
{
    V2G output = (V2G) 0;
    output.wPos = mul(UNITY_MATRIX_M, input.lPos);
    return output;
}

[maxvertexcount(4)]
void GeometryShaderStage_SqrMagnitude(triangle V2G input[3], inout TriangleStream<G2F> stream, uint primitiveID : SV_PrimitiveID)
{
    float3 wPos = (input[0].wPos.xyz + input[1].wPos.xyz + input[2].wPos.xyz) / 3.0;
    
    float3 temp0 = wPos - _PreHitPosition;
    
    G2F output = (G2F) 0;
    output.data = dot(temp0, temp0);
    HTRW_SET_WRITE_TEXTURE_SIZE(_TextureSize);
    HTRW_TEXEL_WRITE(primitiveID, output.cPos, stream);
}

float FragmentShaderStage_SqrMagnitude(G2F input) : SV_Target
{
    return input.data;
}