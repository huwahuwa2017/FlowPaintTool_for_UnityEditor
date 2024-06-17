#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace FlowPaintTool
{
    public static class FPT_TextureOperation
    {
        public static void ClearRenderTexture(RenderTexture rt, Color color)
        {
            RenderTexture temp = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, color);
            RenderTexture.active = temp;
        }

        public static Texture2D GenerateTexture2D(RenderTexture renderTexture)
        {
            int mipCount = renderTexture.mipmapCount;
            TextureCreationFlags textureCreationFlags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            return new Texture2D(renderTexture.width, renderTexture.height, renderTexture.graphicsFormat, mipCount, textureCreationFlags);
        }

        public static void DataTransfer(RenderTexture source, Texture2D dest)
        {
            RenderTexture temp = RenderTexture.active;
            RenderTexture.active = source;
            dest.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            //dest.Apply();
            RenderTexture.active = temp;
        }

        public static void OpenDialog(RenderTexture renderTexture, string title = "")
        {
            Texture2D copyTexture2D = GenerateTexture2D(renderTexture);
            DataTransfer(renderTexture, copyTexture2D);
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