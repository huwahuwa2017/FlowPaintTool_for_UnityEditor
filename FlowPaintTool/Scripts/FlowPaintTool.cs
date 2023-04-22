using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace FlowPaintTool
{
    public class FlowPaintTool : MonoBehaviour
    {
        private static readonly int _mainTexSPID = Shader.PropertyToID("_MainTex");
        private static readonly int _fillTexSPID = Shader.PropertyToID("_FillTex");

        private static readonly int _modelMatrixSPID = Shader.PropertyToID("_ModelMatrix");
        private static readonly int _inverseModelMatrixSPID = Shader.PropertyToID("_InverseModelMatrix");
        private static readonly int _hitPositionSPID = Shader.PropertyToID("_HitPosition");
        private static readonly int _paintDirectionSPID = Shader.PropertyToID("_PaintDirection");

        private static readonly int _brushTypeSPID = Shader.PropertyToID("_BrushType");
        private static readonly int _brushSizeSPID = Shader.PropertyToID("_BrushSize");
        private static readonly int _brushStrengthSPID = Shader.PropertyToID("_BrushStrength");

        private static readonly int _fixedHeightSPID = Shader.PropertyToID("_FixedHeight");
        private static readonly int _fixedHeightMinSPID = Shader.PropertyToID("_FixedHeightMin");
        private static readonly int _fixedHeightMaxSPID = Shader.PropertyToID("_FixedHeightMax");

        private static readonly int _PaintColorSPID = Shader.PropertyToID("_PaintColor");
        private static readonly int _EditRGBASPID = Shader.PropertyToID("_EditRGBA");

        private static float _brushSize = 0.1f;
        private static float _brushStrength = 0.5f;

        private static float _displayNormalLength = 0.02f;
        private static float _displayNormalAmount = 64f;

        public static FlowPaintTool _activeInstance = null;

        public static FlowPaintTool ActiveInstance
        {
            get
            {
                if (_activeInstance != null && _activeInstance._selected)
                {
                    return _activeInstance;
                }

                return null;
            }
        }

        public static bool EnableMaskMode { get; set; } = false;
        public static bool EnableMaterialView { get; set; } = false;

        public static float BrushSize
        {
            get => _brushSize;

            set => _brushSize = Math.Max(value, 0f);
        }

        public static float BrushStrength
        {
            get => _brushStrength;

            set => _brushStrength = Mathf.Clamp01(value);
        }

        public static BrushTypeEnum BrushType { get; set; } = BrushTypeEnum.Smooth;

        //UI未実装　0.01固定
        public static float BrushMoveSensitivity { get; set; } = 0.01f;

        public static bool FixedHeight { get; set; } = false;
        public static float FixedHeightMin { get; set; } = 0.5f;
        public static float FixedHeightMax { get; set; } = 1f;
        public static bool FixedDirection { get; set; } = false;
        public static Vector3 FixedDirectionVector { get; set; } = Vector3.down;

        public static float DisplayNormalLength
        {
            get => _displayNormalLength;

            set => _displayNormalLength = Math.Max(value, 0f);
        }

        public static float DisplayNormalAmount
        {
            get => _displayNormalAmount;

            set => _displayNormalAmount = Math.Max(value, 0f);
        }

        public static Color PaintColor { get; set; } = Color.white;
        public static bool EditR { get; set; } = true;
        public static bool EditG { get; set; } = true;
        public static bool EditB { get; set; } = true;
        public static bool EditA { get; set; } = true;



        [SerializeField]
        private Material _fillMaterial = null;
        [SerializeField]
        private Material _fillBleedMaterial = null;

        [SerializeField]
        private Material _flowPaintMaterial = null;
        [SerializeField]
        private Material _colorPaintMaterial = null;
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

        [SerializeField]
        private ComputeShader _cs_adjacentPolygon = null;

        private FlowPaintToolData _fptData = default;

        private Mesh _paintModeMesh = null;
        private Mesh _maskModeMesh = null;

        private int _polygonCount = 0;

        private Vector3Int[] _pd_VertexIndexArray = null;
        private int[] _pd_SubMeshIndexArray = null;
        private Vector3Int[] _pd_AdjacentIndexArray = null;
        private bool[] _pd_DuplicateUVArray = null;
        private List<int[]> _duplicatePolygonIndexArrayList = new List<int[]>();
        private bool[] _pd_MaskArray = null;
        private bool[] _pd_MaskResultArray = null;
        private Vector3[] _pd_CenterArray = null;

        private string _outputRenderTexturePath = string.Empty;
        private RenderTexture _outputRenderTexture = null;
        private RenderTexture[] _fillTextureArray = null;

        private Material _copyFlowPaintMaterial = null;
        private Material _copyColorPaintMaterial = null;
        private Material _copyCutoutMaterial = null;
        private Material[] _copyBleedMaterialArray = null;
        private Material _copyFlowResultMaterial = null;
        private Material _copyColorResultMaterial = null;

        private CommandBuffer _bleedCommandBuffer = null;
        private CommandBuffer _flowPaintCommandBuffer = null;
        private CommandBuffer _colorPaintCommandBuffer = null;

        private GameObject _flowPaintRender = null;
        private GameObject _colorPaintRender = null;
        private GameObject _maskRender = null;
        private GameObject _meshColider = null;

        private bool _selected = false;
        private Matrix4x4 _preMatrix = Matrix4x4.zero;
        private Vector3 _preHitPosition = Vector3.zero;
        private bool _preHit = false;

        public void SetData(FlowPaintToolData fptData)
        {
            _fptData = fptData;
        }

        private void GenerateMesh()
        {
            _maskModeMesh = Instantiate(_fptData._startMesh);
            _maskModeMesh.MarkDynamic();
            _maskModeMesh.triangles = new int[0];
            _maskModeMesh.subMeshCount = 2;

            _paintModeMesh = Instantiate(_maskModeMesh);
            _paintModeMesh.subMeshCount = 1;
        }

        private void GeneratePolygonList()
        {
            Vector3[] vertices = _fptData._startMesh.vertices;
            List<Vector2> uvs = new List<Vector2>();
            _fptData._startMesh.GetUVs(_fptData._targetUVChannel, uvs);
            int[] triangles = _fptData._startMesh.triangles;
            _polygonCount = triangles.Length / 3;

            _pd_VertexIndexArray = new Vector3Int[_polygonCount];
            _pd_SubMeshIndexArray = new int[_polygonCount];
            _pd_DuplicateUVArray = new bool[_polygonCount];
            _pd_MaskArray = new bool[_polygonCount];
            _pd_MaskResultArray = new bool[_polygonCount];
            _pd_CenterArray = new Vector3[_polygonCount];

            // Generate _polygonList Start
            int subMeshCount = _fptData._startMesh.subMeshCount;
            int triangleIndex = 0;

            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; ++subMeshIndex)
            {
                int[] subMeshTriangles = _fptData._startMesh.GetTriangles(subMeshIndex);
                int subMeshTriangleArrayLength = subMeshTriangles.Length;

                for (int index = 0; index < subMeshTriangleArrayLength; index += 3)
                {
                    Vector3Int vertexIndex = new Vector3Int(subMeshTriangles[index], subMeshTriangles[index + 1], subMeshTriangles[index + 2]);
                    _pd_VertexIndexArray[triangleIndex] = vertexIndex;
                    _pd_SubMeshIndexArray[triangleIndex] = subMeshIndex;
                    ++triangleIndex;
                }
            }
            // Generate polygon list End

            // Compute shader Start
            ComputeBuffer cb_Vertices = new ComputeBuffer(vertices.Count(), Marshal.SizeOf(typeof(Vector3)));
            ComputeBuffer cb_UVs = new ComputeBuffer(uvs.Count(), Marshal.SizeOf(typeof(Vector2)));
            ComputeBuffer cb_Triangles = new ComputeBuffer(triangles.Count(), Marshal.SizeOf(typeof(int)));
            ComputeBuffer cb_AdjacentResult = new ComputeBuffer(_polygonCount, Marshal.SizeOf(typeof(Vector3Int)));
            ComputeBuffer cb_CenterUVResult = new ComputeBuffer(_polygonCount, Marshal.SizeOf(typeof(Vector2)));
            ComputeBuffer cb_DuplicateResult = new ComputeBuffer(_polygonCount, Marshal.SizeOf(typeof(int)));

            cb_Vertices.SetData(vertices);
            cb_UVs.SetData(uvs);
            cb_Triangles.SetData(triangles);

            int adjacent_Main_KI = _cs_adjacentPolygon.FindKernel("Adjacent_Main");
            int duplicate_Main_KI = _cs_adjacentPolygon.FindKernel("Duplicate_Main");

            _cs_adjacentPolygon.SetInt("_TriangleCount", _polygonCount);
            _cs_adjacentPolygon.SetFloat("_Epsilon", _fptData._uv_Epsilon);

            _cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_Vertices", cb_Vertices);
            _cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_UVs", cb_UVs);
            _cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_Triangles", cb_Triangles);
            _cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_AdjacentResult", cb_AdjacentResult);
            _cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_CenterUVResult", cb_CenterUVResult);
            _cs_adjacentPolygon.Dispatch(adjacent_Main_KI, _polygonCount, 1, 1);

            _pd_AdjacentIndexArray = new Vector3Int[_polygonCount];
            cb_AdjacentResult.GetData(_pd_AdjacentIndexArray);

            _cs_adjacentPolygon.SetBuffer(duplicate_Main_KI, "_CenterUVResult", cb_CenterUVResult);
            _cs_adjacentPolygon.SetBuffer(duplicate_Main_KI, "_DuplicateResult", cb_DuplicateResult);
            _cs_adjacentPolygon.Dispatch(duplicate_Main_KI, _polygonCount, 1, 1);

            int[] duplicateResult = new int[_polygonCount];
            cb_DuplicateResult.GetData(duplicateResult);

            cb_Vertices.Release();
            cb_UVs.Release();
            cb_Triangles.Release();
            cb_AdjacentResult.Release();
            cb_CenterUVResult.Release();
            cb_DuplicateResult.Release();
            // Compute shader End

            // Generate _duplicatePolygonListList Start
            bool[] checkIndex = new bool[_polygonCount];

            for (int startIndex = 0; startIndex < _polygonCount; ++startIndex)
            {
                int duplicateIndex = duplicateResult[startIndex];

                if (duplicateIndex == -1 || checkIndex[duplicateIndex]) continue;

                checkIndex[duplicateIndex] = true;
                List<int> duplicatePolygonIndexList = new List<int>();

                for (int index = startIndex; index < _polygonCount; ++index)
                {
                    if (duplicateResult[index] != duplicateIndex) continue;

                    _pd_DuplicateUVArray[index] = true;
                    duplicatePolygonIndexList.Add(index);
                }

                _pd_DuplicateUVArray[duplicateIndex] = true;
                duplicatePolygonIndexList.Add(duplicateIndex);
                _duplicatePolygonIndexArrayList.Add(duplicatePolygonIndexList.ToArray());
            }
            // Generate _duplicatePolygonListList End
        }

        private void GenerateOutputRenderTexture()
        {
            GraphicsFormat graphicsFormat = _fptData._actualSRGB ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
            RenderTextureDescriptor rtd = new RenderTextureDescriptor(_fptData._outputTextureResolution.x, _fptData._outputTextureResolution.y, graphicsFormat, 0);
            _outputRenderTexture = new RenderTexture(rtd);
            _outputRenderTexture.filterMode = FilterMode.Point;

            string path = AssetDatabase.GetAssetPath(_fillMaterial);
            path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(path)), $"RT{GetInstanceID()}.renderTexture");
            AssetDatabase.CreateAsset(_outputRenderTexture, path);
            _outputRenderTexturePath = path;

            if (_fptData._textureExist)
            {
                if (_fptData._startTextureLoadMode == StartTextureLoadModeEnum.Assets)
                {
                    Graphics.Blit(_fptData._startTexture, _outputRenderTexture);
                }
                else if (_fptData._startTextureLoadMode == StartTextureLoadModeEnum.FilePath)
                {
                    GraphicsFormat temp0 = _fptData._actualSRGB ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                    Texture2D texture = new Texture2D(0, 0, temp0, TextureCreationFlags.None);
                    texture.LoadImage(File.ReadAllBytes(_fptData._startTextureFilePath));
                    Graphics.Blit(texture, _outputRenderTexture);
                    Destroy(texture);
                }
            }
            else
            {
                Debug.Log("Texture not found");

                Texture2D defaultColorTexture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
                defaultColorTexture.SetPixel(0, 0, new Color(0.5f, 0.5f, 1f, 1f));
                defaultColorTexture.Apply();
                Graphics.Blit(defaultColorTexture, _outputRenderTexture);
                Destroy(defaultColorTexture);
            }
        }

        private void TargetUVChannel(Material mat)
        {
            mat.DisableKeyword("UV_CHANNEL_0");
            mat.DisableKeyword("UV_CHANNEL_1");
            mat.DisableKeyword("UV_CHANNEL_2");
            mat.DisableKeyword("UV_CHANNEL_3");
            mat.DisableKeyword("UV_CHANNEL_4");
            mat.DisableKeyword("UV_CHANNEL_5");
            mat.DisableKeyword("UV_CHANNEL_6");
            mat.DisableKeyword("UV_CHANNEL_7");

            mat.EnableKeyword("UV_CHANNEL_" + _fptData._targetUVChannel);
        }

        private void GenerateCommandBuffer()
        {
            int[] tempTexSPIDs = new int[] { Shader.PropertyToID("_TempTex0"), Shader.PropertyToID("_TempTex1") };

            RenderTextureDescriptor rtd_main = _outputRenderTexture.descriptor;
            RenderTextureDescriptor rtd_R8 = rtd_main;
            rtd_R8.graphicsFormat = GraphicsFormat.R8_UNorm;

            // Texture Generate Start
            TargetUVChannel(_fillMaterial);
            TargetUVChannel(_fillBleedMaterial);

            _fillTextureArray = new RenderTexture[Math.Max(_fptData._bleedRange, 1)];
            _fillTextureArray[0] = new RenderTexture(rtd_R8);

            CommandBuffer fillCommandBuffer = new CommandBuffer();
            fillCommandBuffer.SetRenderTarget(_fillTextureArray[0]);

            int subMeshCount = _fptData._startMesh.subMeshCount;

            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; ++subMeshIndex)
            {
                fillCommandBuffer.DrawMesh(_fptData._startMesh, Matrix4x4.identity, _fillMaterial, subMeshIndex);
            }

            Graphics.ExecuteCommandBuffer(fillCommandBuffer);

            for (int index = 1; index < _fptData._bleedRange; ++index)
            {
                _fillTextureArray[index] = new RenderTexture(rtd_R8);
                Graphics.Blit(_fillTextureArray[index - 1], _fillTextureArray[index], _fillBleedMaterial);
            }
            // Texture Generate End

            // Material Generate And Setting Start
            _copyBleedMaterialArray = new Material[_fptData._bleedRange];

            for (int index = 0; index < _fptData._bleedRange; ++index)
            {
                Material copyBleedMaterial = Instantiate(_bleedMaterial);
                TargetUVChannel(copyBleedMaterial);
                _copyBleedMaterialArray[index] = copyBleedMaterial;
            }

            _copyFlowPaintMaterial = Instantiate(_flowPaintMaterial);
            _copyColorPaintMaterial = Instantiate(_colorPaintMaterial);
            _copyCutoutMaterial = Instantiate(_cutoutMaterial);
            _copyFlowResultMaterial = Instantiate(_flowResultMaterial);
            _copyColorResultMaterial = Instantiate(_colorResultMaterial);

            TargetUVChannel(_copyFlowPaintMaterial);
            TargetUVChannel(_copyColorPaintMaterial);
            TargetUVChannel(_copyCutoutMaterial);
            TargetUVChannel(_copyFlowResultMaterial);
            TargetUVChannel(_copyColorResultMaterial);

            if (_fptData._actualSRGB)
            {
                _copyFlowResultMaterial.EnableKeyword("IS_SRGB");
                _copyColorResultMaterial.EnableKeyword("IS_SRGB");
            }
            else
            {
                _copyFlowResultMaterial.DisableKeyword("IS_SRGB");
                _copyColorResultMaterial.DisableKeyword("IS_SRGB");
            }
            // Material Generate And Setting End

            // _bleedCommandBuffer Generate Start
            _bleedCommandBuffer = new CommandBuffer();
            _bleedCommandBuffer.GetTemporaryRT(tempTexSPIDs[0], rtd_main);
            _bleedCommandBuffer.GetTemporaryRT(tempTexSPIDs[1], rtd_main);
            _bleedCommandBuffer.Blit(_outputRenderTexture, tempTexSPIDs[0], _copyCutoutMaterial);

            int temp1 = 0;

            for (int index = 0; index < _fptData._bleedRange; ++index)
            {
                temp1 = 1 - temp1;
                _bleedCommandBuffer.Blit(tempTexSPIDs[1 - temp1], tempTexSPIDs[temp1], _copyBleedMaterialArray[index]);
            }

            _bleedCommandBuffer.Blit(tempTexSPIDs[temp1], _outputRenderTexture);
            _bleedCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[0]);
            _bleedCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[1]);
            // _bleedCommandBuffer Generate End

            // _flowPaintCommandBuffer Generate Start
            _flowPaintCommandBuffer = new CommandBuffer();
            _flowPaintCommandBuffer.GetTemporaryRT(tempTexSPIDs[0], rtd_main);
            _flowPaintCommandBuffer.Blit(_outputRenderTexture, tempTexSPIDs[0]);
            _flowPaintCommandBuffer.SetRenderTarget(tempTexSPIDs[0]);
            _flowPaintCommandBuffer.DrawMesh(_paintModeMesh, Matrix4x4.identity, _copyFlowPaintMaterial, 0);
            _flowPaintCommandBuffer.Blit(tempTexSPIDs[0], _outputRenderTexture);
            _flowPaintCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[0]);
            // _flowPaintCommandBuffer Generate End

            // _colorPaintCommandBuffer Generate Start
            _colorPaintCommandBuffer = new CommandBuffer();
            _colorPaintCommandBuffer.GetTemporaryRT(tempTexSPIDs[0], rtd_main);
            _colorPaintCommandBuffer.Blit(_outputRenderTexture, tempTexSPIDs[0]);
            _colorPaintCommandBuffer.SetRenderTarget(tempTexSPIDs[0]);
            _colorPaintCommandBuffer.DrawMesh(_paintModeMesh, Matrix4x4.identity, _copyColorPaintMaterial, 0);
            _colorPaintCommandBuffer.Blit(tempTexSPIDs[0], _outputRenderTexture);
            _colorPaintCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[0]);
            // _colorPaintCommandBuffer Generate End
        }

        private void GenerateGameObject()
        {
            int subMeshCount = _fptData._startMesh.subMeshCount;

            Material[] flowResultMaterials = Enumerable.Repeat(_copyFlowResultMaterial, subMeshCount).ToArray();
            Material[] colorResultMaterials = Enumerable.Repeat(_copyColorResultMaterial, subMeshCount).ToArray();
            Material[] maskResultMaterials = new Material[] { _material_MaskOff, _material_MaskOn };

            _flowPaintRender = new GameObject("FlowPaintRender");
            _flowPaintRender.transform.SetParent(transform, false);
            _flowPaintRender.AddComponent<MeshFilter>().sharedMesh = _fptData._startMesh;
            _flowPaintRender.AddComponent<MeshRenderer>().sharedMaterials = flowResultMaterials;

            _colorPaintRender = new GameObject("ColorPaintRender");
            _colorPaintRender.transform.SetParent(transform, false);
            _colorPaintRender.AddComponent<MeshFilter>().sharedMesh = _fptData._startMesh;
            _colorPaintRender.AddComponent<MeshRenderer>().sharedMaterials = colorResultMaterials;

            _maskRender = new GameObject("MaskRender");
            _maskRender.transform.SetParent(transform, false);
            _maskRender.AddComponent<MeshFilter>().sharedMesh = _maskModeMesh;
            _maskRender.AddComponent<MeshRenderer>().sharedMaterials = maskResultMaterials;

            _meshColider = new GameObject("MeshColider");
            _meshColider.transform.SetParent(transform, false);
            _meshColider.AddComponent<MeshCollider>().sharedMesh = _fptData._startMesh;
        }

        private void MaskModeMeshTriangleUpdate()
        {
            List<int> triangleList0 = new List<int>(_polygonCount);
            List<int> triangleList1 = new List<int>(_polygonCount);

            for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
            {
                Vector3Int vIndex = _pd_VertexIndexArray[pIndex];

                if (_pd_MaskArray[pIndex])
                {
                    triangleList1.Add(vIndex.x);
                    triangleList1.Add(vIndex.y);
                    triangleList1.Add(vIndex.z);
                }
                else
                {
                    triangleList0.Add(vIndex.x);
                    triangleList0.Add(vIndex.y);
                    triangleList0.Add(vIndex.z);
                }
            }

            _maskModeMesh.SetTriangles(triangleList0, 0);
            _maskModeMesh.SetTriangles(triangleList1, 1);
        }

        private void RemoveOutputRenderTexture(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= RemoveOutputRenderTexture;

                AssetDatabase.DeleteAsset(_outputRenderTexturePath);
            }
        }

        private void Start()
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                if (_fptData._actualSRGB)
                {
                    Debug.Log("sRGB enabled");
                }
                else
                {
                    Debug.Log("sRGB disabled");
                }
            }

            EditorApplication.playModeStateChanged += RemoveOutputRenderTexture;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            GenerateMesh();
            GeneratePolygonList();
            GenerateOutputRenderTexture();
            GenerateCommandBuffer();
            GenerateGameObject();

            sw.Stop();
            Debug.Log("Start calculation time : " + sw.Elapsed);

            MaskModeMeshTriangleUpdate();
        }

        public ConcurrentBag<int> GetAllConnectedTriangles(IEnumerable<int> triangleIndexs)
        {
            bool[] connectedTriangles = new bool[_polygonCount];
            ConcurrentBag<int> queue = new ConcurrentBag<int>(triangleIndexs);
            ConcurrentBag<int> adjacentTriangles = new ConcurrentBag<int>();

            while (queue.Count > 0)
            {
                List<int> temp10 = queue.ToList();
                queue = new ConcurrentBag<int>();

                Parallel.ForEach(temp10, currentTriangleIndex =>
                {
                    if (currentTriangleIndex == -1 || connectedTriangles[currentTriangleIndex]) return;

                    connectedTriangles[currentTriangleIndex] = true;
                    adjacentTriangles.Add(currentTriangleIndex);

                    Vector3Int temp30 = _pd_AdjacentIndexArray[currentTriangleIndex];
                    queue.Add(temp30.x);
                    queue.Add(temp30.y);
                    queue.Add(temp30.z);
                });
            }

            return adjacentTriangles;
        }

        public void SelectLinkedPlus()
        {
            IEnumerable<int> temp0 = Enumerable.Range(0, _polygonCount).Where(I => !_pd_MaskArray[I]);
            ConcurrentBag<int> temp1 = GetAllConnectedTriangles(temp0);

            foreach (int temp2 in temp1)
            {
                _pd_MaskArray[temp2] = false;
            }

            MaskModeMeshTriangleUpdate();
        }

        public void SelectLinkedMinus()
        {
            IEnumerable<int> temp0 = Enumerable.Range(0, _polygonCount).Where(I => _pd_MaskArray[I]);
            ConcurrentBag<int> temp1 = GetAllConnectedTriangles(temp0);

            foreach (int temp2 in temp1)
            {
                _pd_MaskArray[temp2] = true;
            }

            MaskModeMeshTriangleUpdate();
        }

        private void Update()
        {
            _selected = Selection.activeTransform == transform;

            if (_selected)
            {
                _activeInstance = this;
            }
        }

        private void SetTexture()
        {
            for (int index = 0; index < _fptData._bleedRange; ++index)
            {
                _copyBleedMaterialArray[index].SetTexture(_fillTexSPID, _fillTextureArray[index]);
            }

            _copyFlowPaintMaterial.SetTexture(_mainTexSPID, _outputRenderTexture);
            _copyColorPaintMaterial.SetTexture(_mainTexSPID, _outputRenderTexture);
            _copyCutoutMaterial.SetTexture(_fillTexSPID, _fillTextureArray[0]);
            _copyFlowResultMaterial.SetTexture(_mainTexSPID, _outputRenderTexture);
            _copyColorResultMaterial.SetTexture(_mainTexSPID, _outputRenderTexture);
        }

        private void PaintModeMeshTriangleUpdate(Vector3 hitPosition)
        {
            for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
            {
                _pd_MaskResultArray[pIndex] = _pd_DuplicateUVArray[pIndex] || _pd_MaskArray[pIndex];
            }

            foreach (int[] duplicatePolygonList in _duplicatePolygonIndexArrayList)
            {
                float minSqrDistance = float.MaxValue;
                int targetPolygonIndex = -1;

                for (int index = 0; index < duplicatePolygonList.Length; index++)
                {
                    int pIndex = duplicatePolygonList[index];

                    if (_pd_MaskArray[pIndex]) continue;

                    float sqrDistance = (hitPosition - _pd_CenterArray[pIndex]).sqrMagnitude;

                    if (sqrDistance < minSqrDistance)
                    {
                        minSqrDistance = sqrDistance;
                        targetPolygonIndex = pIndex;
                    }
                }

                if (targetPolygonIndex != -1)
                {
                    _pd_MaskResultArray[targetPolygonIndex] = false;
                }
            }

            List<int> triangleList0 = new List<int>(_polygonCount);

            for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
            {
                if (_pd_MaskResultArray[pIndex]) continue;

                Vector3Int vIndex = _pd_VertexIndexArray[pIndex];
                triangleList0.Add(vIndex.x);
                triangleList0.Add(vIndex.y);
                triangleList0.Add(vIndex.z);
            }

            _paintModeMesh.SetTriangles(triangleList0, 0);
        }

        private void FixedUpdate_FlowPaint(bool hit, RaycastHit raycastHit)
        {
            _flowPaintRender.SetActive(!EnableMaterialView);
            _colorPaintRender.SetActive(false);
            _maskRender.SetActive(false);
            _fptData._sorceRenderer.enabled = EnableMaterialView;

            bool leftClick = Input.GetMouseButton(0);
            bool rightClick = Input.GetMouseButton(1);
            bool click = leftClick || rightClick;

            Vector3 hitPosition = raycastHit.point;

            if (!(_preHit && hit && click))
            {
                _preHitPosition = hitPosition;
            }

            Vector3 paintDirection = hitPosition - _preHitPosition;
            float distance = (hitPosition - Camera.main.transform.position).magnitude;

            if (paintDirection.magnitude > distance * BrushMoveSensitivity)
            {
                PaintModeMeshTriangleUpdate(_preHitPosition);

                Matrix4x4 temp0 = transform.localToWorldMatrix;
                paintDirection = FixedDirection ? FixedDirectionVector : paintDirection;

                _copyFlowPaintMaterial.SetMatrix(_modelMatrixSPID, temp0);
                _copyFlowPaintMaterial.SetVector(_hitPositionSPID, _preHitPosition);
                _copyFlowPaintMaterial.SetInt(_brushTypeSPID, (int)BrushType);
                _copyFlowPaintMaterial.SetFloat(_brushSizeSPID, BrushSize);
                _copyFlowPaintMaterial.SetFloat(_brushStrengthSPID, BrushStrength);
                _copyFlowPaintMaterial.SetMatrix(_inverseModelMatrixSPID, Matrix4x4.Inverse(temp0));
                _copyFlowPaintMaterial.SetVector(_paintDirectionSPID, paintDirection);
                _copyFlowPaintMaterial.SetInt(_fixedHeightSPID, Convert.ToInt32(FixedHeight));
                _copyFlowPaintMaterial.SetFloat(_fixedHeightMinSPID, FixedHeightMin);
                _copyFlowPaintMaterial.SetFloat(_fixedHeightMaxSPID, FixedHeightMax);

                Graphics.ExecuteCommandBuffer(_flowPaintCommandBuffer);

                _preHitPosition = hitPosition;
            }
            else
            {
                Graphics.ExecuteCommandBuffer(_bleedCommandBuffer);
            }

            _copyFlowResultMaterial.SetFloat("_DisplayNormalAmount", DisplayNormalAmount);
            _copyFlowResultMaterial.SetFloat("_DisplayNormalLength", DisplayNormalLength);
        }

        private void FixedUpdate_ColorPaint(bool hit, RaycastHit raycastHit)
        {
            _flowPaintRender.SetActive(false);
            _colorPaintRender.SetActive(!EnableMaterialView);
            _maskRender.SetActive(false);
            _fptData._sorceRenderer.enabled = EnableMaterialView;

            bool leftClick = Input.GetMouseButton(0);
            bool rightClick = Input.GetMouseButton(1);
            bool click = leftClick || rightClick;

            Vector3 hitPosition = raycastHit.point;

            if (!(_preHit && hit && click))
            {
                _preHitPosition = hitPosition;
            }

            Vector3 paintDirection = hitPosition - _preHitPosition;
            float distance = (hitPosition - Camera.main.transform.position).magnitude;

            if (paintDirection.magnitude > distance * BrushMoveSensitivity)
            {
                PaintModeMeshTriangleUpdate(_preHitPosition);

                Matrix4x4 temp0 = transform.localToWorldMatrix;
                int temp1 = (EditR ? 1 : 0) + (EditG ? 2 : 0) + (EditB ? 4 : 0) + (EditA ? 8 : 0);

                _copyColorPaintMaterial.SetMatrix(_modelMatrixSPID, temp0);
                _copyColorPaintMaterial.SetVector(_hitPositionSPID, _preHitPosition);
                _copyColorPaintMaterial.SetInt(_brushTypeSPID, (int)BrushType);
                _copyColorPaintMaterial.SetFloat(_brushSizeSPID, BrushSize);
                _copyColorPaintMaterial.SetFloat(_brushStrengthSPID, BrushStrength);
                _copyColorPaintMaterial.SetColor(_PaintColorSPID, PaintColor);
                _copyColorPaintMaterial.SetInt(_EditRGBASPID, temp1);

                Graphics.ExecuteCommandBuffer(_colorPaintCommandBuffer);

                _preHitPosition = hitPosition;
            }
            else
            {
                Graphics.ExecuteCommandBuffer(_bleedCommandBuffer);
            }
        }

        private void FixedUpdate_Mask(bool hit, RaycastHit raycastHit)
        {
            _flowPaintRender.SetActive(false);
            _colorPaintRender.SetActive(false);
            _maskRender.SetActive(true);
            _fptData._sorceRenderer.enabled = false;

            if (!hit) return;

            bool leftClick = Input.GetMouseButton(0);
            bool rightClick = Input.GetMouseButton(1);

            Vector3 hitPosition = raycastHit.point;

            float sqrRange = BrushSize * BrushSize;

            if (leftClick || rightClick)
            {
                for (int index = 0; index < _polygonCount; index++)
                {
                    if ((hitPosition - _pd_CenterArray[index]).sqrMagnitude < sqrRange)
                    {
                        _pd_MaskArray[index] = rightClick;
                    }
                }

                MaskModeMeshTriangleUpdate();
            }
        }

        private void FixedUpdate()
        {
            // CenterRecalculation Start
            Matrix4x4 matrix = transform.localToWorldMatrix;

            if (_preMatrix != matrix)
            {
                _preMatrix = matrix;

                Vector3[] vpArray = _fptData._startMesh.vertices;
                int maxIndex = vpArray.Length;

                for (int index = 0; index < maxIndex; ++index)
                {
                    vpArray[index] = matrix.MultiplyPoint3x4(vpArray[index]);
                }

                for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
                {
                    Vector3Int vIndex = _pd_VertexIndexArray[pIndex];

                    _pd_CenterArray[pIndex] = (vpArray[vIndex.x] + vpArray[vIndex.y] + vpArray[vIndex.z]) / 3f;
                }
            }
            // CenterRecalculation End

            SetTexture();

            if (!_selected)
            {
                _flowPaintRender.SetActive(false);
                _colorPaintRender.SetActive(false);
                _maskRender.SetActive(false);

                if (ActiveInstance == null)
                {
                    _fptData._sorceRenderer.enabled = true;
                }

                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool hit = Physics.Raycast(ray, out RaycastHit raycastHit, 100f);

            if (EnableMaskMode)
            {
                FixedUpdate_Mask(hit, raycastHit);
            }
            else if (_fptData._paintMode == PaintModeEnum.FlowPaintMode)
            {
                FixedUpdate_FlowPaint(hit, raycastHit);
            }
            else if (_fptData._paintMode == PaintModeEnum.ColorPaintMode)
            {
                FixedUpdate_ColorPaint(hit, raycastHit);
            }

            _preHit = hit;
        }

        [ContextMenu("Parameter Reset")]
        public void ParameterReset()
        {
            EnableMaskMode = false;
            EnableMaterialView = false;
            BrushSize = 0.1f;
            BrushStrength = 0.5f;
            BrushType = BrushTypeEnum.Smooth;
            BrushMoveSensitivity = 0.01f;

            FixedHeight = false;
            FixedHeightMin = 0.5f;
            FixedHeightMax = 1f;
            FixedDirection = false;
            FixedDirectionVector = Vector3.down;
            DisplayNormalLength = 0.02f;
            DisplayNormalAmount = 64f;

            PaintColor = Color.white;
            EditR = true;
            EditG = true;
            EditB = true;
            EditA = true;

            FlowPaintToolControl.FPT_EditorData.ParameterReset();
        }



        [CustomEditor(typeof(FlowPaintTool))]
        public class FlowPaintTool_InspectorUI : Editor
        {
            private FlowPaintTool _instance = null;

            private void OnEnable()
            {
                _instance = target as FlowPaintTool;
            }

            private Texture2D RenderTextureToTexture2D(RenderTexture renderTexture)
            {
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTexture;

                int width = renderTexture.width;
                int height = renderTexture.height;
                Texture2D copyTexture2D = new Texture2D(width, height);
                copyTexture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                copyTexture2D.Apply();

                RenderTexture.active = previous;

                return copyTexture2D;
            }

            private void OutputPNG_Button()
            {
                if (!GUILayout.Button("Output PNG File")) return;

                string filePath = EditorUtility.SaveFilePanel("Output PNG File", "Assets", "texture", "png");
                if (string.IsNullOrEmpty(filePath)) return;

                Texture2D copyTexture2D = RenderTextureToTexture2D(_instance._outputRenderTexture);
                File.WriteAllBytes(filePath, copyTexture2D.EncodeToPNG());
                Destroy(copyTexture2D);

                Debug.Log("output path\n" + filePath);

                string appDataPath = Application.dataPath;

                if (filePath.StartsWith(appDataPath))
                {
                    filePath = filePath.Remove(0, appDataPath.Length - 6);
                    AssetDatabase.ImportAsset(filePath);
                    TextureImporter texImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
                    texImporter.sRGBTexture = _instance._fptData._actualSRGB;
                }
            }

            private void Line()
            {
                EditorGUILayout.Space(5);
                Rect pos = EditorGUILayout.GetControlRect(false, 1f);
                EditorGUI.DrawRect(pos, GUI.skin.label.normal.textColor);
                EditorGUILayout.Space(5);
            }

            private void CommonUI()
            {
                EditorGUILayout.LabelField("Path of RenderTexture for preview", FlowPaintTool_EditorWindow.CenterLabel);

                EditorGUILayout.TextField(_instance._outputRenderTexturePath);

                Line();

                EnableMaskMode = EditorGUILayout.Toggle("MaskMode", EnableMaskMode);
                EnableMaterialView = EditorGUILayout.Toggle("ViewMode", EnableMaterialView);

                Line();

                FlowPaintToolEditorData fpted = FlowPaintToolControl.FPT_EditorData;

                EditorGUILayout.LabelField("Camera settings");
                fpted.CameraVerticalRotateSpeed = EditorGUILayout.FloatField("Rotate Speed", fpted.CameraVerticalRotateSpeed);
                fpted.CameraMoveSpeed = EditorGUILayout.FloatField("Move Speed", fpted.CameraMoveSpeed);
                fpted.CameraInertia = EditorGUILayout.IntField("Inertia", fpted.CameraInertia);

                Line();

                EditorGUILayout.LabelField("Brush settings");
                BrushSize = EditorGUILayout.FloatField("Size", BrushSize);
                BrushStrength = EditorGUILayout.FloatField("Strength", BrushStrength);
                BrushType = (BrushTypeEnum)EditorGUILayout.EnumPopup("Type", BrushType);

                Line();
            }

            private void FlowPaintUI()
            {
                CommonUI();

                EditorGUILayout.LabelField("Flow paint settings");

                FixedHeight = EditorGUILayout.Toggle("Fixed Height", FixedHeight);

                if (FixedHeight)
                {
                    EditorGUI.indentLevel++;
                    float temp0 = EditorGUILayout.Slider("Fixed Height Min", FixedHeightMin, -1f, 1f);
                    float temp1 = EditorGUILayout.Slider("Fixed Height Max", FixedHeightMax, -1f, 1f);
                    FixedHeightMin = Mathf.Clamp(temp0, -1f, temp1);
                    FixedHeightMax = Mathf.Clamp(temp1, temp0, 1f);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(10);

                FixedDirection = EditorGUILayout.Toggle("Fixed Direction", FixedDirection);

                if (FixedDirection)
                {
                    EditorGUI.indentLevel++;
                    FixedDirectionVector = EditorGUILayout.Vector3Field("Fixed Direction Vector", FixedDirectionVector);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(10);

                DisplayNormalLength = EditorGUILayout.FloatField("Display Normal Length", DisplayNormalLength);
                DisplayNormalAmount = EditorGUILayout.FloatField("Display Normal Amount", DisplayNormalAmount);

                Line();

                OutputPNG_Button();
            }

            private void ColorPaintUI()
            {
                CommonUI();

                EditorGUILayout.LabelField("Color paint settings");

                PaintColor = EditorGUILayout.ColorField("Color", PaintColor);

                EditorGUILayout.Space(10);

                EditorGUILayout.LabelField("Edit channel");

                EditorGUILayout.BeginHorizontal();
                {
                    EditR = GUILayout.Toggle(EditR, "R");
                    EditG = GUILayout.Toggle(EditG, "G");
                    EditB = GUILayout.Toggle(EditB, "B");
                    EditA = GUILayout.Toggle(EditA, "A");
                }
                EditorGUILayout.EndHorizontal();

                Line();

                OutputPNG_Button();
            }

            private void MaskUI()
            {
                CommonUI();

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Select linked (Mask off)"))
                    {
                        _instance.SelectLinkedPlus();
                    }

                    if (GUILayout.Button("Select linked (Mask on)"))
                    {
                        _instance.SelectLinkedMinus();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(20);

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("All Mask"))
                    {
                        for (int pIndex = 0; pIndex < _instance._polygonCount; pIndex++)
                        {
                            _instance._pd_MaskArray[pIndex] = true;
                        }

                        _instance.MaskModeMeshTriangleUpdate();
                    }

                    if (GUILayout.Button("All Unmask"))
                    {
                        for (int pIndex = 0; pIndex < _instance._polygonCount; pIndex++)
                        {
                            _instance._pd_MaskArray[pIndex] = false;
                        }

                        _instance.MaskModeMeshTriangleUpdate();
                    }

                    if (GUILayout.Button("All Inversion"))
                    {
                        for (int pIndex = 0; pIndex < _instance._polygonCount; pIndex++)
                        {
                            _instance._pd_MaskArray[pIndex] = !_instance._pd_MaskArray[pIndex];
                        }

                        _instance.MaskModeMeshTriangleUpdate();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10);

                int subMeshCount = _instance._fptData._startMesh.subMeshCount;

                for (int index = 0; index < subMeshCount; ++index)
                {
                    EditorGUILayout.LabelField("SubMeshIndex : " + index);

                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Mask"))
                        {
                            for (int pIndex = 0; pIndex < _instance._polygonCount; pIndex++)
                            {
                                if (_instance._pd_SubMeshIndexArray[pIndex] != index) continue;

                                _instance._pd_MaskArray[pIndex] = true;
                            }

                            _instance.MaskModeMeshTriangleUpdate();
                        }

                        if (GUILayout.Button("Unmask"))
                        {
                            for (int pIndex = 0; pIndex < _instance._polygonCount; pIndex++)
                            {
                                if (_instance._pd_SubMeshIndexArray[pIndex] != index) continue;

                                _instance._pd_MaskArray[pIndex] = false;
                            }

                            _instance.MaskModeMeshTriangleUpdate();
                        }

                        if (GUILayout.Button("Inversion"))
                        {
                            for (int pIndex = 0; pIndex < _instance._polygonCount; pIndex++)
                            {
                                if (_instance._pd_SubMeshIndexArray[pIndex] != index) continue;

                                _instance._pd_MaskArray[pIndex] = !_instance._pd_MaskArray[pIndex];
                            }

                            _instance.MaskModeMeshTriangleUpdate();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            public override void OnInspectorGUI()
            {
                FlowPaintToolData fptData = _instance._fptData;

                if (EnableMaskMode)
                {
                    MaskUI();
                }
                else if (fptData._paintMode == PaintModeEnum.FlowPaintMode)
                {
                    FlowPaintUI();
                }
                else if (fptData._paintMode == PaintModeEnum.ColorPaintMode)
                {
                    ColorPaintUI();
                }
            }
        }
    }
}