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

SamplerState _linear_clamp_sampler;

Texture2D _MainTex;
Texture2D _FillTex;
Texture2D _PaintTex;
Texture2D _DensityTex;

float4 _MainTex_TexelSize;

static int2 _OffsetArray[4] =
{
    int2(1, 0), int2(-1, 0), int2(0, 1), int2(0, -1)
};

bool IsOutOfRange(int2 range, int2 index)
{
    return index.x < 0 || index.y < 0 || index.x >= range.x || index.y >= range.y;
}



V2F VertexShaderStage(I2V input)
{
    V2F output = (V2F) 0;
    output.cPos = UnityObjectToClipPos(input.lPos);
    output.uv = input.uv;
    return output;
}



float4 FragmentShaderStage_UnpackNormal(V2F input) : SV_Target
{
    float4 data = _MainTex.SampleLevel(_linear_clamp_sampler, input.uv, 0.0);
    float3 normal = UnpackNormal(data);
    
    return float4(normal * 0.5 + 0.5, 1.0);
}

float FragmentShaderStage_FillBleed(V2F input) : SV_Target
{
    uint2 index0 = uint2(input.cPos.xy);
    
    if (_MainTex[index0].r > 0.25)
    {
        return 1.0;
    }
    
    float temp0 = 0.0;
    
    for (int count = 0; count < 4; ++count)
    {
        int2 index1 = int2(index0) + _OffsetArray[count];
        bool isOutOfRange = IsOutOfRange(_MainTex_TexelSize.zw, index1);
        index1 = isOutOfRange ? 0 : index1;
        
        temp0 = max(temp0, isOutOfRange ? 0.0 : _MainTex[index1].r);
    }
    
    return (temp0 > 0.25) ? 0.5 : 0.0;
}

float4 FragmentShaderStage_FlowMerge(V2F input) : SV_Target
{
    uint2 index = uint2(input.cPos.xy);
    
    float4 mColor = _MainTex[index];
    float4 pColor = _PaintTex[index];
    float density = _DensityTex[index].r;
    
    float3 mVector = mColor.rgb * 2.0 - 1.0;
    float3 pVector = pColor.rgb * 2.0 - 1.0;
    
    float3 temp0 = normalize(lerp(mVector, pVector, density));
    return float4(temp0 * 0.5 + 0.5, 1.0);
}

float4 FragmentShaderStage_ColorMerge(V2F input) : SV_Target
{
    uint2 index = uint2(input.cPos.xy);
    
    float4 mColor = _MainTex[index];
    float4 pColor = _PaintTex[index];
    float density = _DensityTex[index].r;
    
    return lerp(mColor, pColor, density);
}

float4 FragmentShaderStage_Bleed(V2F input) : SV_Target
{
    uint2 index0 = uint2(input.cPos.xy);
    float temp0 = _FillTex[index0].r;
    
    if ((temp0 < 0.25) || (temp0 > 0.75))
    {
        return _MainTex[index0];
    }
    
    float4 result = 0.0;
    int margeCount = 0;
    
    for (int count = 0; count < 4; ++count)
    {
        int2 index1 = int2(index0) + _OffsetArray[count];
        bool isOutOfRange = IsOutOfRange(_MainTex_TexelSize.zw, index1);
        index1 = isOutOfRange ? 0 : index1;
        
        bool flag = (!isOutOfRange) && (_FillTex[index1].r > 0.75);
        result += flag ? _MainTex[index1] : 0.0;
        margeCount += flag;
    }
    
    return result / max(margeCount, 1);
}
