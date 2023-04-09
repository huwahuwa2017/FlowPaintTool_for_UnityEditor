using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace FlowPaintTool
{
    public class FlowPaintTool : MonoBehaviour
    {
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

        [SerializeField]
        private Material _flowPaintMaterial = null;
        [SerializeField]
        private Material _colorPaintMaterial = null;
        [SerializeField]
        private Material _fillMaterial = null;
        [SerializeField]
        private Material _cutoutMaterial = null;
        [SerializeField]
        private Material _fillBleedMaterial = null;
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
        private GameObject _rangeVisualizationPrefab = null;

        [SerializeField]
        private PaintMode _paintMode = PaintMode.FlowPaintMode;
        [SerializeField]
        private int _outputTextureWidth = 1024;
        [SerializeField]
        private int _outputTextureHeight = 1024;
        [SerializeField]
        private StartTextureLoadMode _startTextureLoadMode = StartTextureLoadMode.Assets;
        [SerializeField]
        private Texture _startTexture = null;
        [SerializeField]
        private bool _startTextureSRGB = false;
        [SerializeField]
        private string _startTextureFilePath = string.Empty;
        [SerializeField]
        private Mesh _startMesh = null;
        [SerializeField]
        private int _bleedRange = 4;
        [SerializeField]
        private float _uv_Epsilon = 0.001f;

        //未実装　0固定
        [SerializeField]
        private int _targetUVChannel = 0;

        private bool _started = false;

        private Mesh _paintModeMesh = null;
        private Mesh _maskModeMesh = null;

        private List<PolygonData> _polygonList = new List<PolygonData>();
        private List<List<PolygonData>> _duplicatePolygonListList = new List<List<PolygonData>>();
        private List<List<PolygonData>> _subMeshPolygonListList = new List<List<PolygonData>>();
        private List<int> _subMeshIndexList = new List<int>();

        private bool _sRGB = false;
        private string _outputRenderTexturePath = string.Empty;
        private RenderTexture _outputRenderTexture = null;
        private RenderTexture[] _fillTextureArray = null;
        private Material[] _bleedMaterialArray = null;
        private CommandBuffer _bleedCommandBuffer = null;
        private CommandBuffer _flowPaintCommandBuffer = null;
        private CommandBuffer _colorPaintCommandBuffer = null;

        private GameObject _flowPaintRender = null;
        private GameObject _colorPaintRender = null;
        private GameObject _maskRender = null;
        private GameObject _materialRender = null;
        private GameObject _rangeVisualization = null;

        private Matrix4x4 _preMatrix = Matrix4x4.zero;
        private Vector3 _preHitPosition = Vector3.zero;
        private bool _preHit = false;

        private bool _preInputKeyTab;
        private bool _preInputKeyZ;



        private bool _enableMaskMode = false;
        private bool _enableMaterialView = false;

        private float _brushSize = 0.1f;
        private float _brushStrength = 1.0f;
        private BrushType _brushType = BrushType.Smooth;

        //UI未実装　0.01固定
        private float _brushMoveSensitivity = 0.01f;

        private bool _fixedHeight = false;
        private float _fixedHeightMin = 0.5f;
        private float _fixedHeightMax = 1f;

        private bool _fixedDirection = false;
        private Vector3 _fixedDirectionVector = Vector3.down;

        private Color _paintColor = Color.white;

        private bool _editR = true;
        private bool _editG = true;
        private bool _editB = true;
        private bool _editA = true;

        private void ResetOutputRenderTexture()
        {
            void GenerateOutputRenderTexture()
            {
                string path = AssetDatabase.GetAssetPath(_fillMaterial);
                path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(path)), "preview.renderTexture");

                GraphicsFormat graphicsFormat = _sRGB ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                RenderTextureDescriptor rtd = new RenderTextureDescriptor(_outputTextureWidth, _outputTextureHeight, graphicsFormat, 0);
                _outputRenderTexture = new RenderTexture(rtd);
                _outputRenderTexture.filterMode = FilterMode.Point;

                AssetDatabase.CreateAsset(_outputRenderTexture, path);
                AssetDatabase.SaveAssets();

                _outputRenderTexturePath = path;
            }

            Texture2D defaultColorTexture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
            defaultColorTexture.SetPixel(0, 0, new Color(0.5f, 0.5f, 1f, 1f));
            defaultColorTexture.Apply();

            if (_startTextureLoadMode == StartTextureLoadMode.Assets)
            {
                if (_startTexture != null)
                {
                    _sRGB = GraphicsFormatUtility.IsSRGBFormat(_startTexture.graphicsFormat);
                    GenerateOutputRenderTexture();

                    Graphics.Blit(_startTexture, _outputRenderTexture);
                }
                else
                {
                    _sRGB = false;
                    GenerateOutputRenderTexture();

                    Graphics.Blit(defaultColorTexture, _outputRenderTexture);
                }
            }
            else if (_startTextureLoadMode == StartTextureLoadMode.FilePath)
            {
                if (File.Exists(_startTextureFilePath))
                {
                    _sRGB = (PlayerSettings.colorSpace == ColorSpace.Linear) && _startTextureSRGB;
                    GenerateOutputRenderTexture();

                    GraphicsFormat temp0 = _sRGB ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                    Texture2D texture = new Texture2D(0, 0, temp0, TextureCreationFlags.None);
                    texture.LoadImage(File.ReadAllBytes(_startTextureFilePath));
                    Graphics.Blit(texture, _outputRenderTexture);
                    Destroy(texture);
                }
                else
                {
                    Debug.Log("Texture not found");

                    _sRGB = false;
                    GenerateOutputRenderTexture();

                    Graphics.Blit(defaultColorTexture, _outputRenderTexture);
                }
            }

            Destroy(defaultColorTexture);

            if (_sRGB)
            {
                Debug.Log("is sRGB");

                _flowResultMaterial.EnableKeyword("IS_SRGB");
                _colorResultMaterial.EnableKeyword("IS_SRGB");

                if (_paintMode == PaintMode.FlowPaintMode)
                {
                    Debug.LogError("Using sRGB textures in FlowPaintMode will not give accurate results\nPlease turn off sRGB");
                    EditorApplication.isPlaying = false;
                }
            }
            else
            {
                Debug.Log("is not sRGB");

                _flowResultMaterial.DisableKeyword("IS_SRGB");
                _colorResultMaterial.DisableKeyword("IS_SRGB");
            }
        }

        private void GeneratePolygonList()
        {
            int subMeshCount = _startMesh.subMeshCount;

            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; ++subMeshIndex)
            {
                int[] triangles = _startMesh.GetTriangles(subMeshIndex);
                int triangleListCount = triangles.Length;
                List<PolygonData> temp0 = new List<PolygonData>();

                for (int index = 0; index < triangleListCount; index += 3)
                {
                    PolygonData temp1 = new PolygonData(triangles[index], triangles[index + 1], triangles[index + 2]);
                    _polygonList.Add(temp1);
                    temp0.Add(temp1);
                }

                _subMeshPolygonListList.Add(temp0);
                _subMeshIndexList.Add(subMeshIndex);
            }

            List<Vector2> uvList = new List<Vector2>();
            _startMesh.GetUVs(_targetUVChannel, uvList);

            int polygonDataCount = _polygonList.Count;
            int startIndex = 0;

            while (startIndex < polygonDataCount)
            {
                List<PolygonData> duplicatePolygonList = new List<PolygonData>();

                PolygonData polygonData = _polygonList[startIndex];
                Vector2 centerUV = (uvList[polygonData.IndexA] + uvList[polygonData.IndexB] + uvList[polygonData.IndexC]) / 3f;

                for (int index = startIndex + 1; index < polygonDataCount; ++index)
                {
                    PolygonData targetPolygonData = _polygonList[index];

                    if (targetPolygonData.DuplicateUV) continue;

                    Vector2 targetCenterUV = (uvList[targetPolygonData.IndexA] + uvList[targetPolygonData.IndexB] + uvList[targetPolygonData.IndexC]) / 3f;
                    Vector2 temp0 = centerUV - targetCenterUV;

                    if ((Mathf.Abs(temp0.x) + Mathf.Abs(temp0.y)) < _uv_Epsilon)
                    {
                        targetPolygonData.DuplicateUV = true;

                        duplicatePolygonList.Add(targetPolygonData);
                    }
                }

                if (duplicatePolygonList.Count > 0)
                {
                    polygonData.DuplicateUV = true;

                    duplicatePolygonList.Add(polygonData);
                    _duplicatePolygonListList.Add(duplicatePolygonList);
                }

                ++startIndex;
            }
        }

        private void GenerateCommandBuffer()
        {
            int mainTexSPID = Shader.PropertyToID("_MainTex");
            int fillTexSPID = Shader.PropertyToID("_FillTex");
            int[] tempTexSPIDs = new int[] { Shader.PropertyToID("_TempTex0"), Shader.PropertyToID("_TempTex1") };

            RenderTextureDescriptor rtd_main = _outputRenderTexture.descriptor;
            RenderTextureDescriptor rtd_R8 = rtd_main;
            rtd_R8.graphicsFormat = GraphicsFormat.R8_UNorm;

            // Texture And Material Generate Start
            _fillTextureArray = new RenderTexture[Math.Max(_bleedRange, 1)];
            _fillTextureArray[0] = new RenderTexture(rtd_R8);

            CommandBuffer fillCommandBuffer = new CommandBuffer();
            fillCommandBuffer.SetRenderTarget(_fillTextureArray[0]);

            int subMeshCount = _startMesh.subMeshCount;

            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; ++subMeshIndex)
            {
                fillCommandBuffer.DrawMesh(_startMesh, Matrix4x4.identity, _fillMaterial, subMeshIndex);
            }

            Graphics.ExecuteCommandBuffer(fillCommandBuffer);

            for (int index = 1; index < _bleedRange; ++index)
            {
                _fillTextureArray[index] = new RenderTexture(rtd_R8);
                Graphics.Blit(_fillTextureArray[index - 1], _fillTextureArray[index], _fillBleedMaterial);
            }

            _bleedMaterialArray = new Material[_bleedRange];

            for (int index = 0; index < _bleedRange; ++index)
            {
                Material temp0 = Instantiate(_bleedMaterial);
                temp0.SetTexture(fillTexSPID, _fillTextureArray[index]);
                _bleedMaterialArray[index] = temp0;
            }

            _cutoutMaterial.SetTexture(fillTexSPID, _fillTextureArray[0]);
            _flowPaintMaterial.SetTexture(mainTexSPID, _outputRenderTexture);
            _colorPaintMaterial.SetTexture(mainTexSPID, _outputRenderTexture);
            _flowResultMaterial.SetTexture(mainTexSPID, _outputRenderTexture);
            _colorResultMaterial.SetTexture(mainTexSPID, _outputRenderTexture);
            // Texture and Material Generate End

            // _bleedCommandBuffer Generate Start
            _bleedCommandBuffer = new CommandBuffer();
            _bleedCommandBuffer.GetTemporaryRT(tempTexSPIDs[0], rtd_main);
            _bleedCommandBuffer.GetTemporaryRT(tempTexSPIDs[1], rtd_main);
            _bleedCommandBuffer.Blit(_outputRenderTexture, tempTexSPIDs[0], _cutoutMaterial);

            int temp1 = 0;

            for (int index = 0; index < _bleedRange; ++index)
            {
                temp1 = 1 - temp1;
                _bleedCommandBuffer.Blit(tempTexSPIDs[1 - temp1], tempTexSPIDs[temp1], _bleedMaterialArray[index]);
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
            _flowPaintCommandBuffer.DrawMesh(_paintModeMesh, Matrix4x4.identity, _flowPaintMaterial, 0);
            _flowPaintCommandBuffer.Blit(tempTexSPIDs[0], _outputRenderTexture);
            _flowPaintCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[0]);
            // _flowPaintCommandBuffer Generate End

            // _colorPaintCommandBuffer Generate Start
            _colorPaintCommandBuffer = new CommandBuffer();
            _colorPaintCommandBuffer.GetTemporaryRT(tempTexSPIDs[0], rtd_main);
            _colorPaintCommandBuffer.Blit(_outputRenderTexture, tempTexSPIDs[0]);
            _colorPaintCommandBuffer.SetRenderTarget(tempTexSPIDs[0]);
            _colorPaintCommandBuffer.DrawMesh(_paintModeMesh, Matrix4x4.identity, _colorPaintMaterial, 0);
            _colorPaintCommandBuffer.Blit(tempTexSPIDs[0], _outputRenderTexture);
            _colorPaintCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[0]);
            // _colorPaintCommandBuffer Generate End
        }

        private void GenerateRenderObject()
        {
            int subMeshCount = _startMesh.subMeshCount;

            Material[] flowResultMaterials = Enumerable.Repeat(_flowResultMaterial, subMeshCount).ToArray();
            Material[] colorResultMaterials = Enumerable.Repeat(_colorResultMaterial, subMeshCount).ToArray();
            Material[] maskResultMaterials = new Material[] { _material_MaskOff, _material_MaskOn };

            _flowPaintRender = new GameObject("FlowPaintRender");
            _flowPaintRender.transform.SetParent(transform, false);
            _flowPaintRender.AddComponent<MeshFilter>().sharedMesh = _startMesh;
            _flowPaintRender.AddComponent<MeshRenderer>().sharedMaterials = flowResultMaterials;

            _colorPaintRender = new GameObject("ColorPaintRender");
            _colorPaintRender.transform.SetParent(transform, false);
            _colorPaintRender.AddComponent<MeshFilter>().sharedMesh = _startMesh;
            _colorPaintRender.AddComponent<MeshRenderer>().sharedMaterials = colorResultMaterials;

            _maskRender = new GameObject("MaskRender");
            _maskRender.transform.SetParent(transform, false);
            _maskRender.AddComponent<MeshFilter>().sharedMesh = _maskModeMesh;
            _maskRender.AddComponent<MeshRenderer>().sharedMaterials = maskResultMaterials;

            _materialRender = new GameObject("MaterialRender");
            _materialRender.transform.SetParent(transform, false);
            _materialRender.AddComponent<MeshFilter>().sharedMesh = _startMesh;
            _materialRender.AddComponent<MeshRenderer>();

            _rangeVisualization = Instantiate(_rangeVisualizationPrefab);
        }

        private void MaskModeMeshTriangleUpdate()
        {
            List<int> triangleList0 = new List<int>(_polygonList.Count);
            List<int> triangleList1 = new List<int>(_polygonList.Count);

            foreach (PolygonData polygonData in _polygonList)
            {
                if (polygonData.Mask)
                {
                    triangleList1.Add(polygonData.IndexA);
                    triangleList1.Add(polygonData.IndexB);
                    triangleList1.Add(polygonData.IndexC);
                }
                else
                {
                    triangleList0.Add(polygonData.IndexA);
                    triangleList0.Add(polygonData.IndexB);
                    triangleList0.Add(polygonData.IndexC);
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
            if (_startMesh == null)
            {
                Debug.LogError("Mesh not set");
                EditorApplication.isPlaying = false;
                return;
            }

            if (!_startMesh.isReadable)
            {
                Debug.LogError("Please allow Read/Write for the mesh");
                EditorApplication.isPlaying = false;
                return;
            }

            _started = true;

            EditorApplication.playModeStateChanged += RemoveOutputRenderTexture;

            _maskModeMesh = Instantiate(_startMesh);
            _maskModeMesh.MarkDynamic();
            _maskModeMesh.triangles = new int[0];
            _maskModeMesh.subMeshCount = 2;

            _paintModeMesh = Instantiate(_maskModeMesh);
            _paintModeMesh.subMeshCount = 1;

            Camera camera = Camera.main;
            camera.nearClipPlane = 0.001f;
            camera.gameObject.AddComponent<CameraControl2>();

            MeshCollider targetCollider = gameObject.AddComponent<MeshCollider>();
            targetCollider.sharedMesh = _startMesh;

            ResetOutputRenderTexture();

            GeneratePolygonList();
            GenerateCommandBuffer();
            GenerateRenderObject();

            MaskModeMeshTriangleUpdate();
        }

        private void Update()
        {
            _brushSize = Math.Max(_brushSize, 0f);
            _brushStrength = Mathf.Clamp01(_brushStrength);

            float preFixedHeightMin = _fixedHeightMin;
            _fixedHeightMin = Mathf.Clamp(_fixedHeightMin, -1f, _fixedHeightMax);
            _fixedHeightMax = Mathf.Clamp(_fixedHeightMax, preFixedHeightMin, 1f);

            float scrollDelta = Input.mouseScrollDelta.y;

            bool inspectorUpdate = false;

            if (Input.GetKey(KeyCode.R))
            {
                _brushSize *= 1f + (scrollDelta * 0.05f);
                inspectorUpdate = true;
            }

            if (Input.GetKey(KeyCode.F))
            {
                _brushStrength *= 1f + (scrollDelta * 0.05f);
                inspectorUpdate = true;
            }

            bool inputKeyTab = Input.GetKey(KeyCode.Tab);

            if (!_preInputKeyTab && inputKeyTab)
            {
                _enableMaskMode = !_enableMaskMode;
                inspectorUpdate = true;
            }

            _preInputKeyTab = inputKeyTab;

            bool inputKeyZ = Input.GetKey(KeyCode.Z);

            if (!_preInputKeyZ && inputKeyZ)
            {
                _enableMaterialView = !_enableMaterialView;
                inspectorUpdate = true;
            }

            _preInputKeyZ = inputKeyZ;

            if (inspectorUpdate)
            {
                foreach (Editor editorUI in ActiveEditorTracker.sharedTracker.activeEditors)
                {
                    if (editorUI == null) continue;
                    editorUI.Repaint();
                }
            }
        }

        private void PaintModeMeshTriangleUpdate(Vector3 hitPosition)
        {
            foreach (PolygonData polygonData in _polygonList)
            {
                polygonData.MaskResult = polygonData.DuplicateUV || polygonData.Mask;
            }

            foreach (List<PolygonData> duplicatePolygonList in _duplicatePolygonListList)
            {
                float minSqrDistance = float.MaxValue;
                PolygonData targetPolygonData = null;

                foreach (PolygonData duplicatePolygon in duplicatePolygonList)
                {
                    if (duplicatePolygon.Mask) continue;

                    float sqrDistance = (hitPosition - duplicatePolygon.Center).sqrMagnitude;

                    if (sqrDistance < minSqrDistance)
                    {
                        minSqrDistance = sqrDistance;
                        targetPolygonData = duplicatePolygon;
                    }
                }

                if (targetPolygonData != null)
                {
                    targetPolygonData.MaskResult = false;
                }
            }

            List<int> triangleList0 = new List<int>(_polygonList.Count);

            foreach (PolygonData polygonData in _polygonList)
            {
                if (!polygonData.MaskResult)
                {
                    triangleList0.Add(polygonData.IndexA);
                    triangleList0.Add(polygonData.IndexB);
                    triangleList0.Add(polygonData.IndexC);
                }
            }

            _paintModeMesh.SetTriangles(triangleList0, 0);
        }

        private void FixedUpdate_FlowPaint(bool hit, RaycastHit raycastHit)
        {
            _flowPaintRender.SetActive(!_enableMaterialView);
            _colorPaintRender.SetActive(false);
            _maskRender.SetActive(false);
            _materialRender.SetActive(_enableMaterialView);

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

            if (paintDirection.magnitude > distance * _brushMoveSensitivity)
            {
                PaintModeMeshTriangleUpdate(_preHitPosition);

                Matrix4x4 temp0 = transform.localToWorldMatrix;
                paintDirection = _fixedDirection ? _fixedDirectionVector : paintDirection;

                _flowPaintMaterial.SetMatrix(_modelMatrixSPID, temp0);
                _flowPaintMaterial.SetVector(_hitPositionSPID, _preHitPosition);

                _flowPaintMaterial.SetInt(_brushTypeSPID, (int)_brushType);
                _flowPaintMaterial.SetFloat(_brushSizeSPID, _brushSize);
                _flowPaintMaterial.SetFloat(_brushStrengthSPID, _brushStrength);

                _flowPaintMaterial.SetMatrix(_inverseModelMatrixSPID, Matrix4x4.Inverse(temp0));
                _flowPaintMaterial.SetVector(_paintDirectionSPID, paintDirection);

                _flowPaintMaterial.SetInt(_fixedHeightSPID, Convert.ToInt32(_fixedHeight));
                _flowPaintMaterial.SetFloat(_fixedHeightMinSPID, _fixedHeightMin);
                _flowPaintMaterial.SetFloat(_fixedHeightMaxSPID, _fixedHeightMax);

                Graphics.ExecuteCommandBuffer(_flowPaintCommandBuffer);

                _preHitPosition = hitPosition;
            }
            else
            {
                Graphics.ExecuteCommandBuffer(_bleedCommandBuffer);
            }
        }

        private void FixedUpdate_ColorPaint(bool hit, RaycastHit raycastHit)
        {
            _flowPaintRender.SetActive(false);
            _colorPaintRender.SetActive(!_enableMaterialView);
            _maskRender.SetActive(false);
            _materialRender.SetActive(_enableMaterialView);

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

            if (paintDirection.magnitude > distance * _brushMoveSensitivity)
            {
                PaintModeMeshTriangleUpdate(_preHitPosition);

                Matrix4x4 temp0 = transform.localToWorldMatrix;
                int temp1 = (_editR ? 1 : 0) + (_editG ? 2 : 0) + (_editB ? 4 : 0) + (_editA ? 8 : 0);

                _colorPaintMaterial.SetMatrix(_modelMatrixSPID, temp0);
                _colorPaintMaterial.SetVector(_hitPositionSPID, _preHitPosition);

                _colorPaintMaterial.SetInt(_brushTypeSPID, (int)_brushType);
                _colorPaintMaterial.SetFloat(_brushSizeSPID, _brushSize);
                _colorPaintMaterial.SetFloat(_brushStrengthSPID, _brushStrength);

                _colorPaintMaterial.SetColor(_PaintColorSPID, _paintColor);

                _colorPaintMaterial.SetInt(_EditRGBASPID, temp1);

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
            _materialRender.SetActive(false);

            if (!hit) return;

            bool leftClick = Input.GetMouseButton(0);
            bool rightClick = Input.GetMouseButton(1);

            Vector3 hitPosition = raycastHit.point;
            float distance = (hitPosition - Camera.main.transform.position).magnitude;

            float range = _brushSize;
            float sqrRange = range * range;

            if (leftClick || rightClick)
            {
                foreach (PolygonData polygonData in _polygonList)
                {
                    if ((hitPosition - polygonData.Center).sqrMagnitude < sqrRange)
                    {
                        polygonData.Mask = leftClick;
                    }
                }

                MaskModeMeshTriangleUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (!_started) return;

            // CenterRecalculation Start
            Matrix4x4 matrix = transform.localToWorldMatrix;

            if (_preMatrix != matrix)
            {
                _preMatrix = matrix;

                Vector3[] vertexPositionArray = _startMesh.vertices;
                int maxIndex = vertexPositionArray.Length;

                for (int index = 0; index < maxIndex; ++index)
                {
                    vertexPositionArray[index] = matrix.MultiplyPoint3x4(vertexPositionArray[index]);
                }

                foreach (PolygonData polygonData in _polygonList)
                {
                    polygonData.CenterRecalculation(vertexPositionArray);
                }
            }
            // CenterRecalculation End

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool hit = Physics.Raycast(ray, out RaycastHit raycastHit, 100f);

            _rangeVisualization.SetActive(hit);
            Transform temp0 = _rangeVisualization.transform;
            temp0.position = raycastHit.point;
            temp0.rotation = Camera.main.transform.rotation;
            temp0.localScale = new Vector3(_brushSize, _brushSize, _brushSize) * 2f;

            if (_enableMaskMode)
            {
                FixedUpdate_Mask(hit, raycastHit);
            }
            else if (_paintMode == PaintMode.FlowPaintMode)
            {
                FixedUpdate_FlowPaint(hit, raycastHit);
            }
            else if (_paintMode == PaintMode.ColorPaintMode)
            {
                FixedUpdate_ColorPaint(hit, raycastHit);
            }

            _preHit = hit;
        }

        private void AllMask()
        {
            foreach (PolygonData polygonData in _polygonList)
            {
                polygonData.Mask = true;
            }

            MaskModeMeshTriangleUpdate();
        }

        private void AllUnmask()
        {
            foreach (PolygonData polygonData in _polygonList)
            {
                polygonData.Mask = false;
            }

            MaskModeMeshTriangleUpdate();
        }

        private void AllMaskInversion()
        {
            foreach (PolygonData polygonData in _polygonList)
            {
                polygonData.Mask = !polygonData.Mask;
            }

            MaskModeMeshTriangleUpdate();
        }

        private void SubMeshMask(int index)
        {
            List<PolygonData> subMeshPolygonList = _subMeshPolygonListList[index];

            foreach (PolygonData polygonData in subMeshPolygonList)
            {
                polygonData.Mask = true;
            }

            MaskModeMeshTriangleUpdate();
        }

        private void SubMeshUnmask(int index)
        {
            List<PolygonData> subMeshPolygonList = _subMeshPolygonListList[index];

            foreach (PolygonData polygonData in subMeshPolygonList)
            {
                polygonData.Mask = false;
            }

            MaskModeMeshTriangleUpdate();
        }

        private void SubMeshMaskInversion(int index)
        {
            List<PolygonData> subMeshPolygonList = _subMeshPolygonListList[index];

            foreach (PolygonData polygonData in subMeshPolygonList)
            {
                polygonData.Mask = !polygonData.Mask;
            }

            MaskModeMeshTriangleUpdate();
        }



        [CustomEditor(typeof(FlowPaintTool))]
        public class FlowPaintToolEditorUI : Editor
        {
            private static void RenderTextureToPNG(RenderTexture renderTexture, string filePath)
            {
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTexture;

                int width = renderTexture.width;
                int height = renderTexture.height;
                Texture2D copyTexture2D = new Texture2D(width, height);
                copyTexture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                copyTexture2D.Apply();

                RenderTexture.active = previous;

                File.WriteAllBytes(filePath, copyTexture2D.EncodeToPNG());
                Destroy(copyTexture2D);

                Debug.Log("output the texture\n" + filePath);
            }

            private FlowPaintTool _instance;

            private SerializedProperty _paintModeProp = null;
            private SerializedProperty _outputTextureWidthProp = null;
            private SerializedProperty _outputTextureHeightProp = null;
            private SerializedProperty _startTextureLoadModeProp = null;
            private SerializedProperty _startTextureProp = null;
            private SerializedProperty _startTextureSRGBProp = null;
            private SerializedProperty _startTextureFilePathProp = null;
            private SerializedProperty _startMeshProp = null;
            private SerializedProperty _bleedRangeProp = null;
            private SerializedProperty _uv_EpsilonProp = null;
            //private SerializedProperty _targetUVChannelProp = null;

            private void OnEnable()
            {
                _instance = target as FlowPaintTool;

                _paintModeProp = serializedObject.FindProperty("_paintMode");
                _outputTextureWidthProp = serializedObject.FindProperty("_outputTextureWidth");
                _outputTextureHeightProp = serializedObject.FindProperty("_outputTextureHeight");
                _startTextureLoadModeProp = serializedObject.FindProperty("_startTextureLoadMode");
                _startTextureProp = serializedObject.FindProperty("_startTexture");
                _startTextureSRGBProp = serializedObject.FindProperty("_startTextureSRGB");
                _startTextureFilePathProp = serializedObject.FindProperty("_startTextureFilePath");
                _startMeshProp = serializedObject.FindProperty("_startMesh");
                _bleedRangeProp = serializedObject.FindProperty("_bleedRange");
                _uv_EpsilonProp = serializedObject.FindProperty("_uv_Epsilon");
                //_targetUVChannelProp = serializedObject.FindProperty("_targetUVChannel");
            }

            private void OutputButton()
            {
                if (!GUILayout.Button("Output PNG File")) return;

                string filePath = EditorUtility.SaveFilePanel("Output PNG File", string.Empty, "texture", "png");
                if (string.IsNullOrEmpty(filePath)) return;

                RenderTextureToPNG(_instance._outputRenderTexture, filePath);
            }

            private void PreStartUI()
            {
                serializedObject.Update();

                EditorGUILayout.PropertyField(_paintModeProp);

                GUILayout.Space(20);

                EditorGUILayout.PropertyField(_outputTextureWidthProp);
                EditorGUILayout.PropertyField(_outputTextureHeightProp);

                GUILayout.Space(20);

                EditorGUILayout.PropertyField(_startTextureLoadModeProp);

                if (_instance._startTextureLoadMode == StartTextureLoadMode.Assets)
                {
                    EditorGUILayout.PropertyField(_startTextureProp);
                }
                else if (_instance._startTextureLoadMode == StartTextureLoadMode.FilePath)
                {
                    EditorGUILayout.PropertyField(_startTextureSRGBProp, new GUIContent("sRGB (Color Texture)"));

                    if (GUILayout.Button("Select Texture"))
                    {
                        string filePath = EditorUtility.OpenFilePanel("Select Texture", string.Empty, string.Empty);

                        if (!string.IsNullOrEmpty(filePath))
                        {
                            _instance._startTextureFilePath = filePath;
                        }
                    }

                    EditorGUILayout.PropertyField(_startTextureFilePathProp);
                }

                GUILayout.Space(20);

                EditorGUILayout.PropertyField(_startMeshProp);

                GUILayout.Space(20);

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Advanced Settings");
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(_bleedRangeProp);
                EditorGUILayout.PropertyField(_uv_EpsilonProp);
                //EditorGUILayout.PropertyField(_targetUVChannelProp);

                serializedObject.ApplyModifiedProperties();
            }

            private void FlowPaintUI()
            {
                if (GUILayout.Button("Change to MaskMode"))
                {
                    _instance._enableMaskMode = true;
                }

                if (GUILayout.Button("Change to ViewMode"))
                {
                    _instance._enableMaterialView = !_instance._enableMaterialView;
                }

                GUILayout.Space(20);

                _instance._brushSize = EditorGUILayout.FloatField("Brush Size", _instance._brushSize);
                _instance._brushStrength = EditorGUILayout.FloatField("Brush Strength", _instance._brushStrength);
                _instance._brushType = (BrushType)EditorGUILayout.EnumPopup("Brush Type", _instance._brushType);

                GUILayout.Space(20);

                _instance._fixedHeight = GUILayout.Toggle(_instance._fixedHeight, "Fixed Height");

                if (_instance._fixedHeight)
                {
                    _instance._fixedHeightMin = EditorGUILayout.Slider("Fixed Height Min", _instance._fixedHeightMin, -1f, 1f);
                    _instance._fixedHeightMax = EditorGUILayout.Slider("Fixed Height Max", _instance._fixedHeightMax, -1f, 1f);
                }

                GUILayout.Space(20);

                _instance._fixedDirection = GUILayout.Toggle(_instance._fixedDirection, "Fixed Direction");

                if (_instance._fixedDirection)
                {
                    _instance._fixedDirectionVector = EditorGUILayout.Vector3Field("Fixed Direction Vector", _instance._fixedDirectionVector);
                }

                GUILayout.Space(20);

                OutputButton();
            }

            private void ColorPaintUI()
            {
                if (GUILayout.Button("Change to MaskMode"))
                {
                    _instance._enableMaskMode = true;
                }

                if (GUILayout.Button("Change to ViewMode"))
                {
                    _instance._enableMaterialView = !_instance._enableMaterialView;
                }

                GUILayout.Space(20);

                _instance._brushSize = EditorGUILayout.FloatField("Brush Size", _instance._brushSize);
                _instance._brushStrength = EditorGUILayout.FloatField("Brush Strength", _instance._brushStrength);
                _instance._brushType = (BrushType)EditorGUILayout.EnumPopup("Brush Type", _instance._brushType);

                GUILayout.Space(20);

                _instance._paintColor = EditorGUILayout.ColorField("Color", _instance._paintColor);

                GUILayout.Space(20);

                GUILayout.Label("Edit channel");

                GUILayout.BeginHorizontal();
                {
                    _instance._editR = GUILayout.Toggle(_instance._editR, "R");
                    _instance._editG = GUILayout.Toggle(_instance._editG, "G");
                    _instance._editB = GUILayout.Toggle(_instance._editB, "B");
                    _instance._editA = GUILayout.Toggle(_instance._editA, "A");
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(20);

                OutputButton();
            }

            private void MaskUI()
            {
                if (GUILayout.Button("Change to " + _instance._paintMode.ToString()))
                {
                    _instance._enableMaskMode = false;
                }

                GUILayout.Space(20);

                _instance._brushSize = EditorGUILayout.FloatField("Brush Size", _instance._brushSize);

                GUILayout.Space(20);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("All Mask"))
                    {
                        _instance.AllMask();
                    }

                    if (GUILayout.Button("All Unmask"))
                    {
                        _instance.AllUnmask();
                    }

                    if (GUILayout.Button("All Inversion"))
                    {
                        _instance.AllMaskInversion();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(20);

                int maxIndex = _instance._subMeshIndexList.Count;

                for (int index = 0; index < maxIndex; ++index)
                {
                    GUILayout.Label("SubMeshIndex : " + _instance._subMeshIndexList[index]);

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Mask"))
                        {
                            _instance.SubMeshMask(index);
                        }

                        if (GUILayout.Button("Unmask"))
                        {
                            _instance.SubMeshUnmask(index);
                        }

                        if (GUILayout.Button("Inversion"))
                        {
                            _instance.SubMeshMaskInversion(index);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            public override void OnInspectorGUI()
            {
                if (!_instance._started)
                {
                    PreStartUI();
                }
                else if (_instance._enableMaskMode)
                {
                    MaskUI();
                }
                else if (_instance._paintMode == PaintMode.FlowPaintMode)
                {
                    FlowPaintUI();
                }
                else if (_instance._paintMode == PaintMode.ColorPaintMode)
                {
                    ColorPaintUI();
                }
            }
        }
    }
}
