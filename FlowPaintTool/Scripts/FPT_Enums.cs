#if UNITY_EDITOR

namespace FlowPaintTool
{
    public enum FPT_StartTextureLoadModeEnum
    {
        FilePath,
        Assets
    }

    public enum FPT_PaintModeEnum
    {
        FlowPaintMode,
        ColorPaintMode
    }

    public enum FPT_BrushShapeEnum
    {
        Constant,
        Linear,
        Smooth,
        Squared,        // Linear^2
        InverseSquared  // 1 - (1 - Linear)^2
    }

    public enum FPT_LanguageTypeEnum
    {
        None,
        Japanese,
        English
    }
}

#endif