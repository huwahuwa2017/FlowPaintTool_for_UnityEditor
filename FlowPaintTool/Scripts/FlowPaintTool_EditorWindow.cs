using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    public class FlowPaintTool_EditorWindow : EditorWindow
    {
        [MenuItem("FlowPaintTool/Open")]
        private static void Open()
        {
            GetWindow<FlowPaintTool_EditorWindow>("FlowPaintTool");
        }



        private Vector2 _scrollPosition = Vector2.zero;

        private FlowPaintToolData _fptData = FlowPaintToolData.Constructor();

        private bool CheckError()
        {
            bool sRGB = _fptData.CheckTextureAndSRGB();

            bool isError = false;

            if (_fptData._startMesh == null)
            {
                EditorGUILayout.HelpBox("Mesh not set", MessageType.Error);
                isError = true;
            }
            else
            {
                if (!_fptData._startMesh.isReadable)
                {
                    EditorGUILayout.HelpBox("Please allow Read/Write for the mesh", MessageType.Error);
                    isError = true;
                }
            }

            if (sRGB && (_fptData._paintMode == PaintMode.FlowPaintMode))
            {
                EditorGUILayout.HelpBox("Using sRGB textures in FlowPaintMode will not give accurate results\nPlease turn off sRGB", MessageType.Error);
                isError = true;
            }

            return isError;
        }

        private void OnGUI()
        {
            bool started = EditorApplication.isPlaying;

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Space(40);

            if (started)
            {
                _fptData._paintMode = (PaintMode)EditorGUILayout.EnumPopup("PaintMode", _fptData._paintMode);

                GUILayout.Space(20);

                _fptData._startMesh = (Mesh)EditorGUILayout.ObjectField("StartMesh", _fptData._startMesh, typeof(Mesh), true);

                GUILayout.Space(20);

                _fptData._outputTextureResolution.x = EditorGUILayout.IntField("OutputTextureWidth", _fptData._outputTextureResolution.x);
                _fptData._outputTextureResolution.y = EditorGUILayout.IntField("OutputTextureHeight", _fptData._outputTextureResolution.y);

                GUILayout.Space(20);

                _fptData._startTextureLoadMode = (StartTextureLoadMode)EditorGUILayout.EnumPopup("StartTextureLoadMode", _fptData._startTextureLoadMode);

                if (_fptData._startTextureLoadMode == StartTextureLoadMode.Assets)
                {
                    _fptData._startTexture = (Texture)EditorGUILayout.ObjectField("StartTexture", _fptData._startTexture, typeof(Texture), true);
                }
                else if (_fptData._startTextureLoadMode == StartTextureLoadMode.FilePath)
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

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Advanced Settings");
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                _fptData._bleedRange = EditorGUILayout.IntField("BleedRange", _fptData._bleedRange);
                _fptData._uv_Epsilon = EditorGUILayout.FloatField("uv_Epsilon", _fptData._uv_Epsilon);

                GUILayout.Space(40);

                bool isError = CheckError();

                if (GUILayout.Button("Generate Paint tool object") && !isError)
                {
                    FlowPaintToolControl[] flowPaintToolControl = FindObjectsOfType<FlowPaintToolControl>();

                    if (flowPaintToolControl.Length == 0)
                    {
                        GameObject go0 = new GameObject("PaintToolControl");
                        go0.AddComponent<FlowPaintToolControl>().FPT_EditorWindow = this;
                    }

                    GameObject go1 = new GameObject("PaintTool");
                    FlowPaintTool fpt = go1.AddComponent<FlowPaintTool>();
                    fpt.SetData(_fptData);

                    GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow"));
                    Selection.instanceIDs = new int[] { go1.GetInstanceID() };
                }
            }
            else
            {
                if (GUILayout.Button("Play mode start"))
                {
                    EditorApplication.isPlaying = true;
                }
            }

            GUILayout.EndScrollView();
        }
    }
}