#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    public static class FPT_TextureEdit
    {
        [MenuItem("Assets/FlowPaintTool/TextureEditAndOutput")]
        private static void TextureEditAndOutput()
        {
            string path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            Object obj = AssetDatabase.LoadMainAssetAtPath(path);

            if (!(obj is Texture))
            {
                Debug.LogError("No Texture selected");
                Debug.Log("type : " + obj.GetType());
                return;
            }

            Material material = FPT_Assets.GetSingleton().GetTextureEditMaterial();

            Texture texture = obj as Texture;
            RenderTextureDescriptor rtd = new RenderTextureDescriptor(texture.width, texture.height, texture.graphicsFormat, 0);
            RenderTexture rt = new RenderTexture(rtd);

            Graphics.Blit(texture, rt, material);
            FPT_TextureOperation.OutputPNG_OpenDialog(rt);
            //Object.DestroyImmediate(rt);
        }
    }
}

#endif