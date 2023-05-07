using System;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    public class FPT_Parameter : MonoBehaviour
    {
        private FPT_EditorData _editorData = null;

        private GameObject _rangeVisualization = null;

        private bool _preInputKeyTab = false;
        private bool _preInputKeyZ = false;
        private bool _preInputKeyPlus = false;
        private bool _preInputKeyMinus = false;
        private bool _preInputKeyLeftBracket = false;
        private bool _preInputKeyRightBracket = false;

        private bool _focus = false;

        private void Start()
        {
            _editorData = FPT_EditorWindow.EditorDataInstance;

            _rangeVisualization = Instantiate(FPT_EditorWindow.RequestAssetsInstance._rangeVisualizationPrefab);
            _rangeVisualization.transform.SetParent(transform, false);

            Camera camera = Camera.main;
            camera.nearClipPlane = Math.Min(camera.nearClipPlane, 0.01f);
            camera.gameObject.AddComponent<FPT_Camera>();
        }

        private void Update()
        {
            bool inputKeyTab = Input.GetKey(KeyCode.Tab);
            bool inputKeyZ = Input.GetKey(KeyCode.Z);
            bool inputKeyPlus = Input.GetKey(KeyCode.KeypadPlus);
            bool inputKeyMinus = Input.GetKey(KeyCode.KeypadMinus);
            bool inputKeyLeftBracket = Input.GetKey(KeyCode.LeftBracket);
            bool inputKeyRightBracket = Input.GetKey(KeyCode.RightBracket);



            float scrollDelta = Input.mouseScrollDelta.y;
            bool repaint = false;

            if (Input.GetKey(KeyCode.R))
            {
                _editorData.ChangeBrushSize(scrollDelta);
                repaint = true;
            }

            if (Input.GetKey(KeyCode.F))
            {
                _editorData.ChangeBrushStrength(scrollDelta);
                repaint = true;
            }

            if (!_preInputKeyTab && inputKeyTab)
            {
                _editorData.ChangeEnableMaskMode();
                repaint = true;
            }

            if (!_preInputKeyZ && inputKeyZ)
            {
                _editorData.ChangeEnableMaterialView();
                repaint = true;
            }

            if (repaint)
            {
                EditorWindow inspectorWindow = FPT_EditorWindow.GetInspectorWindow(false, null, false);
                inspectorWindow.Repaint();
            }



            FPT_Main fpt = FPT_Main.GetActiveInstance();

            if (fpt != null)
            {
                if (_editorData.GetEnableMaskMode())
                {
                    if (!_preInputKeyPlus && inputKeyPlus)
                    {
                        fpt.GetMeshProcess().SelectLinkedPlus();
                    }

                    if (!_preInputKeyMinus && inputKeyMinus)
                    {
                        fpt.GetMeshProcess().SelectLinkedMinus();
                    }
                }
                else
                {
                    repaint = false;

                    if (!_preInputKeyLeftBracket && inputKeyLeftBracket)
                    {
                        fpt.GetShaderProcess().Undo();
                        repaint = true;
                    }

                    if (!_preInputKeyRightBracket && inputKeyRightBracket)
                    {
                        fpt.GetShaderProcess().Redo();
                        repaint = true;
                    }

                    if (repaint)
                    {
                        EditorWindow inspectorWindow = FPT_EditorWindow.GetInspectorWindow(false, null, false);
                        inspectorWindow.Repaint();
                    }
                }
            }



            _preInputKeyTab = inputKeyTab;
            _preInputKeyZ = inputKeyZ;
            _preInputKeyPlus = inputKeyPlus;
            _preInputKeyMinus = inputKeyMinus;
            _preInputKeyLeftBracket = inputKeyLeftBracket;
            _preInputKeyRightBracket = inputKeyRightBracket;
        }

        private void FixedUpdate()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool hit = Physics.Raycast(ray, out RaycastHit raycastHit, 100f);

            float scale = _editorData.GetBrushSize() * 2f;
            _rangeVisualization.SetActive(hit);
            Transform temp0 = _rangeVisualization.transform;
            temp0.position = raycastHit.point;
            temp0.rotation = Camera.main.transform.rotation;
            temp0.localScale = new Vector3(scale, scale, scale);
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



        [CustomEditor(typeof(FPT_Parameter))]
        public class FlowPaintTool_Control_InspectorUI : Editor
        {
            public override void OnInspectorGUI()
            {
            }
        }
    }
}