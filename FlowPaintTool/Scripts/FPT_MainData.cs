#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace FlowPaintTool
{
    using TextData = FPT_Language.FPT_MainDataText;

    [Serializable]
    public struct FPT_MainData
    {
        public static FPT_MainData Constructor()
        {
            FPT_MainData fptData = new FPT_MainData();
            fptData.Reset();
            return fptData;
        }



        public FPT_PaintModeEnum _paintMode;
        public Mesh _startMesh;
        public Renderer _sorceRenderer;
        public int _width;
        public int _height;

        public FPT_StartTextureLoadModeEnum _startTextureLoadMode;
        public Texture _startTexture;
        public string _startTextureFilePath;
        public bool _startTextureSRGB;

        public int _targetSubMesh;
        public int _targetUVChannel;
        public int _bleedRange;
        public float _uv_Epsilon;
        public int _maxUndoCount;

        public bool _textureExist;
        public bool _actualSRGB;

        private void Reset()
        {
            _paintMode = FPT_PaintModeEnum.FlowPaintMode;
            _startMesh = null;
            _sorceRenderer = null;
            _width = 2048;
            _height = 2048;

            _startTextureLoadMode = FPT_StartTextureLoadModeEnum.FilePath;
            _startTexture = null;
            _startTextureFilePath = string.Empty;
            _startTextureSRGB = false;

            _targetSubMesh = 0;
            _targetUVChannel = 0;
            _bleedRange = 4;
            _uv_Epsilon = 0.0001f;
            _maxUndoCount = 15;

            _textureExist = false;
            _actualSRGB = false;
        }

        private bool ErrorCheckGUI(bool flagSMR, bool flagMFMR)
        {
            bool isError = false;

            if (!(flagSMR || flagMFMR))
            {
                EditorGUILayout.HelpBox(TextData.SelectGameObjectThatUsesMeshRendererOrSkinnedMeshRenderer, MessageType.Error);
                isError = true;
            }
            else
            {
                if (_startMesh == null)
                {
                    EditorGUILayout.HelpBox(TextData.MeshNotFound, MessageType.Error);
                    isError = true;
                }
                else
                {
                    if (!_startMesh.isReadable)
                    {
                        EditorGUILayout.HelpBox(TextData.PleaseAllowReadWriteForTheMesh, MessageType.Error);
                        isError = true;
                    }
                    else
                    {
                        List<Vector2> temp0 = new List<Vector2>();
                        _startMesh.GetUVs(_targetUVChannel, temp0);

                        if (temp0.Count == 0)
                        {
                            EditorGUILayout.HelpBox(TextData.UVCoordinateDoesNotExistInUVchannel + _targetUVChannel, MessageType.Error);
                            isError = true;
                        }

                        if (_targetSubMesh >= _startMesh.subMeshCount)
                        {
                            EditorGUILayout.HelpBox(TextData.ThisSubmeshDoesNotExist + _targetSubMesh, MessageType.Error);
                            isError = true;
                        }
                    }
                }
            }

            if ((_startTextureLoadMode == FPT_StartTextureLoadModeEnum.Assets) && (_startTexture == null))
            {
                EditorGUILayout.HelpBox(TextData.IfYouWantToStartThePaintToolWithoutAStartingTexture, MessageType.Error);
                isError = true;
            }

            if ((_paintMode == FPT_PaintModeEnum.FlowPaintMode) && _actualSRGB)
            {
                EditorGUILayout.HelpBox(TextData.UsingSRGBTexturesInFlowPaintModeWillNot, MessageType.Error);
                isError = true;
            }

            if ((_width > 8192) || (_height > 8192))
            {
                EditorGUILayout.HelpBox(TextData.UnityDoesNotSupportImportingImagesIn, MessageType.Warning);
            }

            return isError;
        }

        public void EditorWindowGUI()
        {
            _paintMode = (FPT_PaintModeEnum)EditorGUILayout.EnumPopup(TextData.PaintMode, _paintMode);

            GUILayout.Space(20);

            _startTextureLoadMode = (FPT_StartTextureLoadModeEnum)EditorGUILayout.EnumPopup(TextData.TypeOfStartingTexture, _startTextureLoadMode);

            if (_startTextureLoadMode == FPT_StartTextureLoadModeEnum.Assets)
            {
                _startTexture = (Texture)EditorGUILayout.ObjectField(TextData.StartingTexture, _startTexture, typeof(Texture), true);
            }
            else if (_startTextureLoadMode == FPT_StartTextureLoadModeEnum.FilePath)
            {
                if (GUILayout.Button(TextData.OpenFilePanel))
                {
                    string filePath = EditorUtility.OpenFilePanel(TextData.StartingTexture, string.Empty, string.Empty);

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        _startTextureFilePath = filePath;
                    }
                }

                _startTextureFilePath = EditorGUILayout.TextField(TextData.FilePath, _startTextureFilePath);
                _startTextureSRGB = EditorGUILayout.Toggle(TextData.SRGBColorTexture, _startTextureSRGB);
            }

            GUILayout.Space(20);

            _width = EditorGUILayout.IntField(TextData.WidthOfTextureCreated, _width);
            _height = EditorGUILayout.IntField(TextData.HeightOfTextureCreated, _height);

            GUILayout.Space(20);

            GUILayout.Label(TextData.AdvancedSettings, FPT_GUIStyle.GetCenterLabel());

            _targetSubMesh = EditorGUILayout.IntField(TextData.TargetSubmeshIndex, _targetSubMesh);
            _targetUVChannel = EditorGUILayout.IntField(TextData.TargetUVChannel, _targetUVChannel);
            _bleedRange = EditorGUILayout.IntField(TextData.BleedRange, _bleedRange);
            _uv_Epsilon = EditorGUILayout.FloatField(TextData.UVEpsilon, _uv_Epsilon);
            _maxUndoCount = EditorGUILayout.IntField(TextData.MaxUndoCount, _maxUndoCount);

            GUILayout.Space(40);

            GameObject selectObject = Selection.activeTransform.gameObject;

            SkinnedMeshRenderer temp3 = selectObject.GetComponent<SkinnedMeshRenderer>();
            MeshFilter temp4 = selectObject.GetComponent<MeshFilter>();
            MeshRenderer temp5 = selectObject.GetComponent<MeshRenderer>();

            bool flagSMR = temp3 != null;
            bool flagMFMR = (temp4 != null) && (temp5 != null);

            {
                _width = Math.Max(_width, 0);
                _height = Math.Max(_height, 0);
                _targetSubMesh = Math.Max(_targetSubMesh, 0);
                _targetUVChannel = Math.Max(Math.Min(_targetUVChannel, 7), 0);
                _bleedRange = Math.Max(_bleedRange, 0);
                _uv_Epsilon = Math.Max(_uv_Epsilon, 0f);
                _maxUndoCount = Math.Max(_maxUndoCount, 0);

                _startMesh = null;

                if (flagSMR)
                {
                    _startMesh = temp3.sharedMesh;
                }
                else if (flagMFMR)
                {
                    _startMesh = temp4.sharedMesh;
                }

                if (_startTextureLoadMode == FPT_StartTextureLoadModeEnum.Assets)
                {
                    _textureExist = _startTexture != null;

                    if (_textureExist)
                    {
                        _actualSRGB = GraphicsFormatUtility.IsSRGBFormat(_startTexture.graphicsFormat);
                    }
                    else if (_paintMode == FPT_PaintModeEnum.FlowPaintMode)
                    {
                        _actualSRGB = false;
                    }
                    else if (_paintMode == FPT_PaintModeEnum.ColorPaintMode)
                    {
                        _actualSRGB = true;
                    }
                }
                else if (_startTextureLoadMode == FPT_StartTextureLoadModeEnum.FilePath)
                {
                    _textureExist = File.Exists(_startTextureFilePath);
                    _actualSRGB = _startTextureSRGB;
                }

                _actualSRGB = _actualSRGB && (PlayerSettings.colorSpace == ColorSpace.Linear);
            }

            bool isError = ErrorCheckGUI(flagSMR, flagMFMR);

            if (GUILayout.Button(TextData.StartThePaintTool) && !isError)
            {
                if (flagSMR)
                {
                    _sorceRenderer = temp3;
                }
                else if (flagMFMR)
                {
                    _sorceRenderer = temp5;
                }

                Debug.Log("sRGB : " + _actualSRGB.ToString());

                EditorUtility.SetDirty(FPT_EditorData.GetSingleton());

                FPT_Core.GetSingleton().GenerateFPT_Main(this);
            }
        }
    }
}

#endif