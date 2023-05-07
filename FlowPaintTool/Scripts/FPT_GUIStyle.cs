#if UNITY_EDITOR

using UnityEngine;

namespace FlowPaintTool
{
    public static class FPT_GUIStyle
    {
        private static Texture2D _windowTexture = null;
        private static Texture2D _boxTexture = null;

        private static GUIStyle _window = Generate_Window();
        private static GUIStyle _box = Generate_Box();
        private static GUIStyle _centerLabel = Generate_CenterLabel();
        private static GUIStyle _bigCenterLabel = Generate_BigCenterLabel();

        public static GUIStyle GetWindow() => _window;
        public static GUIStyle GetBox() => _box;
        public static GUIStyle GetCenterLabel() => _centerLabel;
        public static GUIStyle GetBigCenterLabel() => _bigCenterLabel;

        private static GUIStyle Generate_Window()
        {
            GUIStyle temp = new GUIStyle
            {
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(8, 8, 8, 8),
                overflow = new RectOffset(0, 0, 0, 0)
            };

            Color32 color0 = new Color32(0, 0, 0, 255);
            Color32 color1 = new Color32(64, 64, 64, 255);

            Color32[] colors = new Color32[]
            {
                color0, color0, color0, color0,
                color0, color1, color1, color0,
                color0, color1, color1, color0,
                color0, color0, color0, color0
            };

            _windowTexture = new Texture2D(4, 4);
            _windowTexture.SetPixels32(colors, 0);
            _windowTexture.Apply();

            temp.normal.background = _windowTexture;

            return temp;
        }

        private static GUIStyle Generate_Box()
        {
            GUIStyle temp = new GUIStyle
            {
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(4, 4, 4, 4),
                overflow = new RectOffset(0, 0, 0, 0)
            };

            Color32 color0 = GUI.skin.label.normal.textColor;
            Color32 color1 = new Color32(0, 0, 0, 0);

            Color32[] colors = new Color32[]
            {
                color0, color0, color0, color0,
                color0, color1, color1, color0,
                color0, color1, color1, color0,
                color0, color0, color0, color0
            };

            _boxTexture = new Texture2D(4, 4);
            _boxTexture.SetPixels32(colors, 0);
            _boxTexture.Apply();

            temp.normal.background = _boxTexture;

            return temp;
        }

        private static GUIStyle Generate_CenterLabel()
        {
            GUIStyle temp = new GUIStyle(GUI.skin.label);
            temp.alignment = TextAnchor.MiddleCenter;
            return temp;
        }

        private static GUIStyle Generate_BigCenterLabel()
        {
            GUIStyle temp = new GUIStyle(GUI.skin.label);
            temp.alignment = TextAnchor.MiddleCenter;
            temp.fontSize *= 2;
            return temp;
        }
    }
}

#endif