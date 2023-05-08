#include "UnityCG.cginc"
#include "TargetUVChannel.hlsl"

struct I2V
{
    float4 lPos : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 uv : TARGET_UV_CHANNEL;
};

struct V2F
{
    float4 cPos : SV_POSITION;
    float3 wPos : TEXCOORD0;
    float3 direction : TEXCOORD1;
};

Texture2D _MainTex;

float4x4 _ModelMatrix;
float3 _HitPosition;

int _BrushType;
float _BrushSize;
float _BrushStrength;

float4x4 _InverseModelMatrix;
float3 _PaintDirection;

int _FixedHeight;
float _FixedHeightMin;
float _FixedHeightMax;

float4 _PaintColor;
int _EditRGBA;

float3x3 Matrix_WorldSpaceToTangentSpace(float3 normal, float4 tangent)
{
    float3 wNormal = normalize(mul(normal, (float3x3) _InverseModelMatrix));
    float3 wTangent = normalize(mul((float3x3) _ModelMatrix, tangent.xyz));
    float3 wBinormal = cross(wNormal, wTangent) * (tangent.w * unity_WorldTransformParams.w);
    
    return float3x3
    (
        wTangent,
        wBinormal,
        wNormal
    );
}



V2F VertexShaderStage_Fill(I2V input)
{
    V2F output = (V2F) 0;
    output.cPos = float4(input.uv * 2.0 - 1.0, 0.0, 1.0);
    
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
    float3x3 w2t = Matrix_WorldSpaceToTangentSpace(input.normal, input.tangent);
    
    V2F output = (V2F) 0;
    output.cPos = float4(input.uv * 2.0 - 1.0, 0.0, 1.0);
    output.wPos = mul(_ModelMatrix, input.lPos);
    output.direction = mul(w2t, _PaintDirection);
    
#if UNITY_UV_STARTS_AT_TOP
    output.cPos.y = -output.cPos.y;
#endif
    
    return output;
}

float4 FragmentShaderStage_FlowPaint(V2F input) : SV_Target
{
    float attenuation = 1.0 - distance(input.wPos, _HitPosition) / _BrushSize;
    clip(attenuation);
    attenuation = saturate(attenuation);
    
    float4 baseColor = _MainTex[uint2(input.cPos.xy)];
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
    
    return float4(vector_12 * 0.5 + 0.5, 1.0);
}



V2F VertexShaderStage_ColorPaint(I2V input)
{
    V2F output = (V2F) 0;
    output.cPos = float4(input.uv * 2.0 - 1.0, 0.0, 1.0);
    output.wPos = mul(_ModelMatrix, input.lPos);
    
#if UNITY_UV_STARTS_AT_TOP
    output.cPos.y = -output.cPos.y;
#endif
    
    return output;
}

half4 FragmentShaderStage_ColorPaint(V2F input) : SV_Target
{
    float attenuation = 1.0 - distance(input.wPos, _HitPosition) / _BrushSize;
    clip(attenuation);
    
    half4 paintColor = _MainTex[uint2(input.cPos.xy)];
    paintColor.r = lerp(paintColor.r, _PaintColor.r, _EditRGBA & 1);
    paintColor.g = lerp(paintColor.g, _PaintColor.g, (_EditRGBA >> 1) & 1);
    paintColor.b = lerp(paintColor.b, _PaintColor.b, (_EditRGBA >> 2) & 1);
    paintColor.a = lerp(paintColor.a, _PaintColor.a, (_EditRGBA >> 3) & 1);
    
    return paintColor;
}



V2F VertexShaderStage_Density(I2V input)
{
    V2F output = (V2F) 0;
    output.cPos = float4(input.uv * 2.0 - 1.0, 0.0, 1.0);
    output.wPos = mul(_ModelMatrix, input.lPos);
    
#if UNITY_UV_STARTS_AT_TOP
    output.cPos.y = -output.cPos.y;
#endif
    
    return output;
}

half FragmentShaderStage_Density(V2F input) : SV_Target
{
    float attenuation = 1.0 - distance(input.wPos, _HitPosition) / _BrushSize;
    clip(attenuation);
    
    float preDensity = _MainTex[uint2(input.cPos.xy)].r;
    
    float hitPositionDistance = distance(input.wPos, _HitPosition);
    float shpdi = saturate(hitPositionDistance / _BrushSize);
    float shpd = 1.0 - shpdi;
    
    float type0 = hitPositionDistance < _BrushSize;
    type0 *= _BrushType == 0;
    
    float type1 = shpd;
    type1 *= _BrushType == 1;
    
    float type2 = (3.0 - 2.0 * shpd) * shpd * shpd;
    type2 *= _BrushType == 2;
    
    float type3 = shpd * shpd;
    type3 *= _BrushType == 3;

    float type4 = 1 - shpdi * shpdi;
    type4 *= _BrushType == 4;
    
    float density = type0 + type1 + type2 + type3 + type4;
    return max(preDensity, density * _BrushStrength);
}