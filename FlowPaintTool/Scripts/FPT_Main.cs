#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    public class FPT_Main : MonoBehaviour
    {
        private static FPT_Main _activeInstance = null;

        public static FPT_Main GetActiveInstance()
        {
            if (_activeInstance != null && _activeInstance._selected)
            {
                return _activeInstance;
            }

            return null;
        }



        private FPT_MainData _fptData = default;
        private FPT_MeshProcess _meshProcess = null;
        private FPT_ShaderProcess _shaderProcess = null;

        private GameObject _paintRender = null;
        private GameObject _maskRender = null;
        private GameObject _meshColider = null;

        private bool _selected = false;

        private void Start()
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                string log = (_fptData._actualSRGB) ? "sRGB enabled" : "sRGB disabled";
                Debug.Log(log);
            }

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            _meshProcess = new FPT_MeshProcess(_fptData);
            _shaderProcess = new FPT_ShaderProcess(_fptData, _meshProcess, GetInstanceID());

            _paintRender = new GameObject("PaintRender");
            _paintRender.transform.SetParent(transform, false);
            _paintRender.AddComponent<MeshFilter>().sharedMesh = _fptData._startMesh;
            _shaderProcess.PaintRenderMaterialArray(_paintRender.AddComponent<MeshRenderer>());

            _maskRender = new GameObject("MaskRender");
            _maskRender.transform.SetParent(transform, false);
            _maskRender.AddComponent<MeshFilter>().sharedMesh = _meshProcess.GetMaskModeMesh();
            _maskRender.AddComponent<MeshRenderer>().sharedMaterials = new Material[]
            {
                FPT_Assets.GetStaticInstance().GetMaterial_MaskOff(),
                FPT_Assets.GetStaticInstance().GetMaterial_MaskOn()
            };

            _meshColider = new GameObject("MeshColider");
            _meshColider.transform.SetParent(transform, false);
            _meshColider.AddComponent<MeshCollider>().sharedMesh = _fptData._startMesh;

            sw.Stop();
            Debug.Log("Start calculation time : " + sw.Elapsed);

            FPT_EditorData.GetStaticInstance().DisableMaterialView();
            FPT_EditorWindow.GetInspectorWindow();
            Selection.instanceIDs = new int[] { gameObject.GetInstanceID() };
            Undo.ClearAll();
        }



        private void Update()
        {
            _selected = Selection.activeTransform == transform;

            if (_selected)
            {
                _activeInstance = this;
            }
        }



        private void FixedUpdate()
        {
            FPT_EditorData editorData = FPT_EditorData.GetStaticInstance();

            _meshProcess.CenterRecalculation(transform.localToWorldMatrix);
            _shaderProcess.MaterialUpdate();

            if (!_selected)
            {
                _paintRender.SetActive(false);
                _maskRender.SetActive(false);

                if (GetActiveInstance() == null)
                {
                    _fptData._sorceRenderer.enabled = true;
                }
            }
            else if (editorData.GetEnableMaskMode())
            {
                _paintRender.SetActive(false);
                _maskRender.SetActive(true);
                _fptData._sorceRenderer.enabled = false;

                _meshProcess.MaskPaint();
            }
            else
            {
                _paintRender.SetActive(!editorData.GetEnablePreviewMode());
                _maskRender.SetActive(false);
                _fptData._sorceRenderer.enabled = editorData.GetEnablePreviewMode();

                _shaderProcess.PaintProcess(transform.localToWorldMatrix);
            }
        }



        public void SetData(FPT_MainData fptData)
        {
            _fptData = fptData;
        }

        public void LinkedUnmask()
        {
            _meshProcess.LinkedUnmask();
        }

        public void LinkedMask()
        {
            _meshProcess.LinkedMask();
        }

        public void RenderTextureUndo()
        {
            _shaderProcess.RenderTextureUndo();
        }

        public void RenderTextureRedo()
        {
            _shaderProcess.RenderTextureRedo();
        }



        [CustomEditor(typeof(FPT_Main))]
        public class FPT_Main_InspectorUI : Editor
        {
            private FPT_Main _instance = null;

            private void OnEnable()
            {
                _instance = target as FPT_Main;
            }

            public override void OnInspectorGUI()
            {
                FPT_EditorData.GetStaticInstance().InspectorGUI(_instance._meshProcess, _instance._shaderProcess, _instance._fptData._paintMode);
            }
        }
    }
}

#endif