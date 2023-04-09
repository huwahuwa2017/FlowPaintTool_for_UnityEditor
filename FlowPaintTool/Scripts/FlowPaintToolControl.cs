using System;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    public class FlowPaintToolControl : MonoBehaviour
    {
        [SerializeField]
        private GameObject _rangeVisualizationPrefab = null;

        private GameObject _rangeVisualization = null;

        private bool _preInputKeyTab = false;
        private bool _preInputKeyZ = false;

        public bool Started { get; set; } = false;

        public FlowPaintTool_EditorWindow FPT_EditorWindow { get; set; } = null;

        private void Start()
        {
            Started = true;

            _rangeVisualization = Instantiate(_rangeVisualizationPrefab);
            _rangeVisualization.transform.SetParent(transform, false);

            Camera camera = Camera.main;
            camera.nearClipPlane = Math.Min(camera.nearClipPlane, 0.01f);
            camera.gameObject.AddComponent<CameraControl2>();
        }

        private void Update()
        {
            FlowPaintTool.BrushSize = Math.Max(FlowPaintTool.BrushSize, 0f);
            FlowPaintTool.BrushStrength = Mathf.Clamp01(FlowPaintTool.BrushStrength);

            float scrollDelta = Input.mouseScrollDelta.y;

            bool inspectorUpdate = false;

            if (Input.GetKey(KeyCode.R))
            {
                FlowPaintTool.BrushSize *= 1f + (scrollDelta * 0.05f);
                inspectorUpdate = true;
            }

            if (Input.GetKey(KeyCode.F))
            {
                FlowPaintTool.BrushStrength *= 1f + (scrollDelta * 0.05f);
                inspectorUpdate = true;
            }

            bool inputKeyTab = Input.GetKey(KeyCode.Tab);

            if (!_preInputKeyTab && inputKeyTab)
            {
                FlowPaintTool.EnableMaskMode = !FlowPaintTool.EnableMaskMode;
                inspectorUpdate = true;
            }

            _preInputKeyTab = inputKeyTab;

            bool inputKeyZ = Input.GetKey(KeyCode.Z);

            if (!_preInputKeyZ && inputKeyZ)
            {
                FlowPaintTool.EnableMaterialView = !FlowPaintTool.EnableMaterialView;
                inspectorUpdate = true;
            }

            _preInputKeyZ = inputKeyZ;

            if (inspectorUpdate && (FPT_EditorWindow != null))
            {
                FPT_EditorWindow.Repaint();

                Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
                EditorWindow inspectorWindow = EditorWindow.GetWindow(type, false, null, false);
                inspectorWindow.Repaint();
            }
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

        [CustomEditor(typeof(FlowPaintToolControl))]
        public class FlowPaintToolControl_InspectorUI : Editor
        {
            public override void OnInspectorGUI()
            {
            }
        }
    }
}