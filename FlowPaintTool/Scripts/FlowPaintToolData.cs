using System;
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

        public PaintModeEnum _paintMode;
        public Mesh _startMesh;
        public Renderer _sorceRenderer;
        public Vector2Int _outputTextureResolution;
        public StartTextureLoadModeEnum _startTextureLoadMode;
        public Texture _startTexture;
        public string _startTextureFilePath;
        public bool _startTextureSRGB;
        public int _targetUVChannel;
        public int _bleedRange;
        public float _uv_Epsilon;

        public bool _textureExist;
        public bool _actualSRGB;

        public void Reset()
        {
            _paintMode = PaintModeEnum.FlowPaintMode;
            _startMesh = null;
            _sorceRenderer = null;
            _outputTextureResolution = new Vector2Int(1024, 1024);
            _startTextureLoadMode = StartTextureLoadModeEnum.Assets;
            _startTexture = null;
            _startTextureFilePath = string.Empty;
            _startTextureSRGB = false;
            _bleedRange = 4;
            _uv_Epsilon = 0.0001f;
            _targetUVChannel = 0;

            _textureExist = false;
            _actualSRGB = false;
        }

        public void ConsistencyCheck()
        {
            _outputTextureResolution.x = Math.Max(_outputTextureResolution.x, 0);
            _outputTextureResolution.y = Math.Max(_outputTextureResolution.y, 0);
            _targetUVChannel = Math.Max(Math.Min(_targetUVChannel, 7), 0);
            _bleedRange = Math.Max(_bleedRange, 0);
            _uv_Epsilon = Math.Max(_uv_Epsilon, 0f);
        }

        public bool SRGBCheck()
        {
            bool result = false;

            if (_startTextureLoadMode == StartTextureLoadModeEnum.Assets)
            {
                result = _startTexture != null;
            }
            else if (_startTextureLoadMode == StartTextureLoadModeEnum.FilePath)
            {
                result = File.Exists(_startTextureFilePath);
            }

            _textureExist = result;

            result = false;

            if (_textureExist && (PlayerSettings.colorSpace == ColorSpace.Linear))
            {
                if (_startTextureLoadMode == StartTextureLoadModeEnum.Assets)
                {
                    result = GraphicsFormatUtility.IsSRGBFormat(_startTexture.graphicsFormat);
                }
                else if (_startTextureLoadMode == StartTextureLoadModeEnum.FilePath)
                {
                    result = _startTextureSRGB;
                }
            }

            _actualSRGB = result;
            return result;
        }
    }
}