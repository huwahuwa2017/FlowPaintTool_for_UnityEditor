using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace FlowPaintTool
{
    public struct FlowPaintToolData
    {
        static public FlowPaintToolData Constructor()
        {
            FlowPaintToolData fptData = new FlowPaintToolData();
            fptData.Reset();
            return fptData;
        }

        public PaintMode _paintMode;
        public Mesh _startMesh;
        public Vector2Int _outputTextureResolution;
        public StartTextureLoadMode _startTextureLoadMode;
        public Texture _startTexture;
        public string _startTextureFilePath;
        public bool _startTextureSRGB;
        public int _bleedRange;
        public float _uv_Epsilon;

        public bool _textureExist;
        public bool _actualSRGB;

        public void Reset()
        {
            _paintMode = PaintMode.FlowPaintMode;
            _startMesh = null;
            _outputTextureResolution = new Vector2Int(1024, 1024);
            _startTextureLoadMode = StartTextureLoadMode.Assets;
            _startTexture = null;
            _startTextureFilePath = string.Empty;
            _startTextureSRGB = false;
            _bleedRange = 4;
            _uv_Epsilon = 0.001f;

            _textureExist = false;
            _actualSRGB = false;
        }

        public bool CheckTextureAndSRGB()
        {
            bool result = false;

            if (_startTextureLoadMode == StartTextureLoadMode.Assets)
            {
                result = _startTexture != null;
            }
            else if (_startTextureLoadMode == StartTextureLoadMode.FilePath)
            {
                result = File.Exists(_startTextureFilePath);
            }

            _textureExist = result;

            result = false;

            if (_textureExist && (PlayerSettings.colorSpace == ColorSpace.Linear))
            {
                if (_startTextureLoadMode == StartTextureLoadMode.Assets)
                {
                    result = GraphicsFormatUtility.IsSRGBFormat(_startTexture.graphicsFormat);
                }
                else if (_startTextureLoadMode == StartTextureLoadMode.FilePath)
                {
                    result = _startTextureSRGB;
                }
            }

            _actualSRGB = result;
            return result;
        }
    }
}