using System;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    //[CreateAssetMenu(fileName = "Data", menuName = "FlowPaintTool/FPT_EditorData")]
    public class FPT_EditorData : ScriptableObject
    {
        [SerializeField]
        private float _cameraRotateSpeed = 2f;
        [SerializeField]
        private float _moveSpeed = 0.05f;
        [SerializeField]
        private int _inertia = 6;
        [SerializeField]
        private int _undoMaxCount = 15;

        private bool _enableMaskMode = false;
        private bool _enableMaterialView = false;

        private float _brushSize = 0.1f;
        private float _brushStrength = 1.0f;
        private FPT_BrushTypeEnum _brushType = FPT_BrushTypeEnum.Smooth;
        private float _brushMoveSensitivity = 0.01f; // UI未実装　0.01固定

        private bool _fixedHeight = false;
        private float _fixedHeightMin = 0.5f;
        private float _fixedHeightMax = 1f;
        private bool _fixedDirection = false;
        private Vector3 _fixedDirectionVector = Vector3.down;
        private float _displayNormalLength = 0.02f;
        private float _displayNormalAmount = 64f;

        private Color _paintColor = Color.white;
        private bool _editR = true;
        private bool _editG = true;
        private bool _editB = true;
        private bool _editA = true;

        public float GetCameraRotateSpeed() => _cameraRotateSpeed;
        public float GetCameraMoveSpeed() => _moveSpeed;
        public int GetCameraInertia() => _inertia;
        public int GetUndoMaxCount() => _undoMaxCount;

        public bool GetEnableMaskMode() => _enableMaskMode;
        public bool GetEnableMaterialView() => _enableMaterialView;

        public float GetBrushSize() => _brushSize;
        public float GetBrushStrength() => _brushStrength;
        public FPT_BrushTypeEnum GetBrushType() => _brushType;
        public float GetBrushMoveSensitivity() => _brushMoveSensitivity;

        public bool GetFixedHeight() => _fixedHeight;
        public float GetFixedHeightMin() => _fixedHeightMin;
        public float GetFixedHeightMax() => _fixedHeightMax;
        public bool GetFixedDirection() => _fixedDirection;
        public Vector3 GetFixedDirectionVector() => _fixedDirectionVector;
        public float GetDisplayNormalLength() => _displayNormalLength;
        public float GetDisplayNormalAmount() => _displayNormalAmount;

        public Color GetPaintColor() => _paintColor;
        public bool GetEditR() => _editR;
        public bool GetEditG() => _editG;
        public bool GetEditB() => _editB;
        public bool GetEditA() => _editA;



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

            if (newValue != _moveSpeed)
            {
                _moveSpeed = newValue;
                EditorUtility.SetDirty(this);
            }
        }

        private void SetCameraInertia(int value)
        {
            var newValue = Math.Max(value, 1);

            if (newValue != _inertia)
            {
                _inertia = newValue;
                EditorUtility.SetDirty(this);
            }
        }

        private void SetUndoMaxCount(int value)
        {
            var newValue = Math.Max(value, 0);

            if (newValue != _undoMaxCount)
            {
                _undoMaxCount = newValue;
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

        private void CommonUI(FPT_ShaderProcess shaderProcess)
        {
            GUIStyle box = FPT_GUIStyle.GetBox();

            EditorGUILayout.Space(10);

            shaderProcess.PreviewGUI();

            EditorGUILayout.Space(10);

            _enableMaskMode = EditorGUILayout.Toggle("MaskMode", _enableMaskMode);
            _enableMaterialView = EditorGUILayout.Toggle("ViewMode", _enableMaterialView);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Camera settings");
            EditorGUILayout.BeginVertical(box);
            {
                SetCameraRotateSpeed(EditorGUILayout.FloatField("Rotate Speed", _cameraRotateSpeed));
                SetCameraMoveSpeed(EditorGUILayout.FloatField("Move Speed", _moveSpeed));
                SetCameraInertia(EditorGUILayout.IntField("Inertia", _inertia));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Brush settings");
            EditorGUILayout.BeginVertical(box);
            {
                SetBrushSize(EditorGUILayout.FloatField("Size", _brushSize));
                SetBrushStrength(EditorGUILayout.FloatField("Strength", _brushStrength));
                _brushType = (FPT_BrushTypeEnum)EditorGUILayout.EnumPopup("Type", _brushType);
            }
            EditorGUILayout.EndVertical();
        }

        private void FlowPaintUI(FPT_ShaderProcess shaderProcess)
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Flow paint settings");
            EditorGUILayout.BeginVertical(FPT_GUIStyle.GetBox());
            {
                _fixedHeight = EditorGUILayout.Toggle("Fixed Height", _fixedHeight);

                if (_fixedHeight)
                {
                    EditorGUI.indentLevel++;
                    float temp0 = EditorGUILayout.Slider("Fixed Height Min", _fixedHeightMin, -1f, 1f);
                    float temp1 = EditorGUILayout.Slider("Fixed Height Max", _fixedHeightMax, -1f, 1f);
                    _fixedHeightMin = Mathf.Clamp(temp0, -1f, temp1);
                    _fixedHeightMax = Mathf.Clamp(temp1, temp0, 1f);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(10);

                _fixedDirection = EditorGUILayout.Toggle("Fixed Direction", _fixedDirection);

                if (_fixedDirection)
                {
                    EditorGUI.indentLevel++;
                    _fixedDirectionVector = EditorGUILayout.Vector3Field("Fixed Direction Vector", _fixedDirectionVector);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(10);

                SetDisplayNormalLength(EditorGUILayout.FloatField("Display Normal Length", _displayNormalLength));
                SetDisplayNormalAmount(EditorGUILayout.FloatField("Display Normal Amount", _displayNormalAmount));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20);

            shaderProcess.UndoRedoOutputGUI();

            EditorGUILayout.Space(20);
        }

        private void ColorPaintUI(FPT_ShaderProcess shaderProcess)
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Color paint settings");
            EditorGUILayout.BeginVertical(FPT_GUIStyle.GetBox());
            {
                _paintColor = EditorGUILayout.ColorField("Color", _paintColor);

                EditorGUILayout.Space(10);

                EditorGUILayout.LabelField("Edit channel");

                EditorGUILayout.BeginHorizontal();
                {
                    _editR = GUILayout.Toggle(_editR, "R");
                    _editG = GUILayout.Toggle(_editG, "G");
                    _editB = GUILayout.Toggle(_editB, "B");
                    _editA = GUILayout.Toggle(_editA, "A");
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20);

            shaderProcess.UndoRedoOutputGUI();

            GUILayout.Space(20);
        }

        private void MaskUI(FPT_MeshProcess meshProcess)
        {
            EditorGUILayout.Space(20);

            meshProcess.MeshProcessGUI();
        }

        public void InspectorGUI(FPT_MeshProcess meshProcess, FPT_ShaderProcess shaderProcess, FPT_PaintModeEnum paintMode)
        {
            CommonUI(shaderProcess);

            if (_enableMaskMode)
            {
                MaskUI(meshProcess);
            }
            else if (paintMode == FPT_PaintModeEnum.FlowPaintMode)
            {
                FlowPaintUI(shaderProcess);
            }
            else if (paintMode == FPT_PaintModeEnum.ColorPaintMode)
            {
                ColorPaintUI(shaderProcess);
            }
        }



        public void ChangeUndoMaxCount()
        {
            SetUndoMaxCount(EditorGUILayout.IntField("TargetUVChannel", _undoMaxCount));
        }

        public void ChangeBrushSize(float scrollDelta)
        {
            SetBrushSize(_brushSize + Math.Max(_brushSize, 0.001f) * scrollDelta * 0.1f);
        }

        public void ChangeBrushStrength(float scrollDelta)
        {
            SetBrushStrength(_brushStrength + scrollDelta * 0.05f);
        }

        public void ChangeEnableMaskMode()
        {
            _enableMaskMode = !_enableMaskMode;
        }

        public void ChangeEnableMaterialView()
        {
            _enableMaterialView = !_enableMaterialView;
        }

        public void DisableMaterialView()
        {
            _enableMaterialView = false;
        }



        public void ParameterReset()
        {
            _cameraRotateSpeed = 2f;
            _moveSpeed = 0.05f;
            _inertia = 6;
            _undoMaxCount = 15;

            EditorUtility.SetDirty(this);

            _enableMaskMode = false;
            _enableMaterialView = false;
            _brushSize = 0.1f;
            _brushStrength = 1.0f;
            _brushType = FPT_BrushTypeEnum.Smooth;
            _brushMoveSensitivity = 0.01f;

            _fixedHeight = false;
            _fixedHeightMin = 0.5f;
            _fixedHeightMax = 1f;
            _fixedDirection = false;
            _fixedDirectionVector = Vector3.down;
            _displayNormalLength = 0.02f;
            _displayNormalAmount = 64f;

            _paintColor = Color.white;
            _editR = true;
            _editG = true;
            _editB = true;
            _editA = true;
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
                    _instance.ParameterReset();
                }
            }
        }
    }
}
