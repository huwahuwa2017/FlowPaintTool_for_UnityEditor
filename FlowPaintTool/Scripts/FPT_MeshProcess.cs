#if UNITY_EDITOR

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    using TextData = FPT_Language.FPT_MeshProcessText;

    public class FPT_MeshProcess
    {
        private Mesh _paintModeMesh = null;
        private Mesh _maskModeMesh = null;

        private int _polygonCount = 0;
        private int _subMeshCount = 1;
        private Vector3[] _vertices;

        private IEnumerable<int> _pd_IndexArray = null;
        private int[] _pd_SubMeshIndexArray = null;
        private Vector3Int[] _pd_VertexIndexArray = null;
        private Vector3Int[] _pd_AdjacentIndexArray = null;
        private bool[] _pd_DuplicateUVArray = null;
        private int[][] _pd_duplicatePolygonIndexArrayArray = null;
        private Vector3[] _pd_CenterArray = null;
        private bool[] _pd_MaskArray = null;
        private bool[] _pd_MaskResultArray = null;

        private FPT_Main _fptMain = null;
        private Matrix4x4 _preMatrix = Matrix4x4.zero;

        public int GetSubMeshCount() => _subMeshCount;

        public Mesh GetPaintModeMesh() => _paintModeMesh;

        public Mesh GetMaskModeMesh() => _maskModeMesh;



        private void MaskModeMeshTriangleUpdate()
        {
            List<int> triangleList0 = new List<int>(_polygonCount);
            List<int> triangleList1 = new List<int>(_polygonCount);

            foreach (int pdIndex in _pd_IndexArray)
            {
                Vector3Int vIndex = _pd_VertexIndexArray[pdIndex];

                if (_pd_MaskArray[pdIndex])
                {
                    triangleList1.Add(vIndex.x);
                    triangleList1.Add(vIndex.y);
                    triangleList1.Add(vIndex.z);
                }
                else
                {
                    triangleList0.Add(vIndex.x);
                    triangleList0.Add(vIndex.y);
                    triangleList0.Add(vIndex.z);
                }
            }

            _maskModeMesh.SetTriangles(triangleList0, 0);
            _maskModeMesh.SetTriangles(triangleList1, 1);
        }

        public FPT_MeshProcess(FPT_Main fptMain, FPT_MainData fptData)
        {
            _fptMain = fptMain;

            Mesh startMesh = fptData._startMesh;
            int targetUVChannel = fptData._targetUVChannel;
            float uv_Epsilon = fptData._uv_Epsilon;



            _maskModeMesh = Object.Instantiate(startMesh);
            _maskModeMesh.MarkDynamic();
            _maskModeMesh.triangles = new int[0];
            _maskModeMesh.subMeshCount = 2;

            _paintModeMesh = Object.Instantiate(_maskModeMesh);
            _paintModeMesh.subMeshCount = 1;



            _vertices = startMesh.vertices;
            List<Vector2> uvs = new List<Vector2>();
            startMesh.GetUVs(targetUVChannel, uvs);
            int[] triangles = startMesh.triangles;
            _polygonCount = triangles.Length / 3;
            _subMeshCount = startMesh.subMeshCount;

            _pd_IndexArray = Enumerable.Range(0, _polygonCount);
            _pd_SubMeshIndexArray = new int[_polygonCount];
            _pd_VertexIndexArray = new Vector3Int[_polygonCount];
            _pd_DuplicateUVArray = new bool[_polygonCount];
            _pd_CenterArray = new Vector3[_polygonCount];
            _pd_MaskArray = new bool[_polygonCount];
            _pd_MaskResultArray = new bool[_polygonCount];

            // Generate _polygonList Start
            int triangleIndex = 0;

            for (int subMeshIndex = 0; subMeshIndex < _subMeshCount; ++subMeshIndex)
            {
                int[] subMeshTriangles = startMesh.GetTriangles(subMeshIndex);
                int subMeshTriangleArrayLength = subMeshTriangles.Length;

                for (int index = 0; index < subMeshTriangleArrayLength; index += 3)
                {
                    Vector3Int vertexIndex = new Vector3Int(subMeshTriangles[index], subMeshTriangles[index + 1], subMeshTriangles[index + 2]);
                    _pd_VertexIndexArray[triangleIndex] = vertexIndex;
                    _pd_SubMeshIndexArray[triangleIndex] = subMeshIndex;
                    ++triangleIndex;
                }
            }
            // Generate polygon list End

            // Compute shader Start
            ComputeShader cs_adjacentPolygon = FPT_Assets.GetStaticInstance().GetAdjacentPolygonComputeShader();

            ComputeBuffer cb_Vertices = new ComputeBuffer(_vertices.Count(), Marshal.SizeOf(typeof(Vector3)));
            ComputeBuffer cb_UVs = new ComputeBuffer(uvs.Count(), Marshal.SizeOf(typeof(Vector2)));
            ComputeBuffer cb_Triangles = new ComputeBuffer(triangles.Count(), Marshal.SizeOf(typeof(int)));
            ComputeBuffer cb_AdjacentResult = new ComputeBuffer(_polygonCount, Marshal.SizeOf(typeof(Vector3Int)));
            ComputeBuffer cb_CenterUVResult = new ComputeBuffer(_polygonCount, Marshal.SizeOf(typeof(Vector2)));
            ComputeBuffer cb_DuplicateResult = new ComputeBuffer(_polygonCount, Marshal.SizeOf(typeof(int)));

            cb_Vertices.SetData(_vertices);
            cb_UVs.SetData(uvs);
            cb_Triangles.SetData(triangles);

            int adjacent_Main_KI = cs_adjacentPolygon.FindKernel("Adjacent_Main");
            int duplicate_Main_KI = cs_adjacentPolygon.FindKernel("Duplicate_Main");

            cs_adjacentPolygon.SetInt("_TriangleCount", _polygonCount);
            cs_adjacentPolygon.SetFloat("_Epsilon", uv_Epsilon);

            cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_Vertices", cb_Vertices);
            cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_UVs", cb_UVs);
            cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_Triangles", cb_Triangles);
            cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_AdjacentResult", cb_AdjacentResult);
            cs_adjacentPolygon.SetBuffer(adjacent_Main_KI, "_CenterUVResult", cb_CenterUVResult);
            cs_adjacentPolygon.Dispatch(adjacent_Main_KI, _polygonCount, 1, 1);

            _pd_AdjacentIndexArray = new Vector3Int[_polygonCount];
            cb_AdjacentResult.GetData(_pd_AdjacentIndexArray);

            cs_adjacentPolygon.SetBuffer(duplicate_Main_KI, "_CenterUVResult", cb_CenterUVResult);
            cs_adjacentPolygon.SetBuffer(duplicate_Main_KI, "_DuplicateResult", cb_DuplicateResult);
            cs_adjacentPolygon.Dispatch(duplicate_Main_KI, _polygonCount, 1, 1);

            int[] duplicateResult = new int[_polygonCount];
            cb_DuplicateResult.GetData(duplicateResult);

            cb_Vertices.Release();
            cb_UVs.Release();
            cb_Triangles.Release();
            cb_AdjacentResult.Release();
            cb_CenterUVResult.Release();
            cb_DuplicateResult.Release();
            // Compute shader End

            // Generate _duplicatePolygonListList Start
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



            MaskModeMeshTriangleUpdate();
        }



        public void CenterRecalculation(Matrix4x4 matrix)
        {
            if (_preMatrix == matrix) return;

            _preMatrix = matrix;

            Vector3[] vpArray = _vertices.Clone() as Vector3[];
            int vpArrayLength = vpArray.Length;

            for (int index = 0; index < vpArrayLength; ++index)
            {
                vpArray[index] = matrix.MultiplyPoint3x4(vpArray[index]);
            }

            foreach (int pdIndex in _pd_IndexArray)
            {
                Vector3Int vIndex = _pd_VertexIndexArray[pdIndex];

                _pd_CenterArray[pdIndex] = (vpArray[vIndex.x] + vpArray[vIndex.y] + vpArray[vIndex.z]) / 3f;
            }
        }



        public void PaintModeMeshTriangleUpdate(Vector3 hitPosition)
        {
            foreach (int pdIndex in _pd_IndexArray)
            {
                _pd_MaskResultArray[pdIndex] = _pd_DuplicateUVArray[pdIndex] || _pd_MaskArray[pdIndex];
            }

            foreach (int[] duplicatePolygonList in _pd_duplicatePolygonIndexArrayArray)
            {
                float minSqrDistance = float.MaxValue;
                int targetPolygonIndex = -1;

                for (int index = 0; index < duplicatePolygonList.Length; index++)
                {
                    int pdIndex = duplicatePolygonList[index];

                    if (_pd_MaskArray[pdIndex]) continue;

                    float sqrDistance = (hitPosition - _pd_CenterArray[pdIndex]).sqrMagnitude;

                    if (sqrDistance < minSqrDistance)
                    {
                        minSqrDistance = sqrDistance;
                        targetPolygonIndex = pdIndex;
                    }
                }

                if (targetPolygonIndex != -1)
                {
                    _pd_MaskResultArray[targetPolygonIndex] = false;
                }
            }

            List<int> triangleList0 = new List<int>(_polygonCount);

            foreach (int pdIndex in _pd_IndexArray)
            {
                if (_pd_MaskResultArray[pdIndex]) continue;

                Vector3Int vIndex = _pd_VertexIndexArray[pdIndex];
                triangleList0.Add(vIndex.x);
                triangleList0.Add(vIndex.y);
                triangleList0.Add(vIndex.z);
            }

            _paintModeMesh.SetTriangles(triangleList0, 0);
        }



        public void MaskProcess()
        {
            bool hit = _fptMain.PaintToolRaycast(out RaycastHit raycastHit);

            if (!hit) return;

            bool leftClick = Input.GetMouseButton(0);
            bool rightClick = Input.GetMouseButton(1);
            bool click = leftClick || rightClick;

            if (click)
            {
                Vector3 hitPosition = raycastHit.point;
                float brushSize = FPT_EditorData.GetStaticInstance().GetBrushSize();
                float sqrRange = brushSize * brushSize;

                foreach (int pdIndex in _pd_IndexArray)
                {
                    if ((hitPosition - _pd_CenterArray[pdIndex]).sqrMagnitude < sqrRange)
                    {
                        _pd_MaskArray[pdIndex] = rightClick;
                    }
                }

                MaskModeMeshTriangleUpdate();
            }
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

        public void LinkedUnmask()
        {
            IEnumerable<int> temp0 = _pd_IndexArray.Where(I => !_pd_MaskArray[I]);
            ConcurrentBag<int> temp1 = GetAllConnectedTriangles(temp0);

            foreach (int temp2 in temp1)
            {
                _pd_MaskArray[temp2] = false;
            }

            MaskModeMeshTriangleUpdate();
        }

        public void LinkedMask()
        {
            IEnumerable<int> temp0 = _pd_IndexArray.Where(I => _pd_MaskArray[I]);
            ConcurrentBag<int> temp1 = GetAllConnectedTriangles(temp0);

            foreach (int temp2 in temp1)
            {
                _pd_MaskArray[temp2] = true;
            }

            MaskModeMeshTriangleUpdate();
        }



        public void MeshProcessGUI()
        {
            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(TextData.LinkedMask))
                {
                    LinkedMask();
                }

                if (GUILayout.Button(TextData.LinkedUnmask))
                {
                    LinkedUnmask();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(TextData.MaskAll))
                {
                    foreach (int pdIndex in _pd_IndexArray)
                    {
                        _pd_MaskArray[pdIndex] = true;
                    }

                    MaskModeMeshTriangleUpdate();
                }

                if (GUILayout.Button(TextData.UnmaskAll))
                {
                    foreach (int pdIndex in _pd_IndexArray)
                    {
                        _pd_MaskArray[pdIndex] = false;
                    }

                    MaskModeMeshTriangleUpdate();
                }

                if (GUILayout.Button(TextData.InvertAll))
                {
                    foreach (int pdIndex in _pd_IndexArray)
                    {
                        _pd_MaskArray[pdIndex] = !_pd_MaskArray[pdIndex];
                    }

                    MaskModeMeshTriangleUpdate();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            for (int index = 0; index < _subMeshCount; ++index)
            {
                EditorGUILayout.LabelField(TextData.SubMeshIndex + index);

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(TextData.Mask))
                    {
                        foreach (int pdIndex in _pd_IndexArray)
                        {
                            if (_pd_SubMeshIndexArray[pdIndex] != index) continue;

                            _pd_MaskArray[pdIndex] = true;
                        }

                        MaskModeMeshTriangleUpdate();
                    }

                    if (GUILayout.Button(TextData.Unmask))
                    {
                        foreach (int pdIndex in _pd_IndexArray)
                        {
                            if (_pd_SubMeshIndexArray[pdIndex] != index) continue;

                            _pd_MaskArray[pdIndex] = false;
                        }

                        MaskModeMeshTriangleUpdate();
                    }

                    if (GUILayout.Button(TextData.Invert))
                    {
                        foreach (int pdIndex in _pd_IndexArray)
                        {
                            if (_pd_SubMeshIndexArray[pdIndex] != index) continue;

                            _pd_MaskArray[pdIndex] = !_pd_MaskArray[pdIndex];
                        }

                        MaskModeMeshTriangleUpdate();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(20);
        }
    }
}

#endif