#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    using TextData = FPT_Language.FPT_EditorWindowText;

    public class FPT_EditorWindow : EditorWindow
    {
        public const string MenuPathJapanese = "FlowPaintTool/Japanese";
        public const string MenuPathEnglish = "FlowPaintTool/English";

        [MenuItem("FlowPaintTool/Open", false, 0)]
        private static void Open()
        {
            GetWindow<FPT_EditorWindow>("FlowPaintTool");
        }

        [MenuItem(MenuPathJapanese, false, 20)]
        private static void Japanese()
        {
            FPT_EditorData.GetStaticInstance().ChangeLanguageType(FPT_LanguageTypeEnum.Japanese);
        }

        [MenuItem(MenuPathEnglish, false, 21)]
        private static void English()
        {
            FPT_EditorData.GetStaticInstance().ChangeLanguageType(FPT_LanguageTypeEnum.English);
        }

        [MenuItem("FlowPaintTool/Reset Parameter", false, 40)]
        private static void ResetParameter()
        {
            FPT_EditorData.GetStaticInstance().ResetParameter();
        }



        public static EditorWindow GetInspectorWindow(bool utility = false, string title = null, bool focus = true)
        {
            Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
            return GetWindow(type, utility, title, focus);
        }

        public static void RepaintInspectorWindow()
        {
            EditorWindow inspectorWindow = GetInspectorWindow(false, null, false);
            inspectorWindow.Repaint();
        }



        private void OnGUI()
        {
            FPT_EditorData.GetStaticInstance().UpdateLanguageType();

            bool started = EditorApplication.isPlaying;

            GUILayout.Space(40);

            if (!started)
            {
                GUILayout.Label("3D Flow Paint Tool", FPT_GUIStyle.GetBigCenterLabel());
                GUILayout.Label("Version 62", FPT_GUIStyle.GetBigCenterLabel());

                GUILayout.Space(40);

                if (GUILayout.Button(TextData.CheckReleases))
                {
                    Application.OpenURL("https://github.com/huwahuwa2017/FlowPaintTool_for_UnityEditor/releases");
                }

                GUILayout.Space(20);

                if (GUILayout.Button(TextData.CheckManual))
                {
                    Application.OpenURL("https://github.com/huwahuwa2017/FlowPaintTool_for_UnityEditor/tree/main#flowpainttool_for_unityeditor");
                }

                return;
            }

            Transform selectTransform = Selection.activeTransform;

            if (selectTransform == null)
            {
                EditorGUILayout.HelpBox(TextData.PleaseSelectOnlyOneGameObject, MessageType.Info);
                return;
            }

            FPT_Main fpt0 = selectTransform.gameObject.GetComponent<FPT_Main>();

            if (fpt0 != null)
            {
                EditorGUILayout.HelpBox(TextData.ThePaintToolIsReady, MessageType.Info);
                return;
            }

            FPT_EditorData.GetStaticInstance().EditorWindowGUI(selectTransform);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}

#endif