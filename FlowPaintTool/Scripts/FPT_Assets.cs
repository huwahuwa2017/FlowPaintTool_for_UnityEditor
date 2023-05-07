using UnityEngine;

//[CreateAssetMenu(fileName = "Data", menuName = "FlowPaintTool/FPT_Assets")]
public class FPT_Assets : ScriptableObject
{
    [SerializeField]
    public GameObject _rangeVisualizationPrefab = null;

    [SerializeField]
    public ComputeShader _adjacentPolygonComputeShader = null;

    [SerializeField]
    public Material _fillMaterial = null;
    [SerializeField]
    public Material _fillBleedMaterial = null;

    [SerializeField]
    public Material _flowPaintMaterial = null;
    [SerializeField]
    public Material _colorPaintMaterial = null;
    [SerializeField]
    public Material _densityMaterial = null;
    [SerializeField]
    public Material _flowMergeMaterial = null;
    [SerializeField]
    public Material _colorMergeMaterial = null;
    [SerializeField]
    public Material _cutoutMaterial = null;
    [SerializeField]
    public Material _bleedMaterial = null;
    [SerializeField]
    public Material _flowResultMaterial = null;
    [SerializeField]
    public Material _colorResultMaterial = null;

    [SerializeField]
    public Material _material_MaskOff = null;
    [SerializeField]
    public Material _material_MaskOn = null;
}
