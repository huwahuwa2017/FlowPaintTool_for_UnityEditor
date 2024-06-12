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

        private static readonly int[] _tempRT_SPIDs = new int[]
        {
            Shader.PropertyToID("_TempTex0"),
            Shader.PropertyToID("_TempTex1")
        };

        private static readonly RenderTargetIdentifier[] _tempRT_RTIDs = new RenderTargetIdentifier[]
        {
            new RenderTargetIdentifier(_tempRT_SPIDs[0]),
            new RenderTargetIdentifier(_tempRT_SPIDs[1])
        };

        private string _outputRenderTexturePath = string.Empty;
        private RenderTexture _outputRenderTexture = null;
        private RenderTexture _preOutputRenderTexture = null;
        private RenderTexture[] _fillRenderTextureArray = null;
        private RenderTexture _paintRenderTexture = null;
        private RenderTexture _densityRenderTexture = null;
        private RenderTexture[] _undoMemoryRenderTextureArray = null;

        private Material _copyTargetPaintMaterial = null;
        private Material _copyTargetMergeMaterial = null;
        private Material[] _copyBleedMaterialArray = null;
        private Material _copyFlowResultMaterial = null;
        private Material _copyColorResultMaterial = null;

        private CommandBuffer _paintCommandBuffer = null;
        private CommandBuffer _bleedCommandBuffer = null;

        private FPT_PaintModeEnum _paintMode = FPT_PaintModeEnum.FlowPaintMode;
        private int _bleedRange = 4;
        private bool _actualSRGB = false;
        private int _memoryCount = 0;

        private Vector3 _preHitPosition = Vector3.zero;
        private bool _drawing = false;
        private bool _preHit = false;

        private int _undoMemoryIndex = 0;
        private int _redoMemoryIndex = 0;

        private FPT_Main _fptMain = null;
        private FPT_MeshProcess _meshProcess = null;

        private void RemoveOutputRenderTexture(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= RemoveOutputRenderTexture;

                AssetDatabase.DeleteAsset(_outputRenderTexturePath);
            }
        }

        private void TargetUVChannel(Material mat, int targetUVChannel)
        {
            mat.DisableKeyword("UV_CHANNEL_0");
            mat.DisableKeyword("UV_CHANNEL_1");
            mat.DisableKeyword("UV_CHANNEL_2");
            mat.DisableKeyword("UV_CHANNEL_3");
            mat.DisableKeyword("UV_CHANNEL_4");
            mat.DisableKeyword("UV_CHANNEL_5");
            mat.DisableKeyword("UV_CHANNEL_6");
            mat.DisableKeyword("UV_CHANNEL_7");

            mat.EnableKeyword("UV_CHANNEL_" + targetUVChannel);
        }

        public FPT_ShaderProcess(FPT_Main fptMain, FPT_MainData fptData, FPT_MeshProcess meshProcess, int InstanceID)
        {
            _paintMode = fptData._paintMode;
            _bleedRange = fptData._bleedRange;
            _actualSRGB = fptData._actualSRGB;
            _memoryCount = fptData._maxUndoCount + 1;
            _fptMain = fptMain;
            _meshProcess = meshProcess;

            FPT_Assets assets = FPT_Assets.GetSingleton();
            Material fillMaterial = assets.GetFillMaterial();

            // GenerateRenderTexture Start
            GraphicsFormat graphicsFormat = _actualSRGB ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
            RenderTextureDescriptor rtd_main = new RenderTextureDescriptor(fptData._width, fptData._height, graphicsFormat, 0);
            RenderTextureDescriptor rtd_R8 = new RenderTextureDescriptor(fptData._width, fptData._height, GraphicsFormat.R8_UNorm, 0);
            RenderTextureDescriptor rtd_R16 = new RenderTextureDescriptor(fptData._width, fptData._height, GraphicsFormat.R16_UNorm, 0);

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
                    if (fptData._startTextureLoadMode == FPT_StartTextureLoadModeEnum.Assets)
                    {
                        TextureImporter sourceTextureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(fptData._startTexture)) as TextureImporter;

                        if (sourceTextureImporter.textureType == TextureImporterType.NormalMap)
                        {
                            Graphics.Blit(fptData._startTexture, _outputRenderTexture, assets.GetUnpackNormalMaterial());
                        }
                        else
                        {
                            Graphics.Blit(fptData._startTexture, _outputRenderTexture);
                        }
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
                    if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
                    {
                        FPT_TextureOperation.ClearRenderTexture(_outputRenderTexture, new Color(0.5f, 0.5f, 1.0f));
                    }
                    else if (_paintMode == FPT_PaintModeEnum.ColorPaintMode)
                    {
                        FPT_TextureOperation.ClearRenderTexture(_outputRenderTexture, Color.black);
                    }
                }
            }

            _fillRenderTextureArray = Enumerable.Range(0, Math.Max(_bleedRange, 1)).Select(I => new RenderTexture(rtd_R8)).ToArray();
            {
                TargetUVChannel(fillMaterial, fptData._targetUVChannel);

                CommandBuffer fillCommandBuffer = new CommandBuffer();
                fillCommandBuffer.GetTemporaryRT(_tempRT_SPIDs[0], rtd_R8);
                fillCommandBuffer.SetRenderTarget(_tempRT_SPIDs[0]);
                fillCommandBuffer.DrawMesh(fptData._startMesh, Matrix4x4.identity, fillMaterial, fptData._targetSubMesh);
                fillCommandBuffer.Blit(_tempRT_SPIDs[0], _fillRenderTextureArray[0], assets.GetFillBleedMaterial());
                fillCommandBuffer.ReleaseTemporaryRT(_tempRT_SPIDs[0]);

                Graphics.ExecuteCommandBuffer(fillCommandBuffer);
                fillCommandBuffer.Dispose();

                for (int index = 0; index < (_bleedRange - 1); ++index)
                {
                    Graphics.Blit(_fillRenderTextureArray[index], _fillRenderTextureArray[index + 1], assets.GetFillBleedMaterial());
                }
            }

            _preOutputRenderTexture = new RenderTexture(rtd_main);
            _paintRenderTexture = new RenderTexture(rtd_main);
            _densityRenderTexture = new RenderTexture(rtd_R16);

            Graphics.Blit(_outputRenderTexture, _preOutputRenderTexture);
            Graphics.Blit(_outputRenderTexture, _paintRenderTexture);
            FPT_TextureOperation.ClearRenderTexture(_densityRenderTexture, Color.clear);

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

            _copyFlowResultMaterial = UnityEngine.Object.Instantiate(assets.GetFlowResultMaterial());
            _copyColorResultMaterial = UnityEngine.Object.Instantiate(assets.GetColorResultMaterial());

            TargetUVChannel(_copyTargetPaintMaterial, fptData._targetUVChannel);
            TargetUVChannel(_copyFlowResultMaterial, fptData._targetUVChannel);
            TargetUVChannel(_copyColorResultMaterial, fptData._targetUVChannel);

            if (_actualSRGB)
            {
                _copyColorResultMaterial.EnableKeyword("IS_SRGB");
            }
            else
            {
                _copyColorResultMaterial.DisableKeyword("IS_SRGB");
            }
            // GenerateMaterial End

            // GenerateCommandBuffer Start
            Mesh paintModeMesh = _meshProcess.GetPaintModeMesh();

            _paintCommandBuffer = new CommandBuffer();
            _paintCommandBuffer.GetTemporaryRT(_tempRT_SPIDs[0], rtd_main);
            _paintCommandBuffer.GetTemporaryRT(_tempRT_SPIDs[1], rtd_R16);
            _paintCommandBuffer.Blit(_paintRenderTexture, _tempRT_SPIDs[0]);
            _paintCommandBuffer.Blit(_densityRenderTexture, _tempRT_SPIDs[1]);
            _paintCommandBuffer.SetRenderTarget(_tempRT_RTIDs, _tempRT_RTIDs[0]);
            _paintCommandBuffer.DrawMesh(paintModeMesh, Matrix4x4.identity, _copyTargetPaintMaterial, 0);
            _paintCommandBuffer.Blit(_tempRT_SPIDs[0], _paintRenderTexture);
            _paintCommandBuffer.Blit(_tempRT_SPIDs[1], _densityRenderTexture);
            _paintCommandBuffer.Blit(_preOutputRenderTexture, _outputRenderTexture, _copyTargetMergeMaterial);
            _paintCommandBuffer.ReleaseTemporaryRT(_tempRT_SPIDs[0]);
            _paintCommandBuffer.ReleaseTemporaryRT(_tempRT_SPIDs[1]);

            _bleedCommandBuffer = new CommandBuffer();
            _bleedCommandBuffer.GetTemporaryRT(_tempRT_SPIDs[0], rtd_main);
            _bleedCommandBuffer.GetTemporaryRT(_tempRT_SPIDs[1], rtd_main);
            _bleedCommandBuffer.Blit(_outputRenderTexture, _tempRT_SPIDs[0]);

            int temp1 = 0;

            for (int index = 0; index < _bleedRange; ++index)
            {
                _bleedCommandBuffer.Blit(_tempRT_SPIDs[temp1], _tempRT_SPIDs[1 - temp1], _copyBleedMaterialArray[index]);
                temp1 = 1 - temp1;
            }

            _bleedCommandBuffer.Blit(_tempRT_SPIDs[temp1], _outputRenderTexture);
            _bleedCommandBuffer.ReleaseTemporaryRT(_tempRT_SPIDs[0]);
            _bleedCommandBuffer.ReleaseTemporaryRT(_tempRT_SPIDs[1]);
            // GenerateCommandBuffer End
        }



        public void MaterialUpdate()
        {
            if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
            {
                _copyFlowResultMaterial.SetFloat("_DisplayNormalAmount", FPT_EditorData.GetSingleton().GetDisplayNormalAmount());
                _copyFlowResultMaterial.SetFloat("_DisplayNormalLength", FPT_EditorData.GetSingleton().GetDisplayNormalLength());
            }

            for (int index = 0; index < _bleedRange; ++index)
            {
                _copyBleedMaterialArray[index].SetTexture(_fillTexSPID, _fillRenderTextureArray[index]);
            }

            _copyTargetPaintMaterial.SetTexture(_paintTexSPID, _paintRenderTexture);
            _copyTargetPaintMaterial.SetTexture(_densityTexSPID, _densityRenderTexture);
            _copyTargetMergeMaterial.SetTexture(_paintTexSPID, _paintRenderTexture);
            _copyTargetMergeMaterial.SetTexture(_densityTexSPID, _densityRenderTexture);
            _copyFlowResultMaterial.SetTexture(_mainTexSPID, _outputRenderTexture);
            _copyColorResultMaterial.SetTexture(_mainTexSPID, _outputRenderTexture);
        }



        private Color GammaToLinearSpace(Color color)
        {
            float r = color.r;
            float g = color.g;
            float b = color.b;

            color.r = r * (r * (r * 0.305306011f + 0.682171111f) + 0.012522878f);
            color.g = g * (g * (g * 0.305306011f + 0.682171111f) + 0.012522878f);
            color.b = b * (b * (b * 0.305306011f + 0.682171111f) + 0.012522878f);

            return color;
        }

        public void PaintProcess(Matrix4x4 matrix)
        {
            bool hit = _fptMain.PaintToolRaycast(out Vector3 hitPosition);

            FPT_Core.GetSingleton().MoveRangeVisualizar(hit, hitPosition);

            bool leftClick = Input.GetMouseButton(0);
            bool rightClick = false; // Input.GetMouseButton(1);
            bool click = leftClick || rightClick;

            if (!_preHit || !hit || !click)
            {
                _preHitPosition = hitPosition;
            }

            FPT_EditorData editorData = FPT_EditorData.GetSingleton();

            Vector3 paintDirection = hitPosition - _preHitPosition;
            float distance = (hitPosition - FPT_Main.GetCamera().transform.position).magnitude;
            bool flowPaintDraw = paintDirection.magnitude > distance * editorData.GetBrushMoveSensitivity();
            flowPaintDraw &= _paintMode == FPT_PaintModeEnum.FlowPaintMode;

            bool colorPaintDraw = (click && !_drawing) || (_preHitPosition != hitPosition);
            colorPaintDraw &= _paintMode == FPT_PaintModeEnum.ColorPaintMode;

            if (colorPaintDraw || flowPaintDraw)
            {
                _meshProcess.PaintModeMeshTriangleUpdate(_preHitPosition);

                _copyTargetPaintMaterial.SetMatrix(_modelMatrixSPID, matrix);
                _copyTargetPaintMaterial.SetVector(_preHitPositionSPID, _preHitPosition);
                _copyTargetPaintMaterial.SetVector(_hitPositionSPID, hitPosition);
                _copyTargetPaintMaterial.SetFloat(_brushSizeSPID, editorData.GetBrushSize());

                _copyTargetPaintMaterial.SetFloat(_brushStrengthSPID, editorData.GetBrushStrength());
                _copyTargetPaintMaterial.SetInt(_brushTypeSPID, (int)editorData.GetBrushShape());

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

                    Color paintColor = editorData.GetPaintColor();

                    if (_actualSRGB)
                    {
                        paintColor = GammaToLinearSpace(paintColor);
                    }

                    _copyTargetPaintMaterial.SetColor(_paintColorSPID, paintColor);
                    _copyTargetPaintMaterial.SetInt(_editRGBASPID, editRGBA);
                }

                Graphics.ExecuteCommandBuffer(_paintCommandBuffer);

                _preHitPosition = hitPosition;
                _drawing = true;
            }

            if (_drawing && !click)
            {
                Graphics.ExecuteCommandBuffer(_bleedCommandBuffer);

                Graphics.Blit(_outputRenderTexture, _preOutputRenderTexture);
                Graphics.Blit(_outputRenderTexture, _paintRenderTexture);
                FPT_TextureOperation.ClearRenderTexture(_densityRenderTexture, Color.clear);

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

                _drawing = false;
            }

            _preHit = hit;
        }



        public Material GetPaintRenderMaterial()
        {
            Material material = null;

            if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
            {
                material = _copyFlowResultMaterial;
            }
            else if (_paintMode == FPT_PaintModeEnum.ColorPaintMode)
            {
                material = _copyColorResultMaterial;
            }

            return material;
        }



        public void RenderTextureUndo()
        {
            if (_undoMemoryIndex <= 0) return;

            --_undoMemoryIndex;
            Graphics.Blit(_undoMemoryRenderTextureArray[_undoMemoryIndex], _outputRenderTexture);
            Graphics.Blit(_outputRenderTexture, _preOutputRenderTexture);
            Graphics.Blit(_outputRenderTexture, _paintRenderTexture);
            FPT_EditorWindow.RepaintInspectorWindow();
        }

        public void RenderTextureRedo()
        {
            if (_undoMemoryIndex >= _redoMemoryIndex) return;

            ++_undoMemoryIndex;
            Graphics.Blit(_undoMemoryRenderTextureArray[_undoMemoryIndex], _outputRenderTexture);
            Graphics.Blit(_outputRenderTexture, _preOutputRenderTexture);
            Graphics.Blit(_outputRenderTexture, _paintRenderTexture);
            FPT_EditorWindow.RepaintInspectorWindow();
        }



        public void PreviewGUI()
        {
            EditorGUILayout.LabelField(TextData.RenderTextureForPreview);
            EditorGUILayout.ObjectField(_outputRenderTexture, typeof(RenderTexture), true);
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
                FPT_TextureOperation.OpenDialog(_outputRenderTexture, TextData.OutputPNGFile);
            }
        }
    }
}

#endif