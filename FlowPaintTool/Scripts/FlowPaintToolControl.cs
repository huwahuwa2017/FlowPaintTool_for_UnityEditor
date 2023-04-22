using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    public class FlowPaintToolControl : MonoBehaviour
    {
        private static FlowPaintToolEditorData _fptEditorData = null;

        public static FlowPaintToolEditorData FPT_EditorData => _fptEditorData;



        [SerializeField]
        private GameObject _rangeVisualizationPrefab = null;

        private GameObject _rangeVisualization = null;

        private bool _preInputKeyTab = false;
        private bool _preInputKeyZ = false;
        private bool _preInputKeyPlus = false;
        private bool _preInputKeyMinus = false;

        private bool _focus = false;

        public FlowPaintTool_EditorWindow FPT_EditorWindow { get; set; } = null;

        private void Start()
        {
            string path = AssetDatabase.GetAssetPath(_rangeVisualizationPrefab);
            path = Path.Combine(Path.GetDirectoryName(path), "FlowPaintToolEditorData.asset");

            _fptEditorData = AssetDatabase.LoadAssetAtPath<FlowPaintToolEditorData>(path);

            if (_fptEditorData == null)
            {
                _fptEditorData = ScriptableObject.CreateInstance<FlowPaintToolEditorData>();
                AssetDatabase.CreateAsset(_fptEditorData, path);
            }

            _rangeVisualization = Instantiate(_rangeVisualizationPrefab);
            _rangeVisualization.transform.SetParent(transform, false);

            Camera camera = Camera.main;
            camera.nearClipPlane = Math.Min(camera.nearClipPlane, 0.01f);
            camera.gameObject.AddComponent<CameraControl2>();
        }

        private void Update()
        {
            bool inputKeyTab = Input.GetKey(KeyCode.Tab);
            bool inputKeyZ = Input.GetKey(KeyCode.Z);
            bool inputKeyPlus = Input.GetKey(KeyCode.KeypadPlus);
            bool inputKeyMinus = Input.GetKey(KeyCode.KeypadMinus);



            float scrollDelta = Input.mouseScrollDelta.y;

            bool inspectorUpdate = false;

            if (Input.GetKey(KeyCode.R))
            {
                FlowPaintTool.BrushSize += Math.Max(FlowPaintTool.BrushSize, 0.001f) * scrollDelta * 0.1f;
                inspectorUpdate = true;
            }

            if (Input.GetKey(KeyCode.F))
            {
                FlowPaintTool.BrushStrength += scrollDelta * 0.05f;
                inspectorUpdate = true;
            }

            if (!_preInputKeyTab && inputKeyTab)
            {
                FlowPaintTool.EnableMaskMode = !FlowPaintTool.EnableMaskMode;
                inspectorUpdate = true;
            }

            if (!_preInputKeyZ && inputKeyZ)
            {
                FlowPaintTool.EnableMaterialView = !FlowPaintTool.EnableMaterialView;
                inspectorUpdate = true;
            }

            if (inspectorUpdate && (FPT_EditorWindow != null))
            {
                FPT_EditorWindow.Repaint();

                Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
                EditorWindow inspectorWindow = EditorWindow.GetWindow(type, false, null, false);
                inspectorWindow.Repaint();
            }



            FlowPaintTool fpt = FlowPaintTool.ActiveInstance;

            if (fpt != null)
            {
                if (FlowPaintTool.EnableMaskMode)
                {
                    if (!_preInputKeyPlus && inputKeyPlus)
                    {
                        fpt.SelectLinkedPlus();
                    }

                    if (!_preInputKeyMinus && inputKeyMinus)
                    {
                        fpt.SelectLinkedMinus();
                    }
                }
            }



            _preInputKeyTab = inputKeyTab;
            _preInputKeyZ = inputKeyZ;
            _preInputKeyPlus = inputKeyPlus;
            _preInputKeyMinus = inputKeyMinus;
        }

        private void FixedUpdate()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool hit = Physics.Raycast(ray, out RaycastHit raycastHit, 100f);

            _rangeVisualization.SetActive(hit);
            Transform temp0 = _rangeVisualization.transform;
            temp0.position = raycastHit.point;
            temp0.rotation = Camera.main.transform.rotation;
            temp0.localScale = new Vector3(FlowPaintTool.BrushSize, FlowPaintTool.BrushSize, FlowPaintTool.BrushSize) * 2f;
        }

        private void OnGUI()
        {
            if (!_focus)
            {
                GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
                GUILayout.Label("Paused", GUI.skin.box);
                GUILayout.FlexibleSpace();
                GUILayout.Label("Paused", GUI.skin.box);
                GUILayout.EndArea();
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            _focus = focus;
        }



        [CustomEditor(typeof(FlowPaintToolControl))]
        public class FlowPaintToolControl_InspectorUI : Editor
        {
            public override void OnInspectorGUI()
            {
            }
        }
    }
}