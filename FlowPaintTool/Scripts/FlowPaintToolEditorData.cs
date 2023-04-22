using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu]
[Serializable]
public class FlowPaintToolEditorData : ScriptableObject
{
    [SerializeField]
    private float _cameraRotateSpeed = 2f;

    [SerializeField]
    private float _moveSpeed = 0.05f;

    [SerializeField]
    private int _inertia = 6;

    public float CameraVerticalRotateSpeed
    {
        get => _cameraRotateSpeed;

        set
        {
            var newValue = Math.Max(value, 0f);

            if (newValue != _cameraRotateSpeed)
            {
                _cameraRotateSpeed = newValue;
                EditorUtility.SetDirty(this);
            }
        }
    }

    public float CameraMoveSpeed
    {
        get => _moveSpeed;

        set
        {
            var newValue = Math.Max(value, 0f);

            if (newValue != _moveSpeed)
            {
                _moveSpeed = newValue;
                EditorUtility.SetDirty(this);
            }
        }
    }

    public int CameraInertia
    {
        get => _inertia;

        set
        {
            var newValue = Math.Max(value, 1);

            if (newValue != _inertia)
            {
                _inertia = newValue;
                EditorUtility.SetDirty(this);
            }
        }
    }

    public void ParameterReset()
    {
        _cameraRotateSpeed = 2f;
        _moveSpeed = 0.05f;
        _inertia = 6;

        EditorUtility.SetDirty(this);
    }

    [CustomEditor(typeof(FlowPaintToolEditorData))]
    public class FlowPaintToolEditorData_InspectorUI : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(20f);

            if (GUILayout.Button("Reset"))
            {
                FlowPaintToolEditorData _instance = target as FlowPaintToolEditorData;
                _instance.ParameterReset();
            }
        }
    }
}
