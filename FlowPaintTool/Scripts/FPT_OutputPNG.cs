#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace FlowPaintTool
{
    public static class FPT_OutputPNG
    {
        public static Texture2D RenderTextureToTexture2D(RenderTexture renderTexture, TextureCreationFlags textureCreationFlags = TextureCreationFlags.None)
        {
            RenderTexture temp = RenderTexture.active;
            RenderTexture.active = renderTexture;

            int width = renderTexture.width;
            int height = renderTexture.height;
            Texture2D copyTexture2D = new Texture2D(width, height, renderTexture.graphicsFormat, textureCreationFlags);
            copyTexture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            copyTexture2D.Apply();

            RenderTexture.active = temp;

            return copyTexture2D;
        }

        public static void OpenDialog(RenderTexture renderTexture, string title = "")
        {
            Texture2D copyTexture2D = RenderTextureToTexture2D(renderTexture);
            OpenDialog(copyTexture2D, title);
            UnityEngine.Object.DestroyImmediate(copyTexture2D);
        }

        public static void OpenDialog(Texture2D texture2D, string title = "")
        {
            try
            {
                string absolutePath = EditorUtility.SaveFilePanel(title, "Assets", "texture", "png");

                if (string.IsNullOrEmpty(absolutePath))
                    return;

                Debug.Log("Output path : " + absolutePath);

                string dataPath = Application.dataPath;

                if (!absolutePath.StartsWith(dataPath))
                {
                    File.WriteAllBytes(absolutePath, texture2D.EncodeToPNG());
                    return;
                }

                string relativePath = absolutePath.Remove(0, dataPath.Length - 6);
                bool existTextureImporter = AssetImporter.GetAtPath(relativePath) is TextureImporter;

                File.WriteAllBytes(absolutePath, texture2D.EncodeToPNG());
                AssetDatabase.ImportAsset(relativePath);

                if (existTextureImporter)
                    return;

                int tempMax = Math.Max(texture2D.width, texture2D.height);
                tempMax = (int)Math.Pow(2, Math.Ceiling(Math.Log(tempMax, 2)));

                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                importer.sRGBTexture = GraphicsFormatUtility.IsSRGBFormat(texture2D.graphicsFormat);
                importer.maxTextureSize = tempMax;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }
    }
}

#endif