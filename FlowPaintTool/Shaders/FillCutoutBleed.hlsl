#include "UnityCG.cginc"
#include "TargetUVChannel.hlsl"

struct I2V
{
    float4 lPos : POSITION;
    float2 uv : TARGET_UV_CHANNEL;
};

struct V2F
{
    float4 cPos : SV_POSITION;
};

Texture2D _FillTex;
Texture2D _MainTex;
float4 _MainTex_TexelSize;

static int2 _OffsetArray[4] =
{
    int2(1, 0), int2(-1, 0), int2(0, 1), int2(0, -1)
};

uint2 IsOutOfRange(in float2 range, in int2 index, out bool outOfRange)
{
    outOfRange = index.x < 0 || index.y < 0 || index.x >= range.x || index.y >= range.y;
    return uint2(index) * (!outOfRange);
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



V2F VertexShaderStage_FillBleed(I2V input)
{
    V2F output = (V2F) 0;
    output.cPos = UnityObjectToClipPos(input.lPos);
    return output;
}

float4 FragmentShaderStage_FillBleed(V2F input) : SV_Target
{
    uint2 index0 = uint2(input.cPos.xy);
    
    if (_MainTex[index0].r != 0.0)
    {
        return 1.0;
    }
    
    float result = 0.0;
    
    for (int count = 0; count < 4; ++count)
    {
        bool isOutOfRange = false;
        uint2 index1 = IsOutOfRange(_MainTex_TexelSize.zw, int2(index0) + _OffsetArray[count], isOutOfRange);
        result += _MainTex[index1].r * (!isOutOfRange);
    }
    
    return result != 0.0;
}



V2F VertexShaderStage_Cutout(I2V input)
{
    V2F output = (V2F) 0;
    output.cPos = UnityObjectToClipPos(input.lPos);
    return output;
}

float4 FragmentShaderStage_Cutout(V2F input) : SV_Target
{
    uint2 index = uint2(input.cPos.xy);
    
    float4 color = _MainTex[index];
    bool flag = _FillTex[index].r > 0.5;
    
    return lerp(0.0, color, flag);
}



V2F VertexShaderStage_Bleed(I2V input)
{
    V2F output = (V2F) 0;
    output.cPos = UnityObjectToClipPos(input.lPos);
    return output;
}

float4 FragmentShaderStage_Bleed(V2F input) : SV_Target
{
    uint2 index0 = uint2(input.cPos.xy);
    
    if (_FillTex[index0].r != 0.0)
    {
        return _MainTex[index0];
    }
    
    float4 result = 0.0;
    int margeCount = 0;
    
    for (int count = 0; count < 4; ++count)
    {
        bool isOutOfRange = false;
        uint2 index1 = IsOutOfRange(_MainTex_TexelSize.zw, int2(index0) + _OffsetArray[count], isOutOfRange);
        
        bool flag = (_FillTex[index1].r != 0.0) && (!isOutOfRange);
        result += _MainTex[index1] * flag;
        margeCount += flag;
    }
    
    return result / max(margeCount, 1);
}
