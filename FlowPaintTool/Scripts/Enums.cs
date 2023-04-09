namespace FlowPaintTool
{
    public enum StartMeshLoadMode
    {
        Assets,
        HierarchyGameObject
    }

    public enum StartTextureLoadMode
    {
        Assets,
        FilePath
    }

    public enum PaintMode
    {
        FlowPaintMode,
        ColorPaintMode
    }

    public enum BrushType
    {
        Constant,
        Linear,
        Smooth,
        Squared,
        InverseSquared
    }
}