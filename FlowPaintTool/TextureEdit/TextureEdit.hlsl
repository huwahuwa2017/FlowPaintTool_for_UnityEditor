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

Texture2D _MainTex;

V2F VertexShaderStage(I2V input)
{
    V2F output = (V2F) 0;
    output.cPos = UnityObjectToClipPos(input.lPos);
    output.uv = input.uv;
    return output;
}

float4 FragmentShaderStage(V2F input) : SV_Target
{
    uint2 index = uint2(input.cPos.xy);
    
    float4 color = _MainTex[index];
    color = 1.0 - color;
    
    return color;
}
