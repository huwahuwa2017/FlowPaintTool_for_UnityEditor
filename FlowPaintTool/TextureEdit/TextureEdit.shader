Shader "FlowPaintTool2/TextureEdit"
{
    Properties
    {
        // Input from Blit
        [HideInInspector]
        _MainTex("Main Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM

            #pragma vertex VertexShaderStage
            #pragma fragment FragmentShaderStage

            #include "TextureEdit.hlsl"

            ENDCG
        }
    }
}
