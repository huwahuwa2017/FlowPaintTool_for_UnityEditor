#if UNITY_EDITOR

using System.Linq;
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
        private FPT_ShaderProcess _shaderProcess = null;
        private FPT_EditorData _fptEditorData = null;

        private GameObject _paintRenderObject = null;
        private GameObject _maskRenderObject = null;

        private SkinnedMeshRenderer _sourceSMR = null;
        private SkinnedMeshRenderer _paintSMR = null;
        private SkinnedMeshRenderer _maskSMR = null;

        private bool _selected = false;
        private bool _preSelected = false;
        private bool _isSkinnedMeshRenderer = false;

        private bool _forceDrawing = false;

        [ContextMenu("ForceDrawingOn")]
        private void ForceDrawingOn()
        {
            _forceDrawing = true;
        }

        [ContextMenu("ForceDrawingOff")]
        private void ForceDrawingOff()
        {
            _forceDrawing = false;
        }



        public void ManualStart(FPT_MainData fptData)
        {
            _fptData = fptData;
            _fptEditorData = FPT_EditorData.GetSingleton();

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            _shaderProcess = new FPT_ShaderProcess(_fptData, GetInstanceID());

            Material offMaterial = FPT_Assets.GetSingleton().GetMaskOff_Material();

            Material[] paintRenderMaterials = Enumerable.Repeat(offMaterial, _fptData._startMesh.subMeshCount).ToArray();
            paintRenderMaterials[_fptData._targetSubMesh] = _shaderProcess.GetResultMaterial();

            Material[] maskRenderMaterials = Enumerable.Repeat(offMaterial, _fptData._startMesh.subMeshCount).ToArray();
            maskRenderMaterials[_fptData._targetSubMesh] = _shaderProcess.GetPolygonMaskResultMaterial();

            _paintRenderObject = new GameObject("PaintRender");
            _paintRenderObject.transform.SetParent(transform, false);
            _maskRenderObject = new GameObject("MaskRender");
            _maskRenderObject.transform.SetParent(transform, false);

            if (_fptData._sorceRenderer is SkinnedMeshRenderer srcsmr)
            {
                _isSkinnedMeshRenderer = true;

                _sourceSMR = srcsmr;

                _paintSMR = _paintRenderObject.AddComponent<SkinnedMeshRenderer>();
                _paintSMR.localBounds = _sourceSMR.localBounds;
                _paintSMR.bones = _sourceSMR.bones;
                _paintSMR.sharedMesh = _fptData._startMesh;
                _paintSMR.sharedMaterials = paintRenderMaterials;

                _maskSMR = _maskRenderObject.AddComponent<SkinnedMeshRenderer>();
                _maskSMR.localBounds = _sourceSMR.localBounds;
                _maskSMR.bones = _sourceSMR.bones;
                _maskSMR.sharedMesh = _fptData._startMesh;
                _maskSMR.sharedMaterials = maskRenderMaterials;
            }
            else
            {
                _paintRenderObject.AddComponent<MeshFilter>().sharedMesh = _fptData._startMesh;
                _paintRenderObject.AddComponent<MeshRenderer>().sharedMaterials = paintRenderMaterials;

                _maskRenderObject.AddComponent<MeshFilter>().sharedMesh = _fptData._startMesh;
                _maskRenderObject.AddComponent<MeshRenderer>().sharedMaterials = maskRenderMaterials;
            }

            _fptEditorData.DisablePreviewMode();
            FPT_EditorWindow.GetInspectorWindow();
            Selection.instanceIDs = new int[] { gameObject.GetInstanceID() };
            Undo.ClearAll();

            sw.Stop();
            Debug.Log("Start calculation time : " + sw.Elapsed);
        }



        private void FixedUpdate()
        {
            _shaderProcess.MaterialFixedUpdate();

            _selected = transform == Selection.activeTransform;

            if (_selected)
            {
                _activeInstance = this;

                if (_fptEditorData.GetEnableMaskMode())
                {
                    _fptData._sorceRenderer.enabled = false;
                    _paintRenderObject.SetActive(false);
                    _maskRenderObject.SetActive(true);

                    _shaderProcess.MaskProcess();
                }
                else
                {
                    bool enablePreviewMode = _fptEditorData.GetEnablePreviewMode();

                    _fptData._sorceRenderer.enabled = enablePreviewMode;
                    _paintRenderObject.SetActive(!enablePreviewMode);
                    _maskRenderObject.SetActive(false);

                    _shaderProcess.PaintProcess(_forceDrawing);
                }
            }
            else
            {
                _paintRenderObject.SetActive(false);
                _maskRenderObject.SetActive(false);

                if (_preSelected)
                {
                    _fptData._sorceRenderer.enabled = true;
                }
            }

            if (_isSkinnedMeshRenderer)
            {
                int blendShapeCount = _sourceSMR.sharedMesh.blendShapeCount;

                for (int index = 0; index < blendShapeCount; ++index)
                {
                    float val = _sourceSMR.GetBlendShapeWeight(index);
                    _paintSMR.SetBlendShapeWeight(index, val);
                    _maskSMR.SetBlendShapeWeight(index, val);
                }

                _paintSMR.localBounds = _sourceSMR.localBounds;
                _maskSMR.localBounds = _sourceSMR.localBounds;
            }

            _preSelected = _selected;
        }



        public void LinkedUnmask()
        {
            _shaderProcess.UnmaskLinked();
        }

        public void LinkedMask()
        {
            _shaderProcess.MaskLinked();
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
                FPT_EditorData.GetSingleton().InspectorGUI(_instance._shaderProcess, _instance._fptData._paintMode);
            }
        }
    }
}

#endif
