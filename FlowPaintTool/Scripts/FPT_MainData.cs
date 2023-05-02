using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace FlowPaintTool
{
    public struct FPT_MainData
    {
        static public FPT_MainData Constructor()
        {
            FPT_MainData fptData = new FPT_MainData();
            fptData.Reset();
            return fptData;
        }

        public FPT_PaintModeEnum _paintMode;
        public Mesh _startMesh;
        public Renderer _sorceRenderer;
        public Vector2Int _outputTextureResolution;
        public FPT_StartTextureLoadModeEnum _startTextureLoadMode;
        public Texture _startTexture;
        public string _startTextureFilePath;
        public bool _startTextureSRGB;
        public int _targetUVChannel;
        public int _bleedRange;
        public float _uv_Epsilon;

        public bool _textureExist;
        public bool _actualSRGB;

        private void Reset()
        {
            _paintMode = FPT_PaintModeEnum.FlowPaintMode;
            _startMesh = null;
            _sorceRenderer = null;
            _outputTextureResolution = new Vector2Int(1024, 1024);
            _startTextureLoadMode = FPT_StartTextureLoadModeEnum.Assets;
            _startTexture = null;
            _startTextureFilePath = string.Empty;
            _startTextureSRGB = false;
            _bleedRange = 4;
            _uv_Epsilon = 0.0001f;
            _targetUVChannel = 0;

            _textureExist = false;
            _actualSRGB = false;
        }

        private void ConsistencyCheck()
        {
            _outputTextureResolution.x = Math.Max(_outputTextureResolution.x, 0);
            _outputTextureResolution.y = Math.Max(_outputTextureResolution.y, 0);
            _targetUVChannel = Math.Max(Math.Min(_targetUVChannel, 7), 0);
            _bleedRange = Math.Max(_bleedRange, 0);
            _uv_Epsilon = Math.Max(_uv_Epsilon, 0f);
        }

        private bool SRGBCheck()
        {
            bool result = false;

            if (_startTextureLoadMode == FPT_StartTextureLoadModeEnum.Assets)
            {
                result = _startTexture != null;
            }
            else if (_startTextureLoadMode == FPT_StartTextureLoadModeEnum.FilePath)
            {
                result = File.Exists(_startTextureFilePath);
            }

            _textureExist = result;

            result = false;

            if (_textureExist && (PlayerSettings.colorSpace == ColorSpace.Linear))
            {
                if (_startTextureLoadMode == FPT_StartTextureLoadModeEnum.Assets)
                {
                    result = GraphicsFormatUtility.IsSRGBFormat(_startTexture.graphicsFormat);
                }
                else if (_startTextureLoadMode == FPT_StartTextureLoadModeEnum.FilePath)
                {
                    result = _startTextureSRGB;
                }
            }

            _actualSRGB = result;
            return result;
        }

        private bool CheckError(bool flagSMR, bool flagMFMR)
        {
            bool isError = false;

            if (SRGBCheck() && (_paintMode == FPT_PaintModeEnum.FlowPaintMode))
            {
                EditorGUILayout.HelpBox("Using sRGB textures in FlowPaintMode will not give accurate results\nPlease turn off sRGB", MessageType.Error);
                isError = true;
            }

            if (!(flagSMR || flagMFMR))
            {
                EditorGUILayout.HelpBox("Please use either SkinnedMeshRenderer or a combination of MeshFilter and MeshRenderer", MessageType.Error);
                return true;
            }

            if (_startMesh == null)
            {
                EditorGUILayout.HelpBox("Mesh not set", MessageType.Error);
                return true;
            }

            if (!_startMesh.isReadable)
            {
                EditorGUILayout.HelpBox("Please allow Read/Write for the mesh", MessageType.Error);
                return true;
            }

            List<Vector2> temp0 = new List<Vector2>();
            _startMesh.GetUVs(_targetUVChannel, temp0);

            if (temp0.Count == 0)
            {
                EditorGUILayout.HelpBox($"UV coordinate does not exist in UVchannel {_targetUVChannel}", MessageType.Error);
                return true;
            }

            return isError;
        }

        public void EditorWindowGUI(Transform selectTransform)
        {
            _paintMode = (FPT_PaintModeEnum)EditorGUILayout.EnumPopup("PaintMode", _paintMode);

            GUILayout.Space(20);

            _outputTextureResolution.x = EditorGUILayout.IntField("OutputTextureWidth", _outputTextureResolution.x);
            _outputTextureResolution.y = EditorGUILayout.IntField("OutputTextureHeight", _outputTextureResolution.y);

            GUILayout.Space(20);

            _startTextureLoadMode = (FPT_StartTextureLoadModeEnum)EditorGUILayout.EnumPopup("StartTextureLoadMode", _startTextureLoadMode);

            if (_startTextureLoadMode == FPT_StartTextureLoadModeEnum.Assets)
            {
                _startTexture = (Texture)EditorGUILayout.ObjectField("StartTexture", _startTexture, typeof(Texture), true);
            }
            else if (_startTextureLoadMode == FPT_StartTextureLoadModeEnum.FilePath)
            {
                if (GUILayout.Button("Open file panel"))
                {
                    string filePath = EditorUtility.OpenFilePanel("Select Texture", string.Empty, string.Empty);

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        _startTextureFilePath = filePath;
                    }
                }

                _startTextureFilePath = EditorGUILayout.TextField("FilePath", _startTextureFilePath);
                _startTextureSRGB = EditorGUILayout.Toggle("sRGB (Color Texture)", _startTextureSRGB);
            }

            GUILayout.Space(20);

            GUILayout.Label("Advanced Settings", FPT_GUIStyle.GetCenterLabel());

            _targetUVChannel = EditorGUILayout.IntField("TargetUVChannel", _targetUVChannel);
            _bleedRange = EditorGUILayout.IntField("BleedRange", _bleedRange);
            _uv_Epsilon = EditorGUILayout.FloatField("UV_Epsilon", _uv_Epsilon);
            FPT_EditorWindow.EditorDataInstance.ChangeUndoMaxCount();

            GUILayout.Space(40);

            _startMesh = null;
            GameObject selectObject = selectTransform.gameObject;

            SkinnedMeshRenderer temp3 = selectObject.GetComponent<SkinnedMeshRenderer>();
            MeshFilter temp4 = selectObject.GetComponent<MeshFilter>();
            MeshRenderer temp5 = selectObject.GetComponent<MeshRenderer>();

            bool flagSMR = temp3 != null;
            bool flagMFMR = (temp4 != null) && (temp5 != null);

            if (flagSMR)
            {
                _startMesh = temp3.sharedMesh;
            }
            else if (flagMFMR)
            {
                _startMesh = temp4.sharedMesh;
            }

            ConsistencyCheck();
            bool isError = CheckError(flagSMR, flagMFMR);

            if (GUILayout.Button("Generate Paint tool object") && !isError)
            {
                FPT_Parameter[] flowPaintToolControl = UnityEngine.Object.FindObjectsOfType<FPT_Parameter>();

                if (flowPaintToolControl.Length == 0)
                {
                    GameObject go0 = new GameObject("PaintToolControl");
                    go0.AddComponent<FPT_Parameter>();
                }

                if (flagSMR)
                {
                    _sorceRenderer = temp3;
                }
                else if (flagMFMR)
                {
                    _sorceRenderer = temp5;
                }

                GameObject go1 = new GameObject("PaintTool");
                go1.transform.SetParent(selectTransform, false);
                FPT_Main fpt1 = go1.AddComponent<FPT_Main>();
                fpt1.SetData(this);
            }
        }
    }
}
