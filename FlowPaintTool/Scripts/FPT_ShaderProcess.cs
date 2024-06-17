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
    using TextData_Mask = FPT_Language.FPT_MaskText;

    public class FPT_ShaderProcess
    {
        private static readonly int _mainTexSPID = Shader.PropertyToID("_MainTex");
        private static readonly int _fillTexSPID = Shader.PropertyToID("_FillTex");
        private static readonly int _paintTexSPID = Shader.PropertyToID("_PaintTex");
        private static readonly int _densityTexSPID = Shader.PropertyToID("_DensityTex");
        private static readonly int _polygonMaskTexSPID = Shader.PropertyToID("_PolygonMaskTex");
        private static readonly int _polygonThinningTexSPID = Shader.PropertyToID("_PolygonThinningTex");

        private static readonly int _preHitPositionSPID = Shader.PropertyToID("_PreHitPosition");
        private static readonly int _hitPositionSPID = Shader.PropertyToID("_HitPosition");

        private static readonly int _brushSizeSPID = Shader.PropertyToID("_BrushSize");
        private static readonly int _brushStrengthSPID = Shader.PropertyToID("_BrushStrength");
        private static readonly int _brushTypeSPID = Shader.PropertyToID("_BrushType");

        private static readonly int _paintDirectionSPID = Shader.PropertyToID("_PaintDirection");
        private static readonly int _fixedHeightSPID = Shader.PropertyToID("_FixedHeight");
        private static readonly int _fixedHeightMinSPID = Shader.PropertyToID("_FixedHeightMin");
        private static readonly int _fixedHeightMaxSPID = Shader.PropertyToID("_FixedHeightMax");

        private static readonly int _paintColorSPID = Shader.PropertyToID("_PaintColor");
        private static readonly int _editRGBASPID = Shader.PropertyToID("_EditRGBA");

        private static readonly int _changeMaskSPID = Shader.PropertyToID("_ChangeMask");

        private static readonly int _textureSizeSPID = Shader.PropertyToID("_TextureSize");

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

        private RenderTexture _polygonMaskRT = null;
        private RenderTexture _sqrMagnitudeRT = null;
        private Texture2D _polygonMaskT2D = null;
        private Texture2D _sqrMagnitudeT2D = null;
        private Texture2D _polygonThinningT2D = null;

        private Material[] _copyBleedMaterialArray = null;
        private Material _copyTargetPaintMaterial = null;
        private Material _copyTargetMergeMaterial = null;
        private Material _copyTargetResultMaterial = null;
        private Material _copySqrMagnitudeMaterial = null;
        private Material _copyPolygonMaskMaterial = null;
        private Material _copyPolygonMaskResultMaterial = null;

        private CommandBuffer _paintCommandBuffer = null;
        private CommandBuffer _bleedCommandBuffer = null;
        private CommandBuffer _polygonMaskCommandBuffer = null;
        private CommandBuffer _sqrMagnitudeCommandBuffer = null;

        private FPT_PaintModeEnum _paintMode = FPT_PaintModeEnum.FlowPaintMode;
        private int _bleedRange = 4;
        private bool _actualSRGB = false;
        private int _memoryCount = 1;

        private Vector3 _preHitPosition = Vector3.zero;
        private bool _drawing = false;
        private bool _preHit = false;

        private int _undoMemoryIndex = 0;
        private int _redoMemoryIndex = 0;

        private int _polygonDataTexSize = 0;

        private FPT_MeshProcess _meshProcess = null;
        private FPT_Raycast _raycast = null;
        private Renderer _sorceRenderer = null;
        private int[] _subMeshIndexArray = null;

        public Material GetResultMaterial() => _copyTargetResultMaterial;

        public Material GetPolygonMaskResultMaterial() => _copyPolygonMaskResultMaterial;

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

        public FPT_ShaderProcess(FPT_MainData fptData, int instanceID)
        {
            _meshProcess = new FPT_MeshProcess(fptData);
            _raycast = new FPT_Raycast();

            _sorceRenderer = fptData._sorceRenderer;
            _paintMode = fptData._paintMode;
            _bleedRange = fptData._bleedRange;
            _actualSRGB = fptData._actualSRGB;
            _memoryCount = fptData._maxUndoCount + 1;

            _subMeshIndexArray = Enumerable.Range(0, fptData._startMesh.subMeshCount).ToArray();

            int dataCount = fptData._startMesh.GetTriangles(fptData._targetSubMesh).Length / 3;
            _polygonDataTexSize = (int)Math.Ceiling(Math.Sqrt(dataCount));

            FPT_Assets assets = FPT_Assets.GetSingleton();
            Material fillMaterial = assets.GetFillMaterial();

            // GenerateRenderTexture Start
            GraphicsFormat graphicsFormat = _actualSRGB ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
            RenderTextureDescriptor rtd_main = new RenderTextureDescriptor(fptData._width, fptData._height, graphicsFormat, 0);
            RenderTextureDescriptor rtd_R8 = new RenderTextureDescriptor(fptData._width, fptData._height, GraphicsFormat.R8_UNorm, 0);
            RenderTextureDescriptor rtd_R16 = new RenderTextureDescriptor(fptData._width, fptData._height, GraphicsFormat.R16_UNorm, 0);
            RenderTextureDescriptor rtd_R8_PolygonData = new RenderTextureDescriptor(_polygonDataTexSize, _polygonDataTexSize, GraphicsFormat.R8_UNorm, 0);
            RenderTextureDescriptor rtd_Float_PolygonData = new RenderTextureDescriptor(_polygonDataTexSize, _polygonDataTexSize, GraphicsFormat.R32_SFloat, 0);

            _outputRenderTexture = new RenderTexture(rtd_main);
            {
                string path = Path.GetDirectoryName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(fillMaterial)));
                path = Path.Combine(path, $"RT{instanceID}.renderTexture");
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
                    Color color = (_paintMode == FPT_PaintModeEnum.FlowPaintMode) ? new Color(0.5f, 0.5f, 1.0f, 1.0f) : Color.black;
                    FPT_TextureOperation.ClearRenderTexture(_outputRenderTexture, color);
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
            _polygonMaskRT = new RenderTexture(rtd_R8_PolygonData);
            _sqrMagnitudeRT = new RenderTexture(rtd_Float_PolygonData);
            _undoMemoryRenderTextureArray = Enumerable.Range(0, _memoryCount).Select(I => new RenderTexture(rtd_main)).ToArray();

            Graphics.Blit(_outputRenderTexture, _preOutputRenderTexture);
            Graphics.Blit(_outputRenderTexture, _paintRenderTexture);
            FPT_TextureOperation.ClearRenderTexture(_densityRenderTexture, Color.clear);
            FPT_TextureOperation.ClearRenderTexture(_polygonMaskRT, Color.clear);
            FPT_TextureOperation.ClearRenderTexture(_sqrMagnitudeRT, Color.clear);
            Graphics.Blit(_outputRenderTexture, _undoMemoryRenderTextureArray[0]);

            _polygonMaskT2D = FPT_TextureOperation.GenerateTexture2D(_polygonMaskRT);
            _sqrMagnitudeT2D = FPT_TextureOperation.GenerateTexture2D(_sqrMagnitudeRT);
            _polygonThinningT2D = new Texture2D(_polygonDataTexSize, _polygonDataTexSize, GraphicsFormat.R8_UNorm, TextureCreationFlags.None);
            // GenerateRenderTexture End

            // GenerateMaterial Start
            _copyBleedMaterialArray = Enumerable.Range(0, _bleedRange).Select(I => UnityEngine.Object.Instantiate(assets.GetBleedMaterial())).ToArray();

            _copySqrMagnitudeMaterial = UnityEngine.Object.Instantiate(assets.GetSqrMagnitudeMaterial());
            _copyPolygonMaskMaterial = UnityEngine.Object.Instantiate(assets.GetPolygonMaskMaterial());
            _copyPolygonMaskResultMaterial = UnityEngine.Object.Instantiate(assets.GetPolygonMaskResultMaterial());

            if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
            {
                _copyTargetPaintMaterial = UnityEngine.Object.Instantiate(assets.GetFlowPaintMaterial());
                _copyTargetMergeMaterial = UnityEngine.Object.Instantiate(assets.GetFlowMergeMaterial());
                _copyTargetResultMaterial = UnityEngine.Object.Instantiate(assets.GetFlowResultMaterial());
            }
            else if (_paintMode == FPT_PaintModeEnum.ColorPaintMode)
            {
                _copyTargetPaintMaterial = UnityEngine.Object.Instantiate(assets.GetColorPaintMaterial());
                _copyTargetMergeMaterial = UnityEngine.Object.Instantiate(assets.GetColorMergeMaterial());
                _copyTargetResultMaterial = UnityEngine.Object.Instantiate(assets.GetColorResultMaterial());

                if (_actualSRGB)
                {
                    _copyTargetResultMaterial.EnableKeyword("IS_SRGB");
                }
                else
                {
                    _copyTargetResultMaterial.DisableKeyword("IS_SRGB");
                }
            }

            TargetUVChannel(_copyTargetPaintMaterial, fptData._targetUVChannel);
            TargetUVChannel(_copyTargetResultMaterial, fptData._targetUVChannel);
            // GenerateMaterial End

            // GenerateCommandBuffer Start
            _paintCommandBuffer = new CommandBuffer();
            _paintCommandBuffer.GetTemporaryRT(_tempRT_SPIDs[0], rtd_main);
            _paintCommandBuffer.GetTemporaryRT(_tempRT_SPIDs[1], rtd_R16);
            _paintCommandBuffer.Blit(_paintRenderTexture, _tempRT_SPIDs[0]);
            _paintCommandBuffer.Blit(_densityRenderTexture, _tempRT_SPIDs[1]);
            _paintCommandBuffer.SetRenderTarget(_tempRT_RTIDs, _tempRT_RTIDs[0]);
            _paintCommandBuffer.DrawRenderer(_sorceRenderer, _copyTargetPaintMaterial, fptData._targetSubMesh);
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

            _polygonMaskCommandBuffer = new CommandBuffer();
            _polygonMaskCommandBuffer.GetTemporaryRT(_tempRT_SPIDs[0], rtd_R8_PolygonData);
            _polygonMaskCommandBuffer.SetRenderTarget(_tempRT_SPIDs[0]);
            _polygonMaskCommandBuffer.DrawRenderer(_sorceRenderer, _copyPolygonMaskMaterial, fptData._targetSubMesh);
            _polygonMaskCommandBuffer.Blit(_tempRT_SPIDs[0], _polygonMaskRT);
            _polygonMaskCommandBuffer.ReleaseTemporaryRT(_tempRT_SPIDs[0]);

            _sqrMagnitudeCommandBuffer = new CommandBuffer();
            _sqrMagnitudeCommandBuffer.SetRenderTarget(_sqrMagnitudeRT);
            _sqrMagnitudeCommandBuffer.DrawRenderer(_sorceRenderer, _copySqrMagnitudeMaterial, fptData._targetSubMesh);
            // GenerateCommandBuffer End
        }



        public void MaterialFixedUpdate()
        {
            if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
            {
                _copyTargetResultMaterial.SetFloat("_DisplayNormalAmount", FPT_EditorData.GetSingleton().GetDisplayNormalAmount());
                _copyTargetResultMaterial.SetFloat("_DisplayNormalLength", FPT_EditorData.GetSingleton().GetDisplayNormalLength());
            }

            for (int index = 0; index < _bleedRange; ++index)
            {
                _copyBleedMaterialArray[index].SetTexture(_fillTexSPID, _fillRenderTextureArray[index]);
            }

            _copyTargetPaintMaterial.SetTexture(_paintTexSPID, _paintRenderTexture);
            _copyTargetPaintMaterial.SetTexture(_densityTexSPID, _densityRenderTexture);
            _copyTargetPaintMaterial.SetTexture(_polygonMaskTexSPID, _polygonMaskRT);
            _copyTargetPaintMaterial.SetTexture(_polygonThinningTexSPID, _polygonThinningT2D);
            _copyTargetMergeMaterial.SetTexture(_paintTexSPID, _paintRenderTexture);
            _copyTargetMergeMaterial.SetTexture(_densityTexSPID, _densityRenderTexture);
            _copyTargetResultMaterial.SetTexture(_mainTexSPID, _outputRenderTexture);

            _copyPolygonMaskMaterial.SetTexture(_polygonMaskTexSPID, _polygonMaskRT);
            _copyPolygonMaskResultMaterial.SetTexture(_polygonMaskTexSPID, _polygonMaskRT);
        }



        private bool PaintToolRaycast(out Vector3 point)
        {
            Ray ray = FPT_Main.GetCamera().ScreenPointToRay(Input.mousePosition);
            return _raycast.Raycast(_sorceRenderer, _subMeshIndexArray, ray, out point, 1024f);
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

        public void PaintProcess()
        {
            bool hit = PaintToolRaycast(out Vector3 hitPosition);

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
                _copySqrMagnitudeMaterial.SetVector(_preHitPositionSPID, _preHitPosition);
                _copySqrMagnitudeMaterial.SetVector(_textureSizeSPID, new Vector2(_polygonDataTexSize, _polygonDataTexSize));
                Graphics.ExecuteCommandBuffer(_sqrMagnitudeCommandBuffer);
                FPT_TextureOperation.DataTransfer(_sqrMagnitudeRT, _sqrMagnitudeT2D);
                byte[] input = _sqrMagnitudeT2D.GetRawTextureData();
                byte[] output = _meshProcess.ThinningTextureUpdate(input);
                _polygonThinningT2D.LoadRawTextureData(output);
                _polygonThinningT2D.Apply();

                _copyTargetPaintMaterial.SetVector(_preHitPositionSPID, _preHitPosition);
                _copyTargetPaintMaterial.SetVector(_hitPositionSPID, hitPosition);
                _copyTargetPaintMaterial.SetFloat(_brushSizeSPID, editorData.GetBrushSize());
                _copyTargetPaintMaterial.SetFloat(_brushStrengthSPID, editorData.GetBrushStrength());
                _copyTargetPaintMaterial.SetInt(_brushTypeSPID, (int)editorData.GetBrushShape());

                if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
                {
                    paintDirection = editorData.GetFixedDirection() ? editorData.GetFixedDirectionVector() : paintDirection;

                    _copyTargetPaintMaterial.SetVector(_paintDirectionSPID, paintDirection);
                    _copyTargetPaintMaterial.SetInt(_fixedHeightSPID, Convert.ToInt32(editorData.GetHeightLimit()));
                    _copyTargetPaintMaterial.SetFloat(_fixedHeightMinSPID, editorData.GetMinHeight());
                    _copyTargetPaintMaterial.SetFloat(_fixedHeightMaxSPID, editorData.GetMaxHeight());
                }
                else if (_paintMode == FPT_PaintModeEnum.ColorPaintMode)
                {
                    Color paintColor = editorData.GetPaintColor();

                    if (_actualSRGB)
                    {
                        paintColor = GammaToLinearSpace(paintColor);
                    }

                    int editRGBA = (editorData.GetEditR() ? 1 : 0) + (editorData.GetEditG() ? 2 : 0) + (editorData.GetEditB() ? 4 : 0) + (editorData.GetEditA() ? 8 : 0);

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

                _drawing = false;
            }

            _preHit = hit;
        }

        public void MaskProcess()
        {
            bool hit = PaintToolRaycast(out Vector3 hitPosition);

            FPT_Core.GetSingleton().MoveRangeVisualizar(hit, hitPosition);

            bool leftClick = Input.GetMouseButton(0);
            bool rightClick = Input.GetMouseButton(1);
            bool click = leftClick || rightClick;

            if (!_preHit || !hit || !click)
            {
                _preHitPosition = hitPosition;
            }

            bool colorPaintDraw = (click && !_drawing) || (_preHitPosition != hitPosition);

            if (colorPaintDraw)
            {
                float changeMask = leftClick ? 0.0f : 1.0f;

                _copyPolygonMaskMaterial.SetFloat(_changeMaskSPID, changeMask);
                _copyPolygonMaskMaterial.SetVector(_preHitPositionSPID, _preHitPosition);
                _copyPolygonMaskMaterial.SetVector(_hitPositionSPID, hitPosition);
                _copyPolygonMaskMaterial.SetFloat(_brushSizeSPID, FPT_EditorData.GetSingleton().GetBrushSize());

                Graphics.ExecuteCommandBuffer(_polygonMaskCommandBuffer);

                _preHitPosition = hitPosition;
                _drawing = true;
            }

            if (_drawing && !click)
            {
                _drawing = false;
            }

            _preHit = hit;
        }



        public void UnmaskAll()
        {
            FPT_TextureOperation.ClearRenderTexture(_polygonMaskRT, Color.clear);
        }

        public void MaskAll()
        {
            FPT_TextureOperation.ClearRenderTexture(_polygonMaskRT, Color.white);
        }

        public void UnmaskLinked()
        {
            FPT_TextureOperation.DataTransfer(_polygonMaskRT, _polygonMaskT2D);
            byte[] data = _polygonMaskT2D.GetRawTextureData();

            _meshProcess.UnmaskLinked(data);

            _polygonMaskT2D.LoadRawTextureData(data);
            _polygonMaskT2D.Apply();
            Graphics.Blit(_polygonMaskT2D, _polygonMaskRT);
        }

        public void MaskLinked()
        {
            FPT_TextureOperation.DataTransfer(_polygonMaskRT, _polygonMaskT2D);
            byte[] data = _polygonMaskT2D.GetRawTextureData();

            _meshProcess.MaskLinked(data);

            _polygonMaskT2D.LoadRawTextureData(data);
            _polygonMaskT2D.Apply();
            Graphics.Blit(_polygonMaskT2D, _polygonMaskRT);
        }

        public void InvertMasked()
        {
            FPT_TextureOperation.DataTransfer(_polygonMaskRT, _polygonMaskT2D);
            byte[] data = _polygonMaskT2D.GetRawTextureData();

            for (int index = 0; index < data.Length; ++index)
            {
                bool temp0 = data[index] == 0;
                data[index] = temp0 ? (byte)255 : (byte)0;
            }

            _polygonMaskT2D.LoadRawTextureData(data);
            _polygonMaskT2D.Apply();
            Graphics.Blit(_polygonMaskT2D, _polygonMaskRT);
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

        public void MeshProcessGUI()
        {
            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(TextData_Mask.MaskAll))
                {
                    MaskAll();
                }

                if (GUILayout.Button(TextData_Mask.UnmaskAll))
                {
                    UnmaskAll();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(TextData_Mask.MaskLinked))
                {
                    MaskLinked();
                }

                if (GUILayout.Button(TextData_Mask.UnmaskLinked))
                {
                    UnmaskLinked();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            if (GUILayout.Button(TextData_Mask.InvertMasked))
            {
                InvertMasked();
            }

            EditorGUILayout.Space(20);
        }
    }
}

#endif