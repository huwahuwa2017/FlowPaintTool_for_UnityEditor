#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace FlowPaintTool
{
    public class FPT_Raycast
    {
        private RenderTexture _rt = null;

        private Texture2D _memory = null;

        private CommandBuffer _commandBuffer = null;

        private Material _material_WorldPosition = null;

        private float _accuracy = 0.001f;

        public FPT_Raycast()
        {
            RenderTextureDescriptor rtd = new RenderTextureDescriptor(1, 1, GraphicsFormat.R32G32B32A32_SFloat, 16);
            _rt = new RenderTexture(rtd);

            _memory = FPT_TextureOperation.GenerateMemoryTexture(_rt);

            _commandBuffer = new CommandBuffer();

            _material_WorldPosition = FPT_Assets.GetSingleton().GetWorldPositionMaterial();
        }

        public bool Raycast(Renderer renderer, int[] subMeshIndexs, Ray ray, out Vector3 point, float maxDistance)
        {
            Matrix4x4 viewMatrix;
            {
                Quaternion rotation = Quaternion.Inverse(Quaternion.LookRotation(ray.direction));

                Matrix4x4 temp00 = Matrix4x4.TRS(-ray.origin, Quaternion.identity, Vector3.one);
                Matrix4x4 temp01 = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
                viewMatrix = temp01 * temp00;
                Vector4 temp05 = viewMatrix.GetRow(2);
                viewMatrix.SetRow(2, -temp05);
            }

            Matrix4x4 projectionMatrix = Matrix4x4.Ortho(-_accuracy, _accuracy, -_accuracy, _accuracy, 0f, maxDistance);

            FPT_TextureOperation.ClearRenderTexture(_rt, Color.clear);

            _commandBuffer.Clear();
            _commandBuffer.SetRenderTarget(_rt);
            _commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            foreach (int subMeshIndex in subMeshIndexs)
            {
                _commandBuffer.DrawRenderer(renderer, _material_WorldPosition, subMeshIndex);
            }

            Graphics.ExecuteCommandBuffer(_commandBuffer);

            FPT_TextureOperation.DataTransfer(_rt, _memory);
            Color color = _memory.GetPixel(0, 0);
            point.x = color.r;
            point.y = color.g;
            point.z = color.b;

            return color.a != 0f;
        }
    }
}

#endif