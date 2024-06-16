#include "HuwaTexelReadWrite.hlsl"

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
    bool mask : TEXCOORD1;
};

Texture2D _PolygonMaskTex;
float4 _PolygonMaskTex_TexelSize;

float4 _MainColor;
float4 _WireFrameColor;
float4 _Color0;
float4 _Color1;
float _Width;

V2G VertexShaderStage_WireFrame(I2V input)
{
    V2G output = (V2G) 0;
    output.cPos = UnityObjectToClipPos(input.lPos);
    return output;
}

[maxvertexcount(3)]
void GeometryShaderStage_WireFrame(triangle V2G input[3], inout TriangleStream<G2F> stream)
{
    float2 pos0 = input[0].cPos.xy * _ScreenParams.xy / input[0].cPos.w;
    float2 pos1 = input[1].cPos.xy * _ScreenParams.xy / input[1].cPos.w;
    float2 pos2 = input[2].cPos.xy * _ScreenParams.xy / input[2].cPos.w;

    float2 side0 = pos2 - pos1;
    float2 side1 = pos0 - pos2;
    float2 side2 = pos1 - pos0;

    float area = abs(side0.x * side1.y - side0.y * side1.x);

    G2F output = (G2F) 0;

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

float4 FragmentShaderStage_WireFrame(G2F input) : SV_Target
{
    bool flag = (input.distance.x < _Width) || (input.distance.y < _Width) || (input.distance.z < _Width);
    float4 color = flag ? _WireFrameColor : _MainColor;
    clip(color.a - 0.5);
    return color;
}



V2G VertexShaderStage_PolygonMaskResult(I2V input)
{
    V2G output = (V2G) 0;
    output.cPos = UnityObjectToClipPos(input.lPos);
    return output;
}

[maxvertexcount(3)]
void GeometryShaderStage_PolygonMaskResult(triangle V2G input[3], inout TriangleStream<G2F> stream, uint primitiveID : SV_PrimitiveID)
{
    float preData;
    HTRW_TEXEL_READ(_PolygonMaskTex, primitiveID, preData)
    
    float2 pos0 = input[0].cPos.xy * _ScreenParams.xy / input[0].cPos.w;
    float2 pos1 = input[1].cPos.xy * _ScreenParams.xy / input[1].cPos.w;
    float2 pos2 = input[2].cPos.xy * _ScreenParams.xy / input[2].cPos.w;

    float2 side0 = pos2 - pos1;
    float2 side1 = pos0 - pos2;
    float2 side2 = pos1 - pos0;

    float area = abs(side0.x * side1.y - side0.y * side1.x);

    G2F output = (G2F) 0;
    output.mask = preData != 0.0;
    
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

float4 FragmentShaderStage_PolygonMaskResult(G2F input) : SV_Target
{
    bool mask = input.mask;
    float3 mainColor = mask ? _Color0 : _Color1;
    
    bool flag = (input.distance.x < _Width) || (input.distance.y < _Width) || (input.distance.z < _Width);
    float3 color = flag ? _WireFrameColor : mainColor;
    
    return float4(color, 1.0);
}
