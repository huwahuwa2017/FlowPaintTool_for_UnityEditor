using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    public class FPT_MeshProcess
    {
        private Mesh _paintModeMesh = null;
        private Mesh _maskModeMesh = null;

        private int _polygonCount = 0;
        private int _subMeshCount = 1;
        private Vector3[] _vertices;

        private int[] _pd_SubMeshIndexArray = null;
        private Vector3Int[] _pd_VertexIndexArray = null;
        private Vector3Int[] _pd_AdjacentIndexArray = null;
        private bool[] _pd_DuplicateUVArray = null;
        private int[][] _pd_duplicatePolygonIndexArrayArray = null;
        private Vector3[] _pd_CenterArray = null;
        private bool[] _pd_MaskArray = null;
        private bool[] _pd_MaskResultArray = null;

        private FPT_EditorData _editorData = null;
        private Matrix4x4 _preMatrix = Matrix4x4.zero;

        public int GetSubMeshCount()
        {
            return _subMeshCount;
        }

        public Mesh GetPaintModeMesh()
        {
            return _paintModeMesh;
        }

        public Mesh GetMaskModeMesh()
        {
            return _maskModeMesh;
        }



        private void MaskModeMeshTriangleUpdate()
        {
            List<int> triangleList0 = new List<int>(_polygonCount);
            List<int> triangleList1 = new List<int>(_polygonCount);

            for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
            {
                Vector3Int vIndex = _pd_VertexIndexArray[pIndex];

                if (_pd_MaskArray[pIndex])
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

        public FPT_MeshProcess(FPT_MainData fptData)
        {
            _editorData = FPT_EditorWindow.EditorDataInstance;
            FPT_Assets assets = FPT_EditorWindow.RequestAssetsInstance;
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
            ComputeShader cs_adjacentPolygon = assets._adjacentPolygonComputeShader;

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

            for (int startIndex = 0; startIndex < _polygonCount; ++startIndex)
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
            int maxIndex = vpArray.Length;

            for (int index = 0; index < maxIndex; ++index)
            {
                vpArray[index] = matrix.MultiplyPoint3x4(vpArray[index]);
            }

            for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
            {
                Vector3Int vIndex = _pd_VertexIndexArray[pIndex];

                _pd_CenterArray[pIndex] = (vpArray[vIndex.x] + vpArray[vIndex.y] + vpArray[vIndex.z]) / 3f;
            }
        }



        public void PaintModeMeshTriangleUpdate(Vector3 hitPosition)
        {
            for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
            {
                _pd_MaskResultArray[pIndex] = _pd_DuplicateUVArray[pIndex] || _pd_MaskArray[pIndex];
            }

            foreach (int[] duplicatePolygonList in _pd_duplicatePolygonIndexArrayArray)
            {
                float minSqrDistance = float.MaxValue;
                int targetPolygonIndex = -1;

                for (int index = 0; index < duplicatePolygonList.Length; index++)
                {
                    int pIndex = duplicatePolygonList[index];

                    if (_pd_MaskArray[pIndex]) continue;

                    float sqrDistance = (hitPosition - _pd_CenterArray[pIndex]).sqrMagnitude;

                    if (sqrDistance < minSqrDistance)
                    {
                        minSqrDistance = sqrDistance;
                        targetPolygonIndex = pIndex;
                    }
                }

                if (targetPolygonIndex != -1)
                {
                    _pd_MaskResultArray[targetPolygonIndex] = false;
                }
            }

            List<int> triangleList0 = new List<int>(_polygonCount);

            for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
            {
                if (_pd_MaskResultArray[pIndex]) continue;

                Vector3Int vIndex = _pd_VertexIndexArray[pIndex];
                triangleList0.Add(vIndex.x);
                triangleList0.Add(vIndex.y);
                triangleList0.Add(vIndex.z);
            }

            _paintModeMesh.SetTriangles(triangleList0, 0);
        }



        public void MaskPaint()
        {
            bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit raycastHit, 1000f);

            if (!hit) return;

            bool leftClick = Input.GetMouseButton(0);
            bool rightClick = Input.GetMouseButton(1);
            bool click = leftClick || rightClick;

            if (click)
            {
                Vector3 hitPosition = raycastHit.point;
                float brushSize = _editorData.GetBrushSize();
                float sqrRange = brushSize * brushSize;

                for (int index = 0; index < _polygonCount; index++)
                {
                    if ((hitPosition - _pd_CenterArray[index]).sqrMagnitude < sqrRange)
                    {
                        _pd_MaskArray[index] = rightClick;
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

        public void SelectLinkedPlus()
        {
            IEnumerable<int> temp0 = Enumerable.Range(0, _polygonCount).Where(I => !_pd_MaskArray[I]);
            ConcurrentBag<int> temp1 = GetAllConnectedTriangles(temp0);

            foreach (int temp2 in temp1)
            {
                _pd_MaskArray[temp2] = false;
            }

            MaskModeMeshTriangleUpdate();
        }

        public void SelectLinkedMinus()
        {
            IEnumerable<int> temp0 = Enumerable.Range(0, _polygonCount).Where(I => _pd_MaskArray[I]);
            ConcurrentBag<int> temp1 = GetAllConnectedTriangles(temp0);

            foreach (int temp2 in temp1)
            {
                _pd_MaskArray[temp2] = true;
            }

            MaskModeMeshTriangleUpdate();
        }



        public void MeshProcessGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Select linked (Mask)"))
                {
                    SelectLinkedMinus();
                }

                if (GUILayout.Button("Select linked (Unmask)"))
                {
                    SelectLinkedPlus();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("All Mask"))
                {
                    for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
                    {
                        _pd_MaskArray[pIndex] = true;
                    }

                    MaskModeMeshTriangleUpdate();
                }

                if (GUILayout.Button("All Unmask"))
                {
                    for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
                    {
                        _pd_MaskArray[pIndex] = false;
                    }

                    MaskModeMeshTriangleUpdate();
                }

                if (GUILayout.Button("All Inversion"))
                {
                    for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
                    {
                        _pd_MaskArray[pIndex] = !_pd_MaskArray[pIndex];
                    }

                    MaskModeMeshTriangleUpdate();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            for (int index = 0; index < _subMeshCount; ++index)
            {
                EditorGUILayout.LabelField("SubMeshIndex : " + index);

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Mask"))
                    {
                        for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
                        {
                            if (_pd_SubMeshIndexArray[pIndex] != index) continue;

                            _pd_MaskArray[pIndex] = true;
                        }

                        MaskModeMeshTriangleUpdate();
                    }

                    if (GUILayout.Button("Unmask"))
                    {
                        for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
                        {
                            if (_pd_SubMeshIndexArray[pIndex] != index) continue;

                            _pd_MaskArray[pIndex] = false;
                        }

                        MaskModeMeshTriangleUpdate();
                    }

                    if (GUILayout.Button("Inversion"))
                    {
                        for (int pIndex = 0; pIndex < _polygonCount; pIndex++)
                        {
                            if (_pd_SubMeshIndexArray[pIndex] != index) continue;

                            _pd_MaskArray[pIndex] = !_pd_MaskArray[pIndex];
                        }

                        MaskModeMeshTriangleUpdate();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
