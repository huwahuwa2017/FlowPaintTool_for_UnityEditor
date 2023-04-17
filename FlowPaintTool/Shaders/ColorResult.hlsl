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

sampler2D _MainTex;

V2F VertexShaderStage(I2V input)
{
    V2F output = (V2F) 0;
    output.cPos = UnityObjectToClipPos(input.lPos);
    output.uv = input.uv;
    return output;
}

float4 FragmentShaderStage(V2F input) : SV_Target
{
    float4 color = tex2D(_MainTex, input.uv);
    
#if (!defined(UNITY_COLORSPACE_GAMMA)) && (!defined(IS_SRGB))
    color.rgb = GammaToLinearSpace(color.rgb);
#endif
    
    return color;
}