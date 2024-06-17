#if UNITY_EDITOR

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace FlowPaintTool
{
    public class FPT_MeshProcess
    {
        private int _polygonCount = 0;

        private IEnumerable<int> _pd_IndexArray = null;
        private Vector3Int[] _pd_AdjacentIndexArray = null;
        private bool[] _pd_DuplicateUVArray = null;
        private int[][] _pd_duplicatePolygonIndexArrayArray = null;
        private bool[] _pd_ThinningArray = null;

        private byte[] _thinningArrayTemp0 = null;

        public FPT_MeshProcess(FPT_MainData fptData)
        {
            Vector3[] vertices = fptData._startMesh.vertices;

            List<Vector2> uvs = new List<Vector2>();
            fptData._startMesh.GetUVs(fptData._targetUVChannel, uvs);

            int[] triangles = fptData._startMesh.GetTriangles(fptData._targetSubMesh);
            _polygonCount = triangles.Length / 3;

            _pd_IndexArray = Enumerable.Range(0, _polygonCount);
            _pd_ThinningArray = new bool[_polygonCount];

            int polygonDataTexSize = (int)Math.Ceiling(Math.Sqrt(_polygonCount));
            _thinningArrayTemp0 = new byte[polygonDataTexSize * polygonDataTexSize];

            // Compute shader Start
            _pd_AdjacentIndexArray = new Vector3Int[_polygonCount];
            int[] duplicateResult = new int[_polygonCount];
            {
                ComputeShader cs_adjacentPolygon = FPT_Assets.GetSingleton().GetAdjacentPolygonComputeShader();

                int adjacent_Main_KI = cs_adjacentPolygon.FindKernel("Adjacent_Main");
                int duplicate_Main_KI = cs_adjacentPolygon.FindKernel("Duplicate_Main");

                ComputeBuffer cb_Vertices = new ComputeBuffer(vertices.Count(), Marshal.SizeOf(typeof(Vector3)));
                ComputeBuffer cb_UVs = new ComputeBuffer(uvs.Count(), Marshal.SizeOf(typeof(Vector2)));
                ComputeBuffer cb_Triangles = new ComputeBuffer(triangles.Count(), Marshal.SizeOf(typeof(int)));
                ComputeBuffer cb_AdjacentResult = new ComputeBuffer(_polygonCount, Marshal.SizeOf(typeof(Vector3Int)));
                ComputeBuffer cb_CenterUVResult = new ComputeBuffer(_polygonCount, Marshal.SizeOf(typeof(Vector2)));
                ComputeBuffer cb_DuplicateResult = new ComputeBuffer(_polygonCount, Marshal.SizeOf(typeof(int)));

                cb_Vertices.SetData(vertices);
                cb_UVs.SetData(uvs);
                cb_Triangles.SetData(triangles);

                cs_adjacentPolygon.SetInt("_TriangleCount", _polygonCount);
                cs_adjacentPolygon.SetFloat("_Epsilon", fptData._uv_Epsilon);
                cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_Vertices", cb_Vertices);
                cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_UVs", cb_UVs);
                cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_Triangles", cb_Triangles);
                cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_AdjacentResult", cb_AdjacentResult);
                cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_CenterUVResult", cb_CenterUVResult);
                cs_adjacentPolygon.Dispatch(adjacent_Main_KI, _polygonCount, 1, 1);
                cb_AdjacentResult.GetData(_pd_AdjacentIndexArray);

                cs_adjacentPolygon.SetBuffer(duplicate_Main_KI, "_CenterUVResult", cb_CenterUVResult);
                cs_adjacentPolygon.SetBuffer(duplicate_Main_KI, "_DuplicateResult", cb_DuplicateResult);
                cs_adjacentPolygon.Dispatch(duplicate_Main_KI, _polygonCount, 1, 1);
                cb_DuplicateResult.GetData(duplicateResult);

                cb_Vertices.Release();
                cb_UVs.Release();
                cb_Triangles.Release();
                cb_AdjacentResult.Release();
                cb_CenterUVResult.Release();
                cb_DuplicateResult.Release();
            }
            // Compute shader End

            // Generate _duplicatePolygonListList Start
            _pd_DuplicateUVArray = new bool[_polygonCount];

            bool[] checkIndex = new bool[_polygonCount];
            List<int[]> duplicatePolygonIndexArray = new List<int[]>();

            foreach (int startIndex in _pd_IndexArray)
            {
                int duplicateIndex = duplicateResult[startIndex];

                if (duplicateIndex == -1 || checkIndex[duplicateIndex]) continue;

                checkIndex[duplicateIndex] = true;
                List<int> duplicatePolygonIndexList = new List<int>();

                for (int index = startIndex; index < _polygonCount; ++index)
                {
                    if (duplicateResult[index] != duplicateIndex) continue;

                    _pd_DuplicateUVArray[index] = true;
                    duplicatePolygonIndexList.Add(index);
                }

                _pd_DuplicateUVArray[duplicateIndex] = true;
                duplicatePolygonIndexList.Add(duplicateIndex);
                duplicatePolygonIndexArray.Add(duplicatePolygonIndexList.ToArray());
            }

            _pd_duplicatePolygonIndexArrayArray = duplicatePolygonIndexArray.ToArray();
            // Generate _duplicatePolygonListList End
        }



        public byte[] ThinningTextureUpdate(byte[] data)
        {
            Array.Copy(_pd_DuplicateUVArray, _pd_ThinningArray, _pd_DuplicateUVArray.Length);

            foreach (int[] duplicatePolygonList in _pd_duplicatePolygonIndexArrayArray)
            {
                float minSqrDistance = float.MaxValue;
                int targetPolygonIndex = -1;

                for (int index = 0; index < duplicatePolygonList.Length; index++)
                {
                    int pdIndex = duplicatePolygonList[index];
                    float sqrDistance = BitConverter.ToSingle(data, pdIndex * 4);

                    if (sqrDistance < minSqrDistance)
                    {
                        minSqrDistance = sqrDistance;
                        targetPolygonIndex = pdIndex;
                    }
                }

                if (targetPolygonIndex != -1)
                {
                    _pd_ThinningArray[targetPolygonIndex] = false;
                }
            }

            for (int index = 0; index < _pd_ThinningArray.Length; ++index)
            {
                _thinningArrayTemp0[index] = _pd_ThinningArray[index] ? (byte)255 : (byte)0;
            }

            return _thinningArrayTemp0;
        }



        private ConcurrentBag<int> GetAllConnectedTriangles(IEnumerable<int> triangleIndexs)
        {
            bool[] connectedTriangles = new bool[_polygonCount];
            ConcurrentBag<int> queue = new ConcurrentBag<int>(triangleIndexs);
            ConcurrentBag<int> adjacentTriangles = new ConcurrentBag<int>();

            while (queue.Count > 0)
            {
                List<int> temp10 = queue.ToList();
                queue = new ConcurrentBag<int>();

                Parallel.ForEach(temp10, currentTriangleIndex =>
                {
                    if (currentTriangleIndex == -1 || connectedTriangles[currentTriangleIndex]) return;

                    connectedTriangles[currentTriangleIndex] = true;
                    adjacentTriangles.Add(currentTriangleIndex);

                    Vector3Int temp30 = _pd_AdjacentIndexArray[currentTriangleIndex];
                    queue.Add(temp30.x);
                    queue.Add(temp30.y);
                    queue.Add(temp30.z);
                });
            }

            return adjacentTriangles;
        }

        public void UnmaskLinked(byte[] data)
        {
            IEnumerable<int> temp0 = _pd_IndexArray.Where(I => data[I] == 0);
            ConcurrentBag<int> temp1 = GetAllConnectedTriangles(temp0);

            foreach (int temp2 in temp1)
            {
                data[temp2] = 0;
            }
        }

        public void MaskLinked(byte[] data)
        {
            IEnumerable<int> temp0 = _pd_IndexArray.Where(I => data[I] != 0);
            ConcurrentBag<int> temp1 = GetAllConnectedTriangles(temp0);

            foreach (int temp2 in temp1)
            {
                data[temp2] = 255;
            }
        }
    }
}

#endif