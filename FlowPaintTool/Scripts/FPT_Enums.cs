#if UNITY_EDITOR

namespace FlowPaintTool
{
    public enum FPT_StartTextureLoadModeEnum
    {
        Assets,
        FilePath
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