using System;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    public class FPT_EditorWindow : EditorWindow
    {
        public static FPT_EditorData EditorDataInstance { get; private set; }

        public static FPT_Assets RequestAssetsInstance { get; private set; }

        public static EditorWindow GetInspectorWindow(bool utility = false, string title = null, bool focus = true)
        {
            Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
            return GetWindow(type, utility, title, focus);
        }

        [MenuItem("FlowPaintTool/Open")]
        private static void Open()
        {
            GetWindow<FPT_EditorWindow>("FlowPaintTool");
        }



        [SerializeField]
        private FPT_EditorData _editorData = null;

        [SerializeField]
        private FPT_Assets _requestAssets = null;

        private FPT_MainData _fptData = FPT_MainData.Constructor();

        private void OnGUI()
        {
            if (EditorDataInstance == null)
            {
                EditorDataInstance = _editorData;
            }

            if (RequestAssetsInstance == null)
            {
                RequestAssetsInstance = _requestAssets;
            }



            bool started = EditorApplication.isPlaying;

            GUILayout.Space(40);

            if (!started)
            {
                GUILayout.Label("3D Flow Paint Tool", FPT_GUIStyle.GetBigCenterLabel());
                GUILayout.Label("Version 24", FPT_GUIStyle.GetBigCenterLabel());

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

            FPT_Main fpt0 = selectTransform.gameObject.GetComponent<FPT_Main>();

            if (fpt0 != null)
            {
                EditorGUILayout.HelpBox("The paint tool is ready", MessageType.Info);
                return;
            }



            _fptData.EditorWindowGUI(selectTransform);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
