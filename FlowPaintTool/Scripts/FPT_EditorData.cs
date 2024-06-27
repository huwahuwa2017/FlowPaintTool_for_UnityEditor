#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    using TextData = FPT_Language.FPT_EditorDataText;

    //[CreateAssetMenu(fileName = "Data", menuName = "FlowPaintTool/FPT_EditorData")]
    public class FPT_EditorData : ScriptableObject
    {
        private static FPT_EditorData _staticInstance = null;

        public static FPT_EditorData GetSingleton()
        {
            if (_staticInstance == null)
            {
                string path = AssetDatabase.GUIDToAssetPath("dbf48c8133b420242b2628a3104916fd");
                _staticInstance = AssetDatabase.LoadAssetAtPath<FPT_EditorData>(path);
            }

            return _staticInstance;
        }



        [SerializeField]
        private FPT_MainData _mainData = FPT_MainData.Constructor();
        [SerializeField]
        private float _cameraRotateSpeed = 3f;
        [SerializeField]
        private float _cameraMoveSpeed = 0.05f;
        [SerializeField]
        private int _cameraInertia = 6;
        [SerializeField]
        private FPT_LanguageTypeEnum _languageType = FPT_LanguageTypeEnum.Japanese;

        private bool _enableMaskMode = false;
        private bool _enablePreviewMode = false;

        private float _brushSize = 0.1f;
        private float _brushStrength = 1.0f;
        private FPT_BrushShapeEnum _brushShape = FPT_BrushShapeEnum.Smooth;
        private float _brushMoveSensitivity = 0.01f; // UI未実装

        private bool _heightLimit = false;
        private float _minHeight = 0.5f;
        private float _maxHeight = 1f;
        private bool _fixedDirection = false;
        private Vector3 _fixedDirectionVector = Vector3.down;
        private float _displayNormalLength = 0.02f;
        private float _displayNormalAmount = 64f;

        private Color _paintColor = Color.white;
        private bool _editR = true;
        private bool _editG = true;
        private bool _editB = true;
        private bool _editA = true;

        private bool _operationInstructions = true;

        public float GetCameraRotateSpeed() => _cameraRotateSpeed;
        public float GetCameraMoveSpeed() => _cameraMoveSpeed;
        public int GetCameraInertia() => _cameraInertia;

        public bool GetEnableMaskMode() => _enableMaskMode;
        public bool GetEnablePreviewMode() => _enablePreviewMode;

        public float GetBrushSize() => _brushSize;
        public float GetBrushStrength() => _brushStrength;
        public FPT_BrushShapeEnum GetBrushShape() => _brushShape;
        public float GetBrushMoveSensitivity() => _brushMoveSensitivity;

        public bool GetHeightLimit() => _heightLimit;
        public float GetMinHeight() => _minHeight;
        public float GetMaxHeight() => _maxHeight;
        public bool GetFixedDirection() => _fixedDirection;
        public Vector3 GetFixedDirectionVector() => _fixedDirectionVector;
        public float GetDisplayNormalLength() => _displayNormalLength;
        public float GetDisplayNormalAmount() => _displayNormalAmount;

        public Color GetPaintColor() => _paintColor;
        public bool GetEditR() => _editR;
        public bool GetEditG() => _editG;
        public bool GetEditB() => _editB;
        public bool GetEditA() => _editA;

        public bool GetOperationInstructions() => _operationInstructions;



        private void SetCameraRotateSpeed(float value)
        {
            var newValue = Math.Max(value, 0f);

            if (newValue != _cameraRotateSpeed)
            {
                _cameraRotateSpeed = newValue;
                EditorUtility.SetDirty(this);
            }
        }

        private void SetCameraMoveSpeed(float value)
        {
            var newValue = Math.Max(value, 0f);

            if (newValue != _cameraMoveSpeed)
            {
                _cameraMoveSpeed = newValue;
                EditorUtility.SetDirty(this);
            }
        }

        private void SetCameraInertia(int value)
        {
            var newValue = Math.Max(value, 1);

            if (newValue != _cameraInertia)
            {
                _cameraInertia = newValue;
                EditorUtility.SetDirty(this);
            }
        }

        private void SetLanguageType(FPT_LanguageTypeEnum value)
        {
            var newValue = value;

            if (newValue != _languageType)
            {
                _languageType = newValue;
                EditorUtility.SetDirty(this);
            }
        }

        private void SetBrushSize(float value)
        {
            _brushSize = Math.Max(value, 0f);
        }

        private void SetBrushStrength(float value)
        {
            _brushStrength = Mathf.Clamp01(value);
        }

        private void SetDisplayNormalLength(float value)
        {
            _displayNormalLength = Math.Max(value, 0f);
        }

        private void SetDisplayNormalAmount(float value)
        {
            _displayNormalAmount = Math.Max(value, 0f);
        }



        public void ChangeBrushSize(float scrollDelta)
        {
            SetBrushSize(_brushSize + Math.Max(_brushSize, 0.001f) * scrollDelta * 0.1f);
            FPT_EditorWindow.RepaintInspectorWindow();
        }

        public void ChangeBrushStrength(float scrollDelta)
        {
            SetBrushStrength(_brushStrength + scrollDelta * 0.05f);
            FPT_EditorWindow.RepaintInspectorWindow();
        }

        public void ChangeMaskMode()
        {
            _enableMaskMode = !_enableMaskMode;
            FPT_EditorWindow.RepaintInspectorWindow();
        }

        public void ChangePreviewMode()
        {
            if (_enableMaskMode) return;

            _enablePreviewMode = !_enablePreviewMode;
            FPT_EditorWindow.RepaintInspectorWindow();
        }



        public void UpdateLanguageType()
        {
            FPT_Language.ChangeLanguage(_languageType);
        }

        public void ChangeLanguageType(FPT_LanguageTypeEnum languageType)
        {
            SetLanguageType(languageType);
            UpdateLanguageType();
        }



        public void DisablePreviewMode()
        {
            _enablePreviewMode = false;
        }

        public void ChangeOperationInstructions()
        {
            _operationInstructions = !_operationInstructions;
        }



        public void ResetParameter()
        {
            _mainData = FPT_MainData.Constructor();
            _cameraRotateSpeed = 3f;
            _cameraMoveSpeed = 0.05f;
            _cameraInertia = 6;
            _languageType = FPT_LanguageTypeEnum.Japanese;

            _enableMaskMode = false;
            _enablePreviewMode = false;

            _brushSize = 0.1f;
            _brushStrength = 1.0f;
            _brushShape = FPT_BrushShapeEnum.Smooth;
            _brushMoveSensitivity = 0.01f;

            _heightLimit = false;
            _minHeight = 0.5f;
            _maxHeight = 1f;
            _fixedDirection = false;
            _fixedDirectionVector = Vector3.down;
            _displayNormalLength = 0.02f;
            _displayNormalAmount = 64f;

            _paintColor = Color.white;
            _editR = true;
            _editG = true;
            _editB = true;
            _editA = true;

            _operationInstructions = true;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }



        public void EditorWindowGUI(Transform selectTransform)
        {
            _mainData.EditorWindowGUI(selectTransform);
        }

        private void CommonGUI(FPT_ShaderProcess shaderProcess)
        {
            GUIStyle box = FPT_GUIStyle.GetBox();

            EditorGUILayout.Space(10);

            shaderProcess.PreviewGUI();

            EditorGUILayout.Space(10);

            _enableMaskMode = EditorGUILayout.Toggle(TextData.MaskMode, _enableMaskMode);
            _enablePreviewMode = EditorGUILayout.Toggle(TextData.PreviewMode, _enablePreviewMode);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(TextData.CameraSettings);
            EditorGUILayout.BeginVertical(box);
            {
                SetCameraRotateSpeed(EditorGUILayout.FloatField(TextData.RotateSpeed, _cameraRotateSpeed));
                SetCameraMoveSpeed(EditorGUILayout.FloatField(TextData.MoveSpeed, _cameraMoveSpeed));
                SetCameraInertia(EditorGUILayout.IntField(TextData.Inertia, _cameraInertia));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(TextData.BrushSettings);
            EditorGUILayout.BeginVertical(box);
            {
                SetBrushSize(EditorGUILayout.FloatField(TextData.Size, _brushSize));
                SetBrushStrength(EditorGUILayout.FloatField(TextData.Strength, _brushStrength));
                _brushShape = (FPT_BrushShapeEnum)EditorGUILayout.EnumPopup(TextData.Shape, _brushShape);
            }
            EditorGUILayout.EndVertical();
        }

        private void FlowPaintGUI(FPT_ShaderProcess shaderProcess)
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(TextData.FlowPaintSettings);
            EditorGUILayout.BeginVertical(FPT_GUIStyle.GetBox());
            {
                _heightLimit = EditorGUILayout.Toggle(TextData.HeightLimit, _heightLimit);

                if (_heightLimit)
                {
                    EditorGUI.indentLevel++;

                    float tempMin = EditorGUILayout.Slider(TextData.MinimumHeight, _minHeight, -1f, 1f);
                    float tempMax = EditorGUILayout.Slider(TextData.MaximumHeight, _maxHeight, -1f, 1f);

                    if (_minHeight != tempMin)
                    {
                        _minHeight = Mathf.Clamp(tempMin, -1f, _maxHeight);
                    }

                    if (_maxHeight != tempMax)
                    {
                        _maxHeight = Mathf.Clamp(tempMax, _minHeight, 1f);
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(10);

                _fixedDirection = EditorGUILayout.Toggle(TextData.FixedDirection, _fixedDirection);

                if (_fixedDirection)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(15);

                        if (GUILayout.Button(TextData.InputGazeVector))
                        {
                            _fixedDirectionVector = FPT_Main.GetCamera().transform.forward;
                        }

                        if (GUILayout.Button(TextData.Flip))
                        {
                            _fixedDirectionVector = -_fixedDirectionVector;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    _fixedDirectionVector = EditorGUILayout.Vector3Field(TextData.FixedDirectionVector, _fixedDirectionVector);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(10);

                SetDisplayNormalLength(EditorGUILayout.FloatField(TextData.DisplayNormalLength, _displayNormalLength));
                SetDisplayNormalAmount(EditorGUILayout.FloatField(TextData.DisplayNormalAmount, _displayNormalAmount));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20);

            shaderProcess.UndoRedoOutputGUI();

            EditorGUILayout.Space(20);
        }

        private void ColorPaintGUI(FPT_ShaderProcess shaderProcess)
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(TextData.ColorPaintSettings);
            EditorGUILayout.BeginVertical(FPT_GUIStyle.GetBox());
            {
                _paintColor = EditorGUILayout.ColorField(TextData.Color, _paintColor);

                EditorGUILayout.Space(10);

                EditorGUILayout.LabelField(TextData.SelectColorToEdit);

                EditorGUILayout.BeginHorizontal();
                {
                    _editR = GUILayout.Toggle(_editR, TextData.R);
                    _editG = GUILayout.Toggle(_editG, TextData.G);
                    _editB = GUILayout.Toggle(_editB, TextData.B);
                    _editA = GUILayout.Toggle(_editA, TextData.A);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20);

            shaderProcess.UndoRedoOutputGUI();

            GUILayout.Space(20);
        }

        public void InspectorGUI(FPT_ShaderProcess shaderProcess, FPT_PaintModeEnum paintMode)
        {
            UpdateLanguageType();

            CommonGUI(shaderProcess);

            if (_enableMaskMode)
            {
                shaderProcess.MeshProcessGUI();
            }
            else if (paintMode == FPT_PaintModeEnum.FlowPaintMode)
            {
                FlowPaintGUI(shaderProcess);
            }
            else if (paintMode == FPT_PaintModeEnum.ColorPaintMode)
            {
                ColorPaintGUI(shaderProcess);
            }
        }



        [CustomEditor(typeof(FPT_EditorData))]
        public class FlowPaintToolEditorData_InspectorUI : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                EditorGUILayout.Space(20f);

                if (GUILayout.Button("Reset"))
                {
                    FPT_EditorData _instance = target as FPT_EditorData;
                    _instance.ResetParameter();
                }
            }
        }
    }
}

#endif