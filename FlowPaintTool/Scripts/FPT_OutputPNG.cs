using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    public static class FPT_OutputPNG
    {
        public static Texture2D RenderTextureToTexture2D(RenderTexture renderTexture)
        {
            RenderTexture temp = RenderTexture.active;
            RenderTexture.active = renderTexture;

            int width = renderTexture.width;
            int height = renderTexture.height;
            Texture2D copyTexture2D = new Texture2D(width, height);
            copyTexture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            copyTexture2D.Apply();

            RenderTexture.active = temp;

            return copyTexture2D;
        }

        public static void OutputDialog(RenderTexture renderTexture, bool sRGB = true, string title = "")
        {
            Texture2D copyTexture2D = RenderTextureToTexture2D(renderTexture);
            OutputDialog(copyTexture2D, sRGB, title);
            UnityEngine.Object.Destroy(copyTexture2D);
        }

        public static void OutputDialog(Texture2D texture2D, bool sRGB = true, string title = "")
        {
            try
            {
                string absolutePath = EditorUtility.SaveFilePanel(title, "Assets", "texture", "png");

                if (string.IsNullOrEmpty(absolutePath))
                    return;

                string relativePath = string.Empty;
                bool existTextureImporter = false;
                string dataPath = Application.dataPath;
                bool isStartsDataPath = absolutePath.StartsWith(dataPath);

                if (isStartsDataPath)
                {
                    relativePath = absolutePath.Remove(0, dataPath.Length - 6);
                    existTextureImporter = AssetImporter.GetAtPath(relativePath) is TextureImporter;

                    File.WriteAllBytes(absolutePath, texture2D.EncodeToPNG());
                    AssetDatabase.ImportAsset(relativePath);
                }
                else
                {
                    File.WriteAllBytes(absolutePath, texture2D.EncodeToPNG());
                }

                Debug.Log("Output path : " + absolutePath);

                if (existTextureImporter)
                    return;

                int tempMax = Math.Max(texture2D.width, texture2D.height);
                tempMax = (int)Math.Pow(2, Math.Ceiling(Math.Log(tempMax, 2)));

                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                importer.sRGBTexture = sRGB;
                importer.maxTextureSize = tempMax;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }
    }
}
