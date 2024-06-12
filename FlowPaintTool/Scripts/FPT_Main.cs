#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    public class FPT_Main : MonoBehaviour
    {
        private static FPT_Main _activeInstance = null;

        private static Camera _camera = null;

        public static FPT_Main GetActiveInstance()
        {
            if (_activeInstance != null && _activeInstance._selected)
            {
                return _activeInstance;
            }

            return null;
        }

        public static Camera GetCamera()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                GameObject cameraObject = new GameObject("Camera");
                _camera = cameraObject.AddComponent<Camera>();
            }

            return _camera;
        }



        private FPT_MainData _fptData = default;
        private FPT_MeshProcess _meshProcess = null;
        private FPT_ShaderProcess _shaderProcess = null;
        private FPT_Raycast _raycast = null;

        private GameObject _paintRenderObject = null;
        private GameObject _maskRenderObject = null;

        private bool _selected = false;
        private bool _preSelected = false;

        private void Start()
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                string log = (_fptData._actualSRGB) ? "sRGB enabled" : "sRGB disabled";
                Debug.Log(log);
            }

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            _meshProcess = new FPT_MeshProcess(this, _fptData);
            _shaderProcess = new FPT_ShaderProcess(this, _fptData, _meshProcess, GetInstanceID());
            _raycast = new FPT_Raycast();

            Material[] maskRenderMaterials = new Material[]
            {
                FPT_Assets.GetSingleton().GetMaterial_MaskOff(),
                FPT_Assets.GetSingleton().GetMaterial_MaskOn()
            };

            Material[] paintRenderMaterials = Enumerable.Repeat(maskRenderMaterials[1], _fptData._startMesh.subMeshCount).ToArray();
            paintRenderMaterials[_fptData._targetSubMesh] = _shaderProcess.GetPaintRenderMaterial();

            _paintRenderObject = new GameObject("PaintRender");
            _paintRenderObject.transform.SetParent(transform, false);
            _paintRenderObject.AddComponent<MeshFilter>().sharedMesh = _fptData._startMesh;
            _paintRenderObject.AddComponent<MeshRenderer>().sharedMaterials = paintRenderMaterials;

            _maskRenderObject = new GameObject("MaskRender");
            _maskRenderObject.transform.SetParent(transform, false);
            _maskRenderObject.AddComponent<MeshFilter>().sharedMesh = _meshProcess.GetMaskModeMesh();
            _maskRenderObject.AddComponent<MeshRenderer>().sharedMaterials = maskRenderMaterials;

            sw.Stop();
            Debug.Log("Start calculation time : " + sw.Elapsed);

            FPT_EditorData.GetSingleton().DisableMaterialView();
            FPT_EditorWindow.GetInspectorWindow();
            Selection.instanceIDs = new int[] { gameObject.GetInstanceID() };
            Undo.ClearAll();
        }



        private void FixedUpdate()
        {
            _selected = Selection.activeTransform == transform;

            if (_selected)
            {
                _activeInstance = this;
            }

            _meshProcess.CenterRecalculation(transform.localToWorldMatrix);
            _shaderProcess.MaterialUpdate();

            if (!_selected)
            {
                _paintRenderObject.SetActive(false);
                _maskRenderObject.SetActive(false);

                if (_preSelected)
                {
                    _fptData._sorceRenderer.enabled = true;
                }
            }
            else
            {
                FPT_EditorData editorData = FPT_EditorData.GetSingleton();

                if (editorData.GetEnableMaskMode())
                {
                    _paintRenderObject.SetActive(false);
                    _maskRenderObject.SetActive(true);
                    _fptData._sorceRenderer.enabled = false;

                    _meshProcess.MaskProcess();
                }
                else
                {
                    bool enablePreviewMode = editorData.GetEnablePreviewMode();

                    _paintRenderObject.SetActive(!enablePreviewMode);
                    _maskRenderObject.SetActive(false);
                    _fptData._sorceRenderer.enabled = enablePreviewMode;

                    _shaderProcess.PaintProcess(transform.localToWorldMatrix);
                }
            }

            _preSelected = _selected;
        }



        public void SetData(FPT_MainData fptData)
        {
            _fptData = fptData;
        }

        public bool PaintToolRaycast(out Vector3 point)
        {
            Ray ray = GetCamera().ScreenPointToRay(Input.mousePosition);
            int[] indexs = new int[] { _fptData._targetSubMesh };
            return _raycast.Raycast(_fptData._sorceRenderer, indexs, ray, out point, 1024f);
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
                FPT_EditorData.GetSingleton().InspectorGUI(_instance._meshProcess, _instance._shaderProcess, _instance._fptData._paintMode);
            }
        }
    }
}

#endif