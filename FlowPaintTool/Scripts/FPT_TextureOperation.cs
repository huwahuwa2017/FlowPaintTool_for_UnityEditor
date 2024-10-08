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



        public static Texture2D GenerateTexture2D(RenderTexture renderTexture, string name = "")
        {
            int mipCount = renderTexture.mipmapCount;
            TextureCreationFlags textureCreationFlags = (mipCount != 1) ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
            Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, renderTexture.graphicsFormat, mipCount, textureCreationFlags);
            texture2D.name = name;
            return texture2D;
        }

        public static void DataTransfer(RenderTexture source, Texture2D dest)
        {
            RenderTexture temp = RenderTexture.active;
            RenderTexture.active = source;
            dest.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            //dest.Apply();
            RenderTexture.active = temp;
        }



        public static void OutputPNG(string outputPath, Texture2D texture2D)
        {
            try
            {
                if (string.IsNullOrEmpty(outputPath))
                    return;

                Debug.Log($"Output png : {outputPath}\nGraphicsFormat : {texture2D.graphicsFormat}");

                string dataPath = Application.dataPath;

                if (!outputPath.StartsWith(dataPath))
                {
                    File.WriteAllBytes(outputPath, texture2D.EncodeToPNG());
                    return;
                }

                string relativePath = outputPath.Remove(0, dataPath.Length - 6);
                bool existTextureImporter = AssetImporter.GetAtPath(relativePath) is TextureImporter;

                File.WriteAllBytes(outputPath, texture2D.EncodeToPNG());
                AssetDatabase.ImportAsset(relativePath);

                if (existTextureImporter)
                    return;

                int tempMax = Math.Max(texture2D.width, texture2D.height);
                tempMax = (int)Math.Pow(2, Math.Ceiling(Math.Log(tempMax, 2)));

                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                importer.sRGBTexture = GraphicsFormatUtility.IsSRGBFormat(texture2D.graphicsFormat);
                importer.maxTextureSize = tempMax;

                AssetDatabase.ImportAsset(relativePath);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        public static void OutputPNG(string outputPath, RenderTexture renderTexture)
        {
            Texture2D copyTexture2D = GenerateTexture2D(renderTexture);
            DataTransfer(renderTexture, copyTexture2D);
            OutputPNG(outputPath, copyTexture2D);
            UnityEngine.Object.DestroyImmediate(copyTexture2D);
        }

        public static void OutputPNG_OpenDialog(Texture2D texture2D)
        {
            string outputPath = EditorUtility.SaveFilePanel("Output PNG", "Assets", "texture", "png");
            OutputPNG(outputPath, texture2D);
        }

        public static void OutputPNG_OpenDialog(RenderTexture renderTexture)
        {
            string outputPath = EditorUtility.SaveFilePanel("Output PNG", "Assets", "texture", "png");
            OutputPNG(outputPath, renderTexture);
        }
    }
}

#endif