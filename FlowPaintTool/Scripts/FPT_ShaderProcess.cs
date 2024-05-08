#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace FlowPaintTool
{
    using TextData = FPT_Language.FPT_ShaderProcessText;

    public class FPT_ShaderProcess
    {
        private static readonly int _mainTexSPID = Shader.PropertyToID("_MainTex");
        private static readonly int _fillTexSPID = Shader.PropertyToID("_FillTex");
        private static readonly int _paintTexSPID = Shader.PropertyToID("_PaintTex");
        private static readonly int _densityTexSPID = Shader.PropertyToID("_DensityTex");

        private static readonly int _modelMatrixSPID = Shader.PropertyToID("_ModelMatrix");
        private static readonly int _preHitPositionSPID = Shader.PropertyToID("_PreHitPosition");
        private static readonly int _hitPositionSPID = Shader.PropertyToID("_HitPosition");

        private static readonly int _brushSizeSPID = Shader.PropertyToID("_BrushSize");
        private static readonly int _brushStrengthSPID = Shader.PropertyToID("_BrushStrength");
        private static readonly int _brushTypeSPID = Shader.PropertyToID("_BrushType");

        private static readonly int _inverseModelMatrixSPID = Shader.PropertyToID("_InverseModelMatrix");
        private static readonly int _paintDirectionSPID = Shader.PropertyToID("_PaintDirection");
        private static readonly int _fixedHeightSPID = Shader.PropertyToID("_FixedHeight");
        private static readonly int _fixedHeightMinSPID = Shader.PropertyToID("_FixedHeightMin");
        private static readonly int _fixedHeightMaxSPID = Shader.PropertyToID("_FixedHeightMax");

        private static readonly int _paintColorSPID = Shader.PropertyToID("_PaintColor");
        private static readonly int _editRGBASPID = Shader.PropertyToID("_EditRGBA");

        private string _outputRenderTexturePath = string.Empty;
        private RenderTexture _outputRenderTexture = null;
        private RenderTexture _preOutputRenderTexture = null;
        private RenderTexture[] _fillRenderTextureArray = null;
        private RenderTexture _paintRenderTexture = null;
        private RenderTexture _densityRenderTexture = null;
        private RenderTexture[] _undoMemoryRenderTextureArray = null;

        private Material _copyTargetPaintMaterial = null;
        private Material _copyDensityMaterial = null;
        private Material _copyTargetMergeMaterial = null;
        private Material[] _copyBleedMaterialArray = null;
        private Material _copyFlowResultMaterial = null;
        private Material _copyColorResultMaterial = null;

        private CommandBuffer _paintCommandBuffer = null;
        private CommandBuffer _bleedCommandBuffer = null;

        private FPT_MeshProcess _meshProcess = null;
        private FPT_PaintModeEnum _paintMode = FPT_PaintModeEnum.FlowPaintMode;
        private int _bleedRange = 4;
        private bool _actualSRGB = false;
        private TextureImporter _sourceTextureImporter = null;

        private Vector3 _preHitPosition = Vector3.zero;
        private bool _prePaint = false;
        private bool _preHit = false;
        private int _memoryCount = 0;
        private int _undoMemoryIndex = 0;
        private int _redoMemoryIndex = 0;

        private FPT_Main _fptMain = null;

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

        public FPT_ShaderProcess(FPT_Main fptMain, FPT_MainData fptData, FPT_MeshProcess meshProcess, int InstanceID)
        {
            _fptMain = fptMain;

            FPT_Assets assets = FPT_Assets.GetStaticInstance();
            Material fillMaterial = assets.GetFillMaterial();

            _meshProcess = meshProcess;
            _paintMode = fptData._paintMode;
            _bleedRange = fptData._bleedRange;
            _actualSRGB = fptData._actualSRGB;
            _memoryCount = fptData._maxUndoCount + 1;

            // GenerateRenderTexture Start
            int[] tempTexSPIDs = new int[] { Shader.PropertyToID("_TempTex0"), Shader.PropertyToID("_TempTex1") };

            GraphicsFormat graphicsFormat = _actualSRGB ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
            RenderTextureDescriptor rtd_main = new RenderTextureDescriptor(fptData._width, fptData._height, graphicsFormat, 0);
            RenderTextureDescriptor rtd_R8 = rtd_main;
            rtd_R8.graphicsFormat = GraphicsFormat.R8_UNorm;
            RenderTextureDescriptor rtd_R16 = rtd_main;
            rtd_R16.graphicsFormat = GraphicsFormat.R16_UNorm;

            _outputRenderTexture = new RenderTexture(rtd_main);
            {
                _outputRenderTexture.filterMode = FilterMode.Point;

                string path = Path.GetDirectoryName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(fillMaterial)));
                path = Path.Combine(path, $"RT{InstanceID}.renderTexture");
                AssetDatabase.CreateAsset(_outputRenderTexture, path);
                _outputRenderTexturePath = path;

                EditorApplication.playModeStateChanged += RemoveOutputRenderTexture;

                if (fptData._textureExist)
                {
                    if (fptData._startTextureType == FPT_StartTextureLoadModeEnum.Assets)
                    {
                        _sourceTextureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(fptData._startTexture)) as TextureImporter;

                        if (_sourceTextureImporter.textureType == TextureImporterType.NormalMap)
                        {
                            Graphics.Blit(fptData._startTexture, _outputRenderTexture, assets.GetUnpackNormalMaterial());
                        }
                        else
                        {
                            Graphics.Blit(fptData._startTexture, _outputRenderTexture);
                        }
                    }
                    else if (fptData._startTextureType == FPT_StartTextureLoadModeEnum.FilePath)
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

                    Color defaultColor = default;

                    if (fptData._paintMode == FPT_PaintModeEnum.FlowPaintMode)
                    {
                        defaultColor = new Color(0.5f, 0.5f, 1f, 1f);
                    }
                    else if (fptData._paintMode == FPT_PaintModeEnum.ColorPaintMode)
                    {
                        defaultColor = Color.black;
                    }

                    Texture2D defaultColorTexture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
                    defaultColorTexture.SetPixel(0, 0, defaultColor);
                    defaultColorTexture.Apply();
                    Graphics.Blit(defaultColorTexture, _outputRenderTexture);
                    UnityEngine.Object.Destroy(defaultColorTexture);
                }
            }

            _fillRenderTextureArray = Enumerable.Range(0, Math.Max(_bleedRange, 1)).Select(I => new RenderTexture(rtd_R8)).ToArray();
            {
                TargetUVChannel(fptData, fillMaterial);
                int subMeshCount = fptData._startMesh.subMeshCount;

                CommandBuffer fillCommandBuffer = new CommandBuffer();
                fillCommandBuffer.GetTemporaryRT(tempTexSPIDs[0], rtd_R8);
                fillCommandBuffer.SetRenderTarget(tempTexSPIDs[0]);

                for (int subMeshIndex = 0; subMeshIndex < subMeshCount; ++subMeshIndex)
                {
                    fillCommandBuffer.DrawMesh(fptData._startMesh, Matrix4x4.identity, fillMaterial, subMeshIndex);
                }

                fillCommandBuffer.Blit(tempTexSPIDs[0], _fillRenderTextureArray[0], assets.GetFillBleedMaterial());
                fillCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[0]);

                Graphics.ExecuteCommandBuffer(fillCommandBuffer);
                fillCommandBuffer.Dispose();

                for (int index = 0; index < (_bleedRange - 1); ++index)
                {
                    Graphics.Blit(_fillRenderTextureArray[index], _fillRenderTextureArray[index + 1], assets.GetFillBleedMaterial());
                }
            }

            _paintRenderTexture = new RenderTexture(rtd_main);
            _densityRenderTexture = new RenderTexture(rtd_R16);

            _preOutputRenderTexture = new RenderTexture(rtd_main);
            Graphics.Blit(_outputRenderTexture, _preOutputRenderTexture);

            _undoMemoryRenderTextureArray = Enumerable.Range(0, _memoryCount).Select(I => new RenderTexture(rtd_main)).ToArray();
            Graphics.Blit(_outputRenderTexture, _undoMemoryRenderTextureArray[0]);
            // GenerateRenderTexture End

            // GenerateMaterial Start
            _copyBleedMaterialArray = Enumerable.Range(0, _bleedRange).Select(I => UnityEngine.Object.Instantiate(assets.GetBleedMaterial())).ToArray();

            if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
            {
                _copyTargetPaintMaterial = UnityEngine.Object.Instantiate(assets.GetFlowPaintMaterial());
                _copyTargetMergeMaterial = UnityEngine.Object.Instantiate(assets.GetFlowMergeMaterial());
            }
            else if (_paintMode == FPT_PaintModeEnum.ColorPaintMode)
            {
                _copyTargetPaintMaterial = UnityEngine.Object.Instantiate(assets.GetColorPaintMaterial());
                _copyTargetMergeMaterial = UnityEngine.Object.Instantiate(assets.GetColorMergeMaterial());
            }

            _copyDensityMaterial = UnityEngine.Object.Instantiate(assets.GetDensityMaterial());
            _copyFlowResultMaterial = UnityEngine.Object.Instantiate(assets.GetFlowResultMaterial());
            _copyColorResultMaterial = UnityEngine.Object.Instantiate(assets.GetColorResultMaterial());

            TargetUVChannel(fptData, _copyTargetPaintMaterial);
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

            _paintCommandBuffer = new CommandBuffer();
            _paintCommandBuffer.GetTemporaryRT(tempTexSPIDs[0], rtd_main);
            _paintCommandBuffer.GetTemporaryRT(tempTexSPIDs[1], rtd_R16);
            _paintCommandBuffer.Blit(_paintRenderTexture, tempTexSPIDs[0]);
            _paintCommandBuffer.SetRenderTarget(tempTexSPIDs[0]);
            _paintCommandBuffer.DrawMesh(paintModeMesh, Matrix4x4.identity, _copyTargetPaintMaterial, 0);
            _paintCommandBuffer.Blit(tempTexSPIDs[0], _paintRenderTexture);
            _paintCommandBuffer.Blit(_densityRenderTexture, tempTexSPIDs[1]);
            _paintCommandBuffer.SetRenderTarget(tempTexSPIDs[1]);
            _paintCommandBuffer.DrawMesh(paintModeMesh, Matrix4x4.identity, _copyDensityMaterial, 0);
            _paintCommandBuffer.Blit(tempTexSPIDs[1], _densityRenderTexture);
            _paintCommandBuffer.Blit(_preOutputRenderTexture, _outputRenderTexture, _copyTargetMergeMaterial);
            _paintCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[0]);
            _paintCommandBuffer.ReleaseTemporaryRT(tempTexSPIDs[1]);

            _bleedCommandBuffer = new CommandBuffer();
            _bleedCommandBuffer.GetTemporaryRT(tempTexSPIDs[0], rtd_main);
            _bleedCommandBuffer.GetTemporaryRT(tempTexSPIDs[1], rtd_main);
            _bleedCommandBuffer.Blit(_outputRenderTexture, tempTexSPIDs[0]);

            int temp1 = 0;

            for (int index = 0; index < _bleedRange; ++index)
            {
                _bleedCommandBuffer.Blit(tempTexSPIDs[temp1], tempTexSPIDs[1 - temp1], _copyBleedMaterialArray[index]);
                temp1 = 1 - temp1;
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
                _copyFlowResultMaterial.SetFloat("_DisplayNormalAmount", FPT_EditorData.GetStaticInstance().GetDisplayNormalAmount());
                _copyFlowResultMaterial.SetFloat("_DisplayNormalLength", FPT_EditorData.GetStaticInstance().GetDisplayNormalLength());
            }

            for (int index = 0; index < _bleedRange; ++index)
            {
                _copyBleedMaterialArray[index].SetTexture(_fillTexSPID, _fillRenderTextureArray[index]);
            }

            _copyTargetPaintMaterial.SetTexture(_mainTexSPID, _paintRenderTexture);
            _copyDensityMaterial.SetTexture(_mainTexSPID, _densityRenderTexture);
            _copyTargetMergeMaterial.SetTexture(_paintTexSPID, _paintRenderTexture);
            _copyTargetMergeMaterial.SetTexture(_densityTexSPID, _densityRenderTexture);
            _copyFlowResultMaterial.SetTexture(_mainTexSPID, _outputRenderTexture);
            _copyColorResultMaterial.SetTexture(_mainTexSPID, _outputRenderTexture);
        }



        public void PaintProcess(Matrix4x4 matrix)
        {
            bool hit = _fptMain.PaintToolRaycast(out RaycastHit raycastHit);
            Vector3 hitPosition = raycastHit.point;

            bool leftClick = Input.GetMouseButton(0);
            bool rightClick = false; // Input.GetMouseButton(1);
            bool click = leftClick || rightClick;

            if (!_preHit || !hit || !click)
            {
                _preHitPosition = hitPosition;
            }

            FPT_EditorData editorData = FPT_EditorData.GetStaticInstance();

            Vector3 paintDirection = hitPosition - _preHitPosition;
            float distance = (hitPosition - FPT_Main.GetCamera().transform.position).magnitude;

            if (paintDirection.magnitude > distance * editorData.GetBrushMoveSensitivity())
            {
                _meshProcess.PaintModeMeshTriangleUpdate(_preHitPosition);

                if (!_prePaint)
                {
                    Graphics.Blit(_outputRenderTexture, _preOutputRenderTexture);
                }

                _copyTargetPaintMaterial.SetMatrix(_modelMatrixSPID, matrix);
                _copyTargetPaintMaterial.SetVector(_preHitPositionSPID, _preHitPosition);
                _copyTargetPaintMaterial.SetVector(_hitPositionSPID, hitPosition);
                _copyTargetPaintMaterial.SetFloat(_brushSizeSPID, editorData.GetBrushSize());

                if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
                {
                    paintDirection = editorData.GetFixedDirection() ? editorData.GetFixedDirectionVector() : paintDirection;

                    _copyTargetPaintMaterial.SetMatrix(_inverseModelMatrixSPID, Matrix4x4.Inverse(matrix));
                    _copyTargetPaintMaterial.SetVector(_paintDirectionSPID, paintDirection);
                    _copyTargetPaintMaterial.SetInt(_fixedHeightSPID, Convert.ToInt32(editorData.GetHeightLimit()));
                    _copyTargetPaintMaterial.SetFloat(_fixedHeightMinSPID, editorData.GetMinHeight());
                    _copyTargetPaintMaterial.SetFloat(_fixedHeightMaxSPID, editorData.GetMaxHeight());
                }
                else if (_paintMode == FPT_PaintModeEnum.ColorPaintMode)
                {
                    int editRGBA = (editorData.GetEditR() ? 1 : 0) + (editorData.GetEditG() ? 2 : 0) + (editorData.GetEditB() ? 4 : 0) + (editorData.GetEditA() ? 8 : 0);

                    _copyTargetPaintMaterial.SetColor(_paintColorSPID, editorData.GetPaintColor());
                    _copyTargetPaintMaterial.SetInt(_editRGBASPID, editRGBA);
                }

                _copyDensityMaterial.SetMatrix(_modelMatrixSPID, matrix);
                _copyDensityMaterial.SetVector(_preHitPositionSPID, _preHitPosition);
                _copyDensityMaterial.SetVector(_hitPositionSPID, hitPosition);
                _copyDensityMaterial.SetFloat(_brushSizeSPID, editorData.GetBrushSize());
                _copyDensityMaterial.SetFloat(_brushStrengthSPID, editorData.GetBrushStrength());
                _copyDensityMaterial.SetInt(_brushTypeSPID, (int)editorData.GetBrushShape());

                Graphics.ExecuteCommandBuffer(_paintCommandBuffer);
                Graphics.ExecuteCommandBuffer(_bleedCommandBuffer);

                _preHitPosition = hitPosition;
                _prePaint = true;
            }

            if (_prePaint && !click)
            {
                _paintRenderTexture.Release();
                _densityRenderTexture.Release();

                int maxIndex = _memoryCount - 1;

                if (_undoMemoryIndex < maxIndex)
                {
                    ++_undoMemoryIndex;
                    Graphics.Blit(_outputRenderTexture, _undoMemoryRenderTextureArray[_undoMemoryIndex]);
                }
                else
                {
                    RenderTexture tempRT = _undoMemoryRenderTextureArray[0];

                    for (int index = 0; index < maxIndex; ++index)
                    {
                        _undoMemoryRenderTextureArray[index] = _undoMemoryRenderTextureArray[index + 1];
                    }

                    _undoMemoryRenderTextureArray[maxIndex] = tempRT;

                    Graphics.Blit(_outputRenderTexture, tempRT);
                }

                _redoMemoryIndex = _undoMemoryIndex;

                FPT_EditorWindow.RepaintInspectorWindow();

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



        public void RenderTextureUndo()
        {
            if (_undoMemoryIndex <= 0) return;

            --_undoMemoryIndex;
            Graphics.Blit(_undoMemoryRenderTextureArray[_undoMemoryIndex], _outputRenderTexture);
            FPT_EditorWindow.RepaintInspectorWindow();
        }

        public void RenderTextureRedo()
        {
            if (_undoMemoryIndex >= _redoMemoryIndex) return;

            ++_undoMemoryIndex;
            Graphics.Blit(_undoMemoryRenderTextureArray[_undoMemoryIndex], _outputRenderTexture);
            FPT_EditorWindow.RepaintInspectorWindow();
        }



        public void PreviewGUI()
        {
            EditorGUILayout.LabelField(TextData.RenderTextureForPreview);
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
            string filePath = EditorUtility.SaveFilePanel(TextData.OutputPNGFile, "Assets", "texture", "png");
            if (string.IsNullOrEmpty(filePath)) return;

            Texture2D copyTexture2D = RenderTextureToTexture2D(_outputRenderTexture);
            File.WriteAllBytes(filePath, copyTexture2D.EncodeToPNG());
            UnityEngine.Object.Destroy(copyTexture2D);

            Debug.Log(TextData.OutputPath + filePath);

            string appDataPath = Application.dataPath;

            if (filePath.StartsWith(appDataPath))
            {
                filePath = filePath.Remove(0, appDataPath.Length - 6);
                AssetDatabase.ImportAsset(filePath);
                TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
                importer.sRGBTexture = _actualSRGB;
                int max = Math.Max(_outputRenderTexture.width, _outputRenderTexture.height);
                importer.maxTextureSize = (int)Math.Pow(2, Math.Ceiling(Math.Log(max, 2)));

                if (_sourceTextureImporter != null)
                {
                    importer.textureType = _sourceTextureImporter.textureType;
                }
            }
        }

        public void UndoRedoOutputGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(TextData.Undo + $" ({_undoMemoryIndex})"))
                {
                    RenderTextureUndo();
                }

                if (GUILayout.Button(TextData.Redo + $" ({_redoMemoryIndex - _undoMemoryIndex})"))
                {
                    RenderTextureRedo();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            if (GUILayout.Button(TextData.OutputPNGFile))
            {
                OutputPNG();
            }
        }
    }
}

#endif