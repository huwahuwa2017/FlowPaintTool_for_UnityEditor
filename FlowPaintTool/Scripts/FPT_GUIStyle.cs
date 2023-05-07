using UnityEngine;

namespace FlowPaintTool
{
    public static class FPT_GUIStyle
    {
        private static GUIStyle _box = GenerateFPTGUI_Box();
        private static GUIStyle _centerLabel = GenerateFPTGUI_CenterLabel();
        private static GUIStyle _bigCenterLabel = GenerateFPTGUI_BigCenterLabel();

        public static GUIStyle GetBox()
        {
            return _box;
        }

        public static GUIStyle GetCenterLabel()
        {
            return _centerLabel;
        }

        public static GUIStyle GetBigCenterLabel()
        {
            return _bigCenterLabel;
        }



        private static GUIStyle GenerateFPTGUI_Box()
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

            Texture2D texture = new Texture2D(4, 4);
            texture.SetPixels32(colors, 0);
            texture.Apply();

            temp.normal.background = texture;

            return temp;
        }

        private static GUIStyle GenerateFPTGUI_CenterLabel()
        {
            GUIStyle temp = new GUIStyle(GUI.skin.label);
            temp.alignment = TextAnchor.MiddleCenter;
            return temp;
        }

        private static GUIStyle GenerateFPTGUI_BigCenterLabel()
        {
            GUIStyle temp = new GUIStyle(GUI.skin.label);
            temp.alignment = TextAnchor.MiddleCenter;
            temp.fontSize *= 2;
            return temp;
        }
    }
}
