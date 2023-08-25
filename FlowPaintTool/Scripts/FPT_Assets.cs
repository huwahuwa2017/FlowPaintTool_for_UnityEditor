#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    //[CreateAssetMenu(fileName = "Data", menuName = "FlowPaintTool/FPT_Assets")]
    public class FPT_Assets : ScriptableObject
    {
        private static FPT_Assets _staticInstance = null;

        public static FPT_Assets GetStaticInstance()
        {
            if (_staticInstance == null)
            {
                string path = AssetDatabase.GUIDToAssetPath("0aabfe59318f09f46ad81b9969288ce2");
                _staticInstance = AssetDatabase.LoadAssetAtPath<FPT_Assets>(path);
            }

            return _staticInstance;
        }



        [SerializeField]
        private GameObject _rangeVisualizationPrefab = null;

        [SerializeField]
        private ComputeShader _adjacentPolygonComputeShader = null;

        [SerializeField]
        private Material _unpackNormalMaterial = null;
        [SerializeField]
        private Material _fillMaterial = null;
        [SerializeField]
        private Material _fillBleedMaterial = null;

        [SerializeField]
        private Material _flowPaintMaterial = null;
        [SerializeField]
        private Material _colorPaintMaterial = null;
        [SerializeField]
        private Material _densityMaterial = null;
        [SerializeField]
        private Material _flowMergeMaterial = null;
        [SerializeField]
        private Material _colorMergeMaterial = null;
        [SerializeField]
        private Material _cutoutMaterial = null;
        [SerializeField]
        private Material _bleedMaterial = null;
        [SerializeField]
        private Material _flowResultMaterial = null;
        [SerializeField]
        private Material _colorResultMaterial = null;

        [SerializeField]
        private Material _material_MaskOff = null;
        [SerializeField]
        private Material _material_MaskOn = null;

        public GameObject GetRangeVisualizationPrefab() => _rangeVisualizationPrefab;

        public ComputeShader GetAdjacentPolygonComputeShader() => _adjacentPolygonComputeShader;

        public Material GetUnpackNormalMaterial() => _unpackNormalMaterial;
        public Material GetFillMaterial() => _fillMaterial;
        public Material GetFillBleedMaterial() => _fillBleedMaterial;

        public Material GetFlowPaintMaterial() => _flowPaintMaterial;
        public Material GetColorPaintMaterial() => _colorPaintMaterial;
        public Material GetDensityMaterial() => _densityMaterial;
        public Material GetFlowMergeMaterial() => _flowMergeMaterial;
        public Material GetColorMergeMaterial() => _colorMergeMaterial;
        public Material GetCutoutMaterial() => _cutoutMaterial;
        public Material GetBleedMaterial() => _bleedMaterial;
        public Material GetFlowResultMaterial() => _flowResultMaterial;
        public Material GetColorResultMaterial() => _colorResultMaterial;

        public Material GetMaterial_MaskOff() => _material_MaskOff;
        public Material GetMaterial_MaskOn() => _material_MaskOn;
    }
}

#endif