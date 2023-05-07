using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace FlowPaintTool
{
    public class FPT_ShaderProcess
    {
        private static readonly int _mainTexSPID = Shader.PropertyToID("_MainTex");
        private static readonly int _fillTexSPID = Shader.PropertyToID("_FillTex");
        private static readonly int _paintTexSPID = Shader.PropertyToID("_PaintTex");
        private static readonly int _densityTexSPID = Shader.PropertyToID("_DensityTex");

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

        private string _outputRenderTexturePath = string.Empty;
        private RenderTexture _outputRenderTexture = null;
        private RenderTexture _preOutputRenderTexture = null;
        private RenderTexture[] _fillRenderTextureArray = null;
        private RenderTexture _paintRenderTexture = null;
        private RenderTexture _densityRenderTexture = null;
        private RenderTexture[] _undoMemoryRenderTextureArray = null;

        private Material _copyFlowPaintMaterial = null;
        private Material _copyColorPaintMaterial = null;
        private Material _copyDensityMaterial = null;
        private Material _copyFlowMergeMaterial = null;
        private Material _copyColorMergeMaterial = null;
        private Material _copyCutoutMaterial = null;
        private Material[] _copyBleedMaterialArray = null;
        private Material _copyFlowResultMaterial = null;
        private Material _copyColorResultMaterial = null;

        private CommandBuffer _bleedCommandBuffer = null;
        private CommandBuffer _paintCommandBuffer = null;
        private CommandBuffer _mergeCommandBuffer = null;

        private FPT_EditorData _editorData = null;
        private FPT_Assets _assets = null;
        private FPT_MeshProcess _meshProcess = null;
        private FPT_PaintModeEnum _paintMode = FPT_PaintModeEnum.FlowPaintMode;
        private int _bleedRange = 4;
        private bool _actualSRGB = false;

        private Vector3 _preHitPosition = Vector3.zero;
        private bool _prePaint = false;
        private bool _preHit = false;
        private int _memoryCount = 0;
        private int _undoMemoryIndex = 0;
        private int _redoMemoryIndex = 0;

        private void RemoveOutputRenderTexture(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= RemoveOutputRenderTexture;

                AssetDatabase.DeleteAsset(_outputRenderTexturePath);
            }
        }

        private void TargetUVChannel(FPT_MainData fptData, Material mat)
        {
            mat.DisableKeyword("UV_CHANNEL_0");
            mat.DisableKeyword("UV_CHANNEL_1");
            mat.DisableKeyword("UV_CHANNEL_2");
            mat.DisableKeyword("UV_CHANNEL_3");
            mat.DisableKeyword("UV_CHANNEL_4");
            mat.DisableKeyword("UV_CHANNEL_5");
            mat.DisableKeyword("UV_CHANNEL_6");
            mat.DisableKeyword("UV_CHANNEL_7");

            mat.EnableKeyword("UV_CHANNEL_" + fptData._targetUVChannel);
        }

        public FPT_ShaderProcess(FPT_MainData fptData, FPT_MeshProcess meshProcess, int InstanceID)
        {
            _editorData = FPT_EditorWindow.EditorDataInstance;
            _assets = FPT_EditorWindow.RequestAssetsInstance;
            _meshProcess = meshProcess;
            _paintMode = fptData._paintMode;
            _bleedRange = fptData._bleedRange;
            _actualSRGB = fptData._actualSRGB;
            _memoryCount = _editorData.GetUndoMaxCount() + 1;

            // GenerateOutputRenderTexture Start
            GraphicsFormat graphicsFormat = _actualSRGB ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
            RenderTextureDescriptor rtd = new RenderTextureDescriptor(fptData._outputTextureResolution.x, fptData._outputTextureResolution.y, graphicsFormat, 0);
            _outputRenderTexture = new RenderTexture(rtd);
            _outputRenderTexture.filterMode = FilterMode.Point;

            string path = Path.GetDirectoryName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(_assets._fillMaterial)));
            path = Path.Combine(path, $"RT{InstanceID}.renderTexture");
            AssetDatabase.CreateAsset(_outputRenderTexture, path);
            _outputRenderTexturePath = path;

            EditorApplication.playModeStateChanged += RemoveOutputRenderTexture;

            if (fptData._textureExist)
            {
                if (fptData._startTextureLoadMode == FPT_StartTextureLoadModeEnum.Assets)
                {
                    Graphics.Blit(fptData._startTexture, _outputRenderTexture);
                }
                else if (fptData._startTextureLoadMode == FPT_StartTextureLoadModeEnum.FilePath)
                {
                    GraphicsFormat temp0 = _actualSRGB ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                    Texture2D texture = new Texture2D(0, 0, temp0, TextureCreationFlags.None);
                    texture.LoadImage(File.ReadAllBytes(fptData._startTextureFilePath));
                    Graphics.Blit(texture, _outputRenderTexture);
                    UnityEngine.Object.Destroy(texture);
                }
            }
            else
            {
                Debug.Log("Texture not found");

                Texture2D defaultColorTexture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
                defaultColorTexture.SetPixel(0, 0, new Color(0.5f, 0.5f, 1f, 1f));
                defaultColorTexture.Apply();
                Graphics.Blit(defaultColorTexture, _outputRenderTexture);
                UnityEngine.Object.Destroy(defaultColorTexture);
            }
            // GenerateOutputRenderTexture End

            // GenerateTexture Start
            RenderTextureDescriptor rtd_main = _outputRenderTexture.descriptor;
            RenderTextureDescriptor rtd_R8 = rtd_main;
            rtd_R8.graphicsFormat = GraphicsFormat.R8_UNorm;
            RenderTextureDescriptor rtd_R16 = rtd_main;
            rtd_R16.graphicsFormat = GraphicsFormat.R16_UNorm;

            TargetUVChannel(fptData, _assets._fillMaterial);

            _preOutputRenderTexture = new RenderTexture(rtd_main);
            _paintRenderTexture = new RenderTexture(rtd_main);
            _densityRenderTexture = new RenderTexture(rtd_R16);

            _fillRenderTextureArray = new RenderTexture[Math.Max(_bleedRange, 1)];
            _fillRenderTextureArray[0] = new RenderTexture(rtd_R8);

            CommandBuffer fillCommandBuffer = new CommandBuffer();
            fillCommandBuffer.SetRenderTarget(_fillRenderTextureArray[0]);

            int subMeshCount = fptData._startMesh.subMeshCount;

            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; ++subMeshIndex)
            {
                fillCommandBuffer.DrawMesh(fptData._startMesh, Matrix4x4.identity, _assets._fillMaterial, subMeshIndex);
            }

            Graphics.ExecuteCommandBuffer(fillCommandBuffer);

            for (int index = 1; index < _bleedRange; ++index)
            {
                _fillRenderTextureArray[index] = new RenderTexture(rtd_R8);
                Graphics.Blit(_fillRenderTextureArray[index - 1], _fillRenderTextureArray[index], _assets._fillBleedMaterial);
            }

            _undoMemoryRenderTextureArray = new RenderTexture[_memoryCount];

            for (int index = 0; index < _memoryCount; ++index)
            {
                _undoMemoryRenderTextureArray[index] = new RenderTexture(rtd_main);
            }

            Graphics.Blit(_outputRenderTexture, _undoMemoryRenderTextureArray[0]);
            // GenerateTexture End

            // GenerateMaterial Start
            _copyBleedMaterialArray = new Material[_bleedRange];

            for (int index = 0; index < _bleedRange; ++index)
            {
                Material copyBleedMaterial = UnityEngine.Object.Instantiate(_assets._bleedMaterial);
                _copyBleedMaterialArray[index] = copyBleedMaterial;
            }

            _copyFlowPaintMaterial = UnityEngine.Object.Instantiate(_assets._flowPaintMaterial);
            _copyColorPaintMaterial = UnityEngine.Object.Instantiate(_assets._colorPaintMaterial);
            _copyDensityMaterial = UnityEngine.Object.Instantiate(_assets._densityMaterial);
            _copyFlowMergeMaterial = UnityEngine.Object.Instantiate(_assets._flowMergeMaterial);
            _copyColorMergeMaterial = UnityEngine.Object.Instantiate(_assets._colorMergeMaterial);
            _copyCutoutMaterial = UnityEngine.Object.Instantiate(_assets._cutoutMaterial);
            _copyFlowResultMaterial = UnityEngine.Object.Instantiate(_assets._flowResultMaterial);
            _copyColorResultMaterial = UnityEngine.Object.Instantiate(_assets._colorResultMaterial);

            TargetUVChannel(fptData, _copyFlowPaintMaterial);
            TargetUVChannel(fptData, _copyColorPaintMaterial);
            TargetUVChannel(fptData, _copyDensityMaterial);
            TargetUVChannel(fptData, _copyFlowResultMaterial);
            TargetUVChannel(fptData, _copyColorResultMaterial);

            if (_actualSRGB)
            {
                _copyFlowResultMaterial.EnableKeyword("IS_SRGB");
                _copyColorResultMaterial.EnableKeyword("IS_SRGB");
            }
            else
            {
                _copyFlowResultMaterial.DisableKeyword("IS_SRGB");
                _copyColorResultMaterial.DisableKeyword("IS_SRGB");
            }
            // GenerateMaterial End

            // GenerateCommandBuffer Start
            Mesh paintModeMesh = _meshProcess.GetPaintModeMesh();

            int[] tempTexSPIDs = new int[] { Shader.PropertyToID("_TempTex0"), Shader.PropertyToID("_TempTex1") };
            Material targetPaintMaterial = null;
            Material targetMergeMaterial = null;

            if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
            {
                targetPaintMaterial = _copyFlowPaintMaterial;
                targetMergeMaterial = _copyFlowMergeMaterial;
            }
            else if (_paintMode == FPT_PaintModeEnum.ColorPaintMode)
            {
                targetPaintMaterial = _copyColorPaintMaterial;
                targetMergeMaterial = _copyColorMergeMaterial;
            }

            _paintCommandBuffer = new CommandBuffer();
            _paintCommandBuffer.GetTemporaryRT(tempTexSPIDs[0], rtd_main);
            _paintCommandBuffer.Blit(_paintRenderTexture, tempTexSPIDs[0]);
            _paintCommandBuffer.SetRenderTarget(tempTexSPIDs[0]);
            _paintCommandBuffer.DrawMesh(paintModeMesh, Matrix4x4.identity, targetPaintMaterial, 0);
            _paintCommandBuffer.Blit(tempTexSPIDs[0], _paintRenderTexture);
            _paintCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[0]);

            _paintCommandBuffer.GetTemporaryRT(tempTexSPIDs[1], rtd_R16);
            _paintCommandBuffer.Blit(_densityRenderTexture, tempTexSPIDs[1]);
            _paintCommandBuffer.SetRenderTarget(tempTexSPIDs[1]);
            _paintCommandBuffer.DrawMesh(paintModeMesh, Matrix4x4.identity, _copyDensityMaterial, 0);
            _paintCommandBuffer.Blit(tempTexSPIDs[1], _densityRenderTexture);
            _paintCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[1]);



            _mergeCommandBuffer = new CommandBuffer();
            _mergeCommandBuffer.GetTemporaryRT(tempTexSPIDs[0], rtd_main);
            _mergeCommandBuffer.Blit(_preOutputRenderTexture, tempTexSPIDs[0], targetMergeMaterial);
            _mergeCommandBuffer.Blit(tempTexSPIDs[0], _outputRenderTexture, _copyCutoutMaterial);
            _mergeCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[0]);



            _bleedCommandBuffer = new CommandBuffer();
            _bleedCommandBuffer.GetTemporaryRT(tempTexSPIDs[0], rtd_main);
            _bleedCommandBuffer.GetTemporaryRT(tempTexSPIDs[1], rtd_main);
            _bleedCommandBuffer.Blit(_outputRenderTexture, tempTexSPIDs[0]);

            int temp1 = 0;

            for (int index = 0; index < _bleedRange; ++index)
            {
                temp1 = 1 - temp1;
                _bleedCommandBuffer.Blit(tempTexSPIDs[1 - temp1], tempTexSPIDs[temp1], _copyBleedMaterialArray[index]);
            }

            _bleedCommandBuffer.Blit(tempTexSPIDs[temp1], _outputRenderTexture);
            _bleedCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[0]);
            _bleedCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[1]);
            // GenerateCommandBuffer End
        }



        public void MaterialUpdate()
        {
            if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
            {
                _copyFlowResultMaterial.SetFloat("_DisplayNormalAmount", _editorData.GetDisplayNormalAmount());
                _copyFlowResultMaterial.SetFloat("_DisplayNormalLength", _editorData.GetDisplayNormalLength());
            }

            for (int index = 0; index < _bleedRange; ++index)
            {
                _copyBleedMaterialArray[index].SetTexture(_fillTexSPID, _fillRenderTextureArray[index]);
            }

            _copyFlowPaintMaterial.SetTexture(_mainTexSPID, _paintRenderTexture);
            _copyColorPaintMaterial.SetTexture(_mainTexSPID, _paintRenderTexture);
            _copyDensityMaterial.SetTexture(_mainTexSPID, _densityRenderTexture);
            _copyFlowMergeMaterial.SetTexture(_paintTexSPID, _paintRenderTexture);
            _copyFlowMergeMaterial.SetTexture(_densityTexSPID, _densityRenderTexture);
            _copyColorMergeMaterial.SetTexture(_paintTexSPID, _paintRenderTexture);
            _copyColorMergeMaterial.SetTexture(_densityTexSPID, _densityRenderTexture);
            _copyCutoutMaterial.SetTexture(_fillTexSPID, _fillRenderTextureArray[0]);
            _copyFlowResultMaterial.SetTexture(_mainTexSPID, _outputRenderTexture);
            _copyColorResultMaterial.SetTexture(_mainTexSPID, _outputRenderTexture);
        }



        public void PaintProcess(Matrix4x4 matrix)
        {
            bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit raycastHit, 1000f);
            Vector3 hitPosition = raycastHit.point;

            bool leftClick = Input.GetMouseButton(0);
            bool rightClick = Input.GetMouseButton(1);
            bool click = leftClick || rightClick;

            if (!_preHit || !hit || !click)
            {
                _preHitPosition = hitPosition;
            }

            Vector3 paintDirection = hitPosition - _preHitPosition;
            float distance = (hitPosition - Camera.main.transform.position).magnitude;

            if (paintDirection.magnitude > distance * _editorData.GetBrushMoveSensitivity())
            {
                _meshProcess.PaintModeMeshTriangleUpdate(_preHitPosition);

                if (!_prePaint)
                {
                    Graphics.Blit(_outputRenderTexture, _paintRenderTexture);
                    Graphics.Blit(_outputRenderTexture, _preOutputRenderTexture);
                }

                if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
                {
                    paintDirection = _editorData.GetFixedDirection() ? _editorData.GetFixedDirectionVector() : paintDirection;

                    _copyFlowPaintMaterial.SetMatrix(_modelMatrixSPID, matrix);
                    _copyFlowPaintMaterial.SetVector(_hitPositionSPID, _preHitPosition);
                    _copyFlowPaintMaterial.SetFloat(_brushSizeSPID, _editorData.GetBrushSize());
                    _copyFlowPaintMaterial.SetMatrix(_inverseModelMatrixSPID, Matrix4x4.Inverse(matrix));
                    _copyFlowPaintMaterial.SetVector(_paintDirectionSPID, paintDirection);
                    _copyFlowPaintMaterial.SetInt(_fixedHeightSPID, Convert.ToInt32(_editorData.GetFixedHeight()));
                    _copyFlowPaintMaterial.SetFloat(_fixedHeightMinSPID, _editorData.GetFixedHeightMin());
                    _copyFlowPaintMaterial.SetFloat(_fixedHeightMaxSPID, _editorData.GetFixedHeightMax());
                }
                else if (_paintMode == FPT_PaintModeEnum.ColorPaintMode)
                {
                    int editRGBA = (_editorData.GetEditR() ? 1 : 0) + (_editorData.GetEditG() ? 2 : 0) + (_editorData.GetEditB() ? 4 : 0) + (_editorData.GetEditA() ? 8 : 0);

                    _copyColorPaintMaterial.SetMatrix(_modelMatrixSPID, matrix);
                    _copyColorPaintMaterial.SetVector(_hitPositionSPID, _preHitPosition);
                    _copyColorPaintMaterial.SetFloat(_brushSizeSPID, _editorData.GetBrushSize());
                    _copyColorPaintMaterial.SetColor(_PaintColorSPID, _editorData.GetPaintColor());
                    _copyColorPaintMaterial.SetInt(_EditRGBASPID, editRGBA);
                }

                _copyDensityMaterial.SetMatrix(_modelMatrixSPID, matrix);
                _copyDensityMaterial.SetVector(_hitPositionSPID, _preHitPosition);
                _copyDensityMaterial.SetInt(_brushTypeSPID, (int)_editorData.GetBrushType());
                _copyDensityMaterial.SetFloat(_brushSizeSPID, _editorData.GetBrushSize());
                _copyDensityMaterial.SetFloat(_brushStrengthSPID, _editorData.GetBrushStrength());

                Graphics.ExecuteCommandBuffer(_paintCommandBuffer);
                Graphics.ExecuteCommandBuffer(_mergeCommandBuffer);

                _preHitPosition = hitPosition;
                _prePaint = true;
            }

            if (_prePaint && !click)
            {
                Graphics.ExecuteCommandBuffer(_bleedCommandBuffer);

                _paintRenderTexture.Release();
                _densityRenderTexture.Release();

                if (_undoMemoryIndex < _memoryCount - 1)
                {
                    ++_undoMemoryIndex;
                    Graphics.Blit(_outputRenderTexture, _undoMemoryRenderTextureArray[_undoMemoryIndex]);
                }
                else
                {
                    RenderTexture firstMemory = _undoMemoryRenderTextureArray[0];
                    Graphics.Blit(_outputRenderTexture, firstMemory);

                    for (int index = 1; index < _memoryCount; ++index)
                    {
                        _undoMemoryRenderTextureArray[index - 1] = _undoMemoryRenderTextureArray[index];
                    }

                    _undoMemoryRenderTextureArray[_undoMemoryIndex] = firstMemory;
                }

                _redoMemoryIndex = _undoMemoryIndex;

                EditorWindow inspectorWindow = FPT_EditorWindow.GetInspectorWindow(false, null, false);
                inspectorWindow.Repaint();

                _prePaint = false;
            }

            _preHit = hit;
        }



        public void PaintRenderMaterialArray(MeshRenderer meshRenderer)
        {
            Material targetMaterial = null;

            if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
            {
                targetMaterial = _copyFlowResultMaterial;
            }
            else if (_paintMode == FPT_PaintModeEnum.ColorPaintMode)
            {
                targetMaterial = _copyColorResultMaterial;
            }

            meshRenderer.sharedMaterials = Enumerable.Repeat(targetMaterial, _meshProcess.GetSubMeshCount()).ToArray();
        }

        public void MaskRenderMaterialArray(MeshRenderer meshRenderer)
        {
            meshRenderer.sharedMaterials = new Material[] { _assets._material_MaskOff, _assets._material_MaskOn };
        }



        public void Undo()
        {
            if (_undoMemoryIndex <= 0) return;

            --_undoMemoryIndex;
            Graphics.Blit(_undoMemoryRenderTextureArray[_undoMemoryIndex], _outputRenderTexture);
        }

        public void Redo()
        {
            if (_undoMemoryIndex >= _redoMemoryIndex) return;

            ++_undoMemoryIndex;
            Graphics.Blit(_undoMemoryRenderTextureArray[_undoMemoryIndex], _outputRenderTexture);
        }



        public void PreviewGUI()
        {
            EditorGUILayout.LabelField("RenderTexture for preview");
            EditorGUILayout.ObjectField(_outputRenderTexture, typeof(RenderTexture), true);
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

        private void OutputPNG()
        {
            string filePath = EditorUtility.SaveFilePanel("Output PNG File", "Assets", "texture", "png");
            if (string.IsNullOrEmpty(filePath)) return;

            Texture2D copyTexture2D = RenderTextureToTexture2D(_outputRenderTexture);
            File.WriteAllBytes(filePath, copyTexture2D.EncodeToPNG());
            UnityEngine.Object.Destroy(copyTexture2D);

            Debug.Log("output path\n" + filePath);

            string appDataPath = Application.dataPath;

            if (filePath.StartsWith(appDataPath))
            {
                filePath = filePath.Remove(0, appDataPath.Length - 6);
                AssetDatabase.ImportAsset(filePath);
                TextureImporter texImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
                texImporter.sRGBTexture = _actualSRGB;
            }
        }

        public void UndoRedoOutputGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button($"Undo ({_undoMemoryIndex})"))
                {
                    Undo();
                }

                if (GUILayout.Button($"Redo ({_redoMemoryIndex - _undoMemoryIndex})"))
                {
                    Redo();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            if (GUILayout.Button("Output PNG File"))
            {
                OutputPNG();
            }
        }
    }
}