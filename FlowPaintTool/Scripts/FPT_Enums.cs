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
        Squared,
        InverseSquared
    }

    public enum FPT_LanguageTypeEnum
    {
        None,
        Japanese,
        English
    }
}

#endif