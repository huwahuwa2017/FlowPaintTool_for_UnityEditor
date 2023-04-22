using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    public class FlowPaintTool_EditorWindow : EditorWindow
    {
        public static GUIStyle CenterLabel { get; private set; } = null;

        public static GUIStyle BigCenterLabel { get; private set; } = null;

        [MenuItem("FlowPaintTool/Open")]
        private static void Open()
        {
            GetWindow<FlowPaintTool_EditorWindow>("FlowPaintTool");
        }



        private FlowPaintToolData _fptData = FlowPaintToolData.Constructor();

        private bool CheckError(bool flagSMR, bool flagMFMR)
        {
            bool isError = false;

            if (_fptData.SRGBCheck() && (_fptData._paintMode == PaintModeEnum.FlowPaintMode))
            {
                EditorGUILayout.HelpBox("Using sRGB textures in FlowPaintMode will not give accurate results\nPlease turn off sRGB", MessageType.Error);
                isError = true;
            }

            if (!(flagSMR || flagMFMR))
            {
                EditorGUILayout.HelpBox("Please use either SkinnedMeshRenderer or a combination of MeshFilter and MeshRenderer", MessageType.Error);
                return true;
            }

            if (_fptData._startMesh == null)
            {
                EditorGUILayout.HelpBox("Mesh not set", MessageType.Error);
                return true;
            }

            if (!_fptData._startMesh.isReadable)
            {
                EditorGUILayout.HelpBox("Please allow Read/Write for the mesh", MessageType.Error);
                return true;
            }

            List<Vector2> temp0 = new List<Vector2>();
            _fptData._startMesh.GetUVs(_fptData._targetUVChannel, temp0);

            if (temp0.Count == 0)
            {
                EditorGUILayout.HelpBox($"UV coordinate does not exist in UVchannel {_fptData._targetUVChannel}", MessageType.Error);
                return true;
            }

            return isError;
        }

        private void OnGUI()
        {
            GUIStyle temp0 = new GUIStyle(GUI.skin.label);
            temp0.alignment = TextAnchor.MiddleCenter;
            CenterLabel = temp0;

            GUIStyle temp1 = new GUIStyle(temp0);
            temp1.fontSize = temp1.fontSize * 2;
            BigCenterLabel = temp1;



            bool started = EditorApplication.isPlaying;

            GUILayout.Space(40);

            if (!started)
            {
                GUILayout.Label("3D Flow Paint Tool", BigCenterLabel);
                GUILayout.Label("Version 16", BigCenterLabel);

                GUILayout.Space(40);

                if (GUILayout.Button("Check GitHub"))
                {
                    Application.OpenURL("https://github.com/huwahuwa2017/3D_FlowPaintTool/releases");
                }

                GUILayout.Space(40);

                if (GUILayout.Button("Play mode start"))
                {
                    EditorApplication.isPlaying = true;
                }

                return;
            }

            Transform selectTransform = Selection.activeTransform;

            if (selectTransform == null)
            {
                EditorGUILayout.HelpBox("Please select only one GameObject", MessageType.Info);
                return;
            }

            FlowPaintTool fpt0 = selectTransform.gameObject.GetComponent<FlowPaintTool>();

            if (fpt0 != null)
            {
                EditorGUILayout.HelpBox("The paint tool is ready", MessageType.Info);
                return;
            }

            _fptData._paintMode = (PaintModeEnum)EditorGUILayout.EnumPopup("PaintMode", _fptData._paintMode);

            GUILayout.Space(20);

            _fptData._outputTextureResolution.x = EditorGUILayout.IntField("OutputTextureWidth", _fptData._outputTextureResolution.x);
            _fptData._outputTextureResolution.y = EditorGUILayout.IntField("OutputTextureHeight", _fptData._outputTextureResolution.y);

            GUILayout.Space(20);

            _fptData._startTextureLoadMode = (StartTextureLoadModeEnum)EditorGUILayout.EnumPopup("StartTextureLoadMode", _fptData._startTextureLoadMode);

            if (_fptData._startTextureLoadMode == StartTextureLoadModeEnum.Assets)
            {
                _fptData._startTexture = (Texture)EditorGUILayout.ObjectField("StartTexture", _fptData._startTexture, typeof(Texture), true);
            }
            else if (_fptData._startTextureLoadMode == StartTextureLoadModeEnum.FilePath)
            {
                if (GUILayout.Button("Open file panel"))
                {
                    string filePath = EditorUtility.OpenFilePanel("Select Texture", string.Empty, string.Empty);

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        _fptData._startTextureFilePath = filePath;
                    }
                }

                _fptData._startTextureFilePath = EditorGUILayout.TextField("FilePath", _fptData._startTextureFilePath);
                _fptData._startTextureSRGB = EditorGUILayout.Toggle("sRGB (Color Texture)", _fptData._startTextureSRGB);
            }

            GUILayout.Space(20);

            GUILayout.Label("Advanced Settings", CenterLabel);

            _fptData._targetUVChannel = EditorGUILayout.IntField("TargetUVChannel", _fptData._targetUVChannel);
            _fptData._bleedRange = EditorGUILayout.IntField("BleedRange", _fptData._bleedRange);
            _fptData._uv_Epsilon = EditorGUILayout.FloatField("UV_Epsilon", _fptData._uv_Epsilon);

            GUILayout.Space(40);

            _fptData._startMesh = null;
            GameObject selectObject = selectTransform.gameObject;

            SkinnedMeshRenderer temp3 = selectObject.GetComponent<SkinnedMeshRenderer>();
            MeshFilter temp4 = selectObject.GetComponent<MeshFilter>();
            MeshRenderer temp5 = selectObject.GetComponent<MeshRenderer>();

            bool flagSMR = temp3 != null;
            bool flagMFMR = (temp4 != null) && (temp5 != null);

            if (flagSMR)
            {
                _fptData._startMesh = temp3.sharedMesh;
            }
            else if (flagMFMR)
            {
                _fptData._startMesh = temp4.sharedMesh;
            }

            _fptData.ConsistencyCheck();
            bool isError = CheckError(flagSMR, flagMFMR);

            if (GUILayout.Button("Generate Paint tool object") && !isError)
            {
                FlowPaintToolControl[] flowPaintToolControl = FindObjectsOfType<FlowPaintToolControl>();

                if (flowPaintToolControl.Length == 0)
                {
                    GameObject go0 = new GameObject("PaintToolControl");
                    go0.AddComponent<FlowPaintToolControl>().FPT_EditorWindow = this;
                }

                if (flagSMR)
                {
                    _fptData._sorceRenderer = temp3;
                }
                else if (flagMFMR)
                {
                    _fptData._sorceRenderer = temp5;
                }

                GameObject go1 = new GameObject("PaintTool");
                go1.transform.SetParent(selectTransform, false);
                FlowPaintTool fpt1 = go1.AddComponent<FlowPaintTool>();
                fpt1.SetData(_fptData);

                GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow"));
                Selection.instanceIDs = new int[] { go1.GetInstanceID() };
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}